using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SADFinalProjectGJ.Data;
using SADFinalProjectGJ.Models;
using SADFinalProjectGJ.ViewModels;
using SADFinalProjectGJ.Services;

namespace SADFinalProjectGJ.Controllers
{
    [Authorize]
    public class InvoicesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;

        public InvoicesController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        // GET: Invoices
        public async Task<IActionResult> Index(string searchString, decimal? minAmount, decimal? maxAmount, bool showArchived = false)
        {
            var query = _context.Invoices.Include(i => i.Client).AsQueryable();

            if (User.IsInRole("Client"))
            {
                var currentUserId = _userManager.GetUserId(User);
                query = query.Where(i => i.Client.UserId == currentUserId);
            }

            // 如果 showArchived 是 true，只看归档的；否则只看没归档的
            query = query.Where(i => i.IsArchived == showArchived);

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(i =>
                    i.InvoiceNumber.Contains(searchString) ||
                    (i.Client != null && i.Client.Name.Contains(searchString)) ||
                    (i.Status != null && i.Status.Contains(searchString)) 
                    //|| i.TotalAmount.ToString().Contains(searchString)
                );
            }
            if (minAmount.HasValue)
            {
                query = query.Where(i => i.TotalAmount >= minAmount.Value);
            }

            if (maxAmount.HasValue)
            {
                query = query.Where(i => i.TotalAmount <= maxAmount.Value);
            }

