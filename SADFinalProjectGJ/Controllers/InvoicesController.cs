using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; // 权限控制
using Microsoft.AspNetCore.Identity;      // 用户身份
using SADFinalProjectGJ.Data;
using SADFinalProjectGJ.Models;
using SADFinalProjectGJ.ViewModels;       // 确保引用了 ViewModel

namespace SADFinalProjectGJ.Controllers
{
    [Authorize]
    public class InvoicesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public InvoicesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Invoices
        public async Task<IActionResult> Index()
        {
            var query = _context.Invoices.Include(i => i.Client).AsQueryable();

            if (User.IsInRole("Client"))
            {
                var currentUserId = _userManager.GetUserId(User);
                // 只有 Client 关联了 UserId 才能过滤
                // 如果你的 Client 表还没有 UserId 字段，这里会报错，请确认你是否做了 Migration
                query = query.Where(i => i.Client.UserId == currentUserId);
            }

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
            // 确保你 Items 表里有数据，否则下拉框是空的
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
                    Status = "Draft",
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

                invoice.TotalAmount = calculatedTotal;
                invoice.TaxAmount = calculatedTotal * 0.09m;

                _context.Add(invoice);
                await _context.SaveChangesAsync();
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

            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null) return NotFound();

            ViewData["ClientId"] = new SelectList(_context.Clients, "ClientId", "Name", invoice.ClientId);
            return View(invoice);
        }

        // POST: Invoices/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,FinanceStaff")]
        public async Task<IActionResult> Edit(int id, [Bind("InvoiceId,InvoiceNumber,Status,Notes,TotalAmount,TaxAmount,ClientId,IssueDate,DueDate")] Invoice invoice)
        {
            if (id != invoice.InvoiceId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(invoice);
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
            return View(invoice);
        }

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
        [Authorize(Roles = "Admin,FinanceStaff")]
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

        private bool InvoiceExists(int id)
        {
            return _context.Invoices.Any(e => e.InvoiceId == id);
        }
    }
}