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
        public async Task<IActionResult> Index(string searchString)
        {
            // 1. 基础查询：预加载 Client 数据
            var query = _context.Invoices.Include(i => i.Client).AsQueryable();

            // 2. 权限控制 (Security Filter)
            // 必须在搜索前执行：Client 只能在自己的数据池里搜
            if (User.IsInRole("Client"))
            {
                var currentUserId = _userManager.GetUserId(User);
                query = query.Where(i => i.Client.UserId == currentUserId);
            }

            // 3. 多字段搜索 (Search Filter)
            if (!string.IsNullOrEmpty(searchString))
            {
                // 支持搜索：发票号、客户名、状态、金额
                query = query.Where(i =>
                    i.InvoiceNumber.Contains(searchString) ||
                    (i.Client != null && i.Client.Name.Contains(searchString)) ||
                    (i.Status != null && i.Status.Contains(searchString)) ||
                    // 将金额转为字符串进行模糊匹配 (例如搜 "200" 能找到 "200.00")
                    i.TotalAmount.ToString().Contains(searchString)
                );
            }

            // 将搜索词回传给前端，保持输入框里有字
            ViewData["CurrentFilter"] = searchString;

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

            var invoice = await _context.Invoices
                .Include(i => i.InvoiceItems) // 必须包含原有商品
                .FirstOrDefaultAsync(m => m.InvoiceId == id);

            if (invoice == null) return NotFound();

            ViewData["ClientId"] = new SelectList(_context.Clients, "ClientId", "Name", invoice.ClientId);

            // ✅ 修复点：只选择需要的字段，防止 JSON 序列化报错
            ViewBag.ItemList = await _context.Items
                .Select(i => new { i.ItemId, i.Description, i.UnitPrice })
                .ToListAsync();

            return View(invoice);
        }

        // POST: Invoices/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,FinanceStaff")]
        // 3. 修改：在 Bind 中添加 "InvoiceItems"，允许绑定商品列表
        public async Task<IActionResult> Edit(int id, [Bind("InvoiceId,InvoiceNumber,Status,Notes,TotalAmount,TaxAmount,ClientId,IssueDate,DueDate,InvoiceItems")] Invoice invoice)
        {
            if (id != invoice.InvoiceId) return NotFound();

            // ✅ 修复点：移除对 Items 细节的校验，因为我们会在下面手动处理
            ModelState.Remove("InvoiceItems");

            // 4. 读取数据库中的原始数据（为了追踪更新）
            var dbInvoice = await _context.Invoices
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (dbInvoice == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // 5. 更新基本字段
                    dbInvoice.InvoiceNumber = invoice.InvoiceNumber;
                    dbInvoice.Status = invoice.Status;
                    dbInvoice.Notes = invoice.Notes;
                    dbInvoice.ClientId = invoice.ClientId;
                    dbInvoice.IssueDate = invoice.IssueDate;
                    dbInvoice.DueDate = invoice.DueDate;

                    // 6. 处理 InvoiceItems (核心逻辑)
                    // 策略：清空旧的 -> 重新添加新的
                    if (dbInvoice.InvoiceItems != null)
                    {
                        _context.InvoiceItems.RemoveRange(dbInvoice.InvoiceItems);
                    }

                    dbInvoice.InvoiceItems = new List<InvoiceItem>();
                    decimal calculatedTotal = 0;

                    // 遍历前端提交进来的 invoice.InvoiceItems
                    if (invoice.InvoiceItems != null && invoice.InvoiceItems.Count > 0)
                    {
                        foreach (var itemInput in invoice.InvoiceItems)
                        {
                            // 即使前端只传了 ItemId 和 Quantity，我们需要去数据库查单价
                            var dbItem = await _context.Items.FindAsync(itemInput.ItemId);
                            if (dbItem != null)
                            {
                                var lineTotal = dbItem.UnitPrice * itemInput.Quantity;

                                // 创建新的实体加入列表
                                dbInvoice.InvoiceItems.Add(new InvoiceItem
                                {
                                    ItemId = itemInput.ItemId,
                                    Quantity = itemInput.Quantity,
                                    UnitPrice = dbItem.UnitPrice, // 使用最新单价
                                    Total = lineTotal
                                });
                                calculatedTotal += lineTotal;
                            }
                        }
                    }

                    // 7. 重新计算总金额
                    dbInvoice.TotalAmount = calculatedTotal;
                    dbInvoice.TaxAmount = calculatedTotal * 0.09m; // 假设税率 9%

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

            // 失败回滚
            ViewData["ClientId"] = new SelectList(_context.Clients, "ClientId", "Name", invoice.ClientId);
            ViewBag.ItemList = _context.Items.ToList();
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