            // 👇 4. 把搜索条件传回给 View，让输入框保持填写的数值
            ViewData["CurrentFilter"] = searchString;
            ViewData["MinAmount"] = minAmount;
            ViewData["MaxAmount"] = maxAmount;
            ViewData["ShowArchived"] = showArchived;
            return View(await query.ToListAsync());
        }

        // GET: Invoices/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.InvoiceItems)!
                    .ThenInclude(ii => ii.Item)
                .FirstOrDefaultAsync(m => m.InvoiceId == id);

            if (invoice == null) return NotFound();

            if (User.IsInRole("Client"))
            {
                var currentUserId = _userManager.GetUserId(User);
                if (invoice.Client == null || invoice.Client.UserId != currentUserId)
                {
                    return Forbid();
                }
            }

            return View(invoice);
        }

        // GET: Invoices/Create
        [Authorize(Roles = "Admin,FinanceStaff")]
        public IActionResult Create()
        {
            ViewData["ClientId"] = new SelectList(_context.Clients, "ClientId", "Name");
            ViewBag.ItemList = _context.Items.ToList();
            return View(new InvoiceCreateViewModel());
        }

        // POST: Invoices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,FinanceStaff")]
        public async Task<IActionResult> Create(InvoiceCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var invoice = new Invoice
                {
                    InvoiceNumber = "INV-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                    ClientId = model.ClientId,
                    IssueDate = DateTime.Now,
                    DueDate = model.DueDate,
                    Status = "Draft", // 建议：创建时通常默认为 Draft，确认无误后再改为 Sent
                    InvoiceItems = new List<InvoiceItem>()
                };

                decimal calculatedTotal = 0;

                if (model.Items != null && model.Items.Count > 0)
                {
                    foreach (var entry in model.Items)
                    {
                        var dbItem = await _context.Items.FindAsync(entry.ItemId);
                        if (dbItem != null)
                        {
                            var lineTotal = dbItem.UnitPrice * entry.Quantity;
                            var invoiceItem = new InvoiceItem
                            {
                                ItemId = entry.ItemId,
                                Quantity = entry.Quantity,
                                UnitPrice = dbItem.UnitPrice,
                                Total = lineTotal
                            };
                            invoice.InvoiceItems.Add(invoiceItem);
                            calculatedTotal += lineTotal;
                        }
                    }
                }

                // 使用用户输入的 GstRate 计算税额
                invoice.TotalAmount = calculatedTotal;
                invoice.TaxAmount = calculatedTotal * (model.GstRate / 100m);

                _context.Add(invoice);
                await _context.SaveChangesAsync();

                // 发送邮件逻辑
                var client = await _context.Clients.FindAsync(model.ClientId);
                if (client != null && !string.IsNullOrEmpty(client.AccountEmail))
                {
                    try
                    {
                        string subject = $"New Invoice Created: {invoice.InvoiceNumber}";
                        string body = $"Dear {client.Name},<br/>A new invoice <b>{invoice.InvoiceNumber}</b> has been created.<br/>Total: {invoice.TotalAmount:C}";

                        await _emailService.SendEmailAsync(client.AccountEmail, subject, body);

                        TempData["Success"] = $"Invoice {invoice.InvoiceNumber} created successfully and email sent!";

                        var notification = new Notification
                        {
                            RecipientEmail = client.AccountEmail,
                            Subject = subject,
                            Message = "Invoice created notification sent.",
                            SentDate = DateTime.Now,
                            Status = "Sent",
                            UserId = client.UserId
                        };
                        _context.Add(notification);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        TempData["Warning"] = $"Invoice created, but email failed: {ex.Message}";
                    }
                }
                else
                {
                    TempData["Warning"] = "Invoice created, but client has no email.";
                }

                return RedirectToAction(nameof(Index));
            }

            ViewData["ClientId"] = new SelectList(_context.Clients, "ClientId", "Name", model.ClientId);
            ViewBag.ItemList = _context.Items.ToList();
            return View(model);
        }

        // GET: Invoices/Edit/5
        [Authorize(Roles = "Admin,FinanceStaff")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(m => m.InvoiceId == id);

            if (invoice == null) return NotFound();

            ViewData["ClientId"] = new SelectList(_context.Clients, "ClientId", "Name", invoice.ClientId);

            ViewBag.ItemList = await _context.Items
                .Select(i => new { i.ItemId, i.Description, i.UnitPrice })
                .ToListAsync();

            decimal currentRate = 9;
            if (invoice.TotalAmount > 0)
            {
                currentRate = Math.Round((invoice.TaxAmount / invoice.TotalAmount) * 100, 2);
            }
            ViewData["CurrentGstRate"] = currentRate;

            return View(invoice);
        }

        // POST: Invoices/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,FinanceStaff")]
        public async Task<IActionResult> Edit(int id, [Bind("InvoiceId,InvoiceNumber,Status,Notes,TotalAmount,TaxAmount,ClientId,IssueDate,DueDate,InvoiceItems")] Invoice invoice, decimal gstRate)
        {
            if (id != invoice.InvoiceId) return NotFound();

            ModelState.Remove("InvoiceItems");

            var dbInvoice = await _context.Invoices
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (dbInvoice == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    dbInvoice.InvoiceNumber = invoice.InvoiceNumber;
                    dbInvoice.Status = invoice.Status;
                    dbInvoice.Notes = invoice.Notes;
                    dbInvoice.ClientId = invoice.ClientId;
                    dbInvoice.IssueDate = invoice.IssueDate;
                    dbInvoice.DueDate = invoice.DueDate;

                    // 清空旧项目，重新添加
                    if (dbInvoice.InvoiceItems != null)
                    {
                        _context.InvoiceItems.RemoveRange(dbInvoice.InvoiceItems);
                    }

                    dbInvoice.InvoiceItems = new List<InvoiceItem>();
                    decimal calculatedTotal = 0;

                    if (invoice.InvoiceItems != null && invoice.InvoiceItems.Count > 0)
                    {
                        foreach (var itemInput in invoice.InvoiceItems)
                        {
                            var dbItem = await _context.Items.FindAsync(itemInput.ItemId);
                            if (dbItem != null)
                            {
                                var lineTotal = dbItem.UnitPrice * itemInput.Quantity;
                                dbInvoice.InvoiceItems.Add(new InvoiceItem
                                {
                                    ItemId = itemInput.ItemId,
                                    Quantity = itemInput.Quantity,
                                    UnitPrice = dbItem.UnitPrice,
                                    Total = lineTotal
                                });
                                calculatedTotal += lineTotal;
                            }
                        }
                    }

                    // 重新计算总额和税额
                    dbInvoice.TotalAmount = calculatedTotal;
                    dbInvoice.TaxAmount = calculatedTotal * (gstRate / 100m); // 使用 Edit 传入的 gstRate

                    _context.Update(dbInvoice);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InvoiceExists(invoice.InvoiceId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["ClientId"] = new SelectList(_context.Clients, "ClientId", "Name", invoice.ClientId);
            ViewBag.ItemList = _context.Items.ToList();
            return View(invoice);
        } // ✅ 这里正确关闭 Edit 方法

        // GET: Invoices/Delete/5
        [Authorize(Roles = "Admin,FinanceStaff")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .FirstOrDefaultAsync(m => m.InvoiceId == id);
            if (invoice == null) return NotFound();

            return View(invoice);
        }

        // POST: Invoices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,、")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice != null)
            {
                _context.Invoices.Remove(invoice);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Invoices/Archive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,FinanceStaff")]
        public async Task<IActionResult> Archive(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null) return NotFound();

            invoice.IsArchived = true; // 标记为归档
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Invoice {invoice.InvoiceNumber} has been archived.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Invoices/Restore/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,FinanceStaff")]
        public async Task<IActionResult> Restore(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null) return NotFound();

            invoice.IsArchived = false; // 还原
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Invoice {invoice.InvoiceNumber} has been restored.";
            return RedirectToAction(nameof(Index), new { showArchived = true }); // 还原后停留在归档页方便查看
        }
        private bool InvoiceExists(int id)
        {
            return _context.Invoices.Any(e => e.InvoiceId == id);
        }
    }
}