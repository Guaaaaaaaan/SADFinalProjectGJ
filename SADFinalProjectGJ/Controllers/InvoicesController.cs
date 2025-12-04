using SADFinalProjectGJ.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SADFinalProjectGJ.Data;
using SADFinalProjectGJ.Models;


namespace SADFinalProjectGJ.Controllers
{
    public class InvoicesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InvoicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Invoices
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Invoices.Include(i => i.Client);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Invoices/Details/5
        // GET: Invoices/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices
                .Include(i => i.Client) // 读取客户信息
                .Include(i => i.InvoiceItems)! // ✅ 新增：读取发票里的商品列表
                    .ThenInclude(ii => ii.Item) // ✅ 新增：顺便把商品的名字/单价也读出来
                .FirstOrDefaultAsync(m => m.InvoiceId == id);

            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }
        public IActionResult Create()
        {
            // 1. 把所有客户取出来，放到下拉菜单里
            ViewData["ClientId"] = new SelectList(_context.Clients, "ClientId", "Name");

            // 2. 把所有商品取出来，告诉网页有哪些商品可选 (这是为了给 JavaScript 用)
            ViewBag.ItemList = _context.Items.ToList();

            // 3. 传一个空的 ViewModel 给页面
            return View(new InvoiceCreateViewModel());
        }

        // POST: Invoices/Create
        // 这个方法负责接收数据并保存
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InvoiceCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                // A. 先创建发票的“头” (Header)
                var invoice = new Invoice
                {
                    // 自动生成发票号：INV-年月日-时分秒 (例如 INV-20251204-103000)
                    InvoiceNumber = "INV-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                    ClientId = model.ClientId,
                    IssueDate = DateTime.Now,
                    DueDate = model.DueDate,
                    Status = "Draft", // 默认状态是草稿
                    InvoiceItems = new List<InvoiceItem>() // 准备好装商品的篮子
                };

                decimal calculatedTotal = 0;

                // B. 循环处理用户提交的每一个商品
                if (model.Items != null && model.Items.Count > 0)
                {
                    foreach (var entry in model.Items)
                    {
                        // 安全检查：去数据库查最新的价格 (防止用户在前端篡改价格)
                        var dbItem = await _context.Items.FindAsync(entry.ItemId);

                        if (dbItem != null)
                        {
                            var lineTotal = dbItem.UnitPrice * entry.Quantity; // 单价 x 数量

                            var invoiceItem = new InvoiceItem
                            {
                                ItemId = entry.ItemId,
                                Quantity = entry.Quantity,
                                UnitPrice = dbItem.UnitPrice, // 存入当时的价格
                                Total = lineTotal
                            };

                            invoice.InvoiceItems.Add(invoiceItem); // 放进篮子
                            calculatedTotal += lineTotal; // 累加总金额
                        }
                    }
                }

                // C. 最后填入总金额和税
                invoice.TotalAmount = calculatedTotal;
                invoice.TaxAmount = calculatedTotal * 0.09m; // 假设 9% GST

                // D. 保存到数据库
                _context.Add(invoice);
                await _context.SaveChangesAsync();

                // 成功后跳转到列表页
                return RedirectToAction(nameof(Index));
            }

            // 如果失败 (比如没选客户)，重新加载数据返回页面
            ViewData["ClientId"] = new SelectList(_context.Clients, "ClientId", "Name", model.ClientId);
            ViewBag.ItemList = _context.Items.ToList();
            return View(model);
        }
        // GET: Invoices/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }
            ViewData["ClientId"] = new SelectList(_context.Clients, "ClientId", "Name", invoice.ClientId);
            return View(invoice);
        }

        // POST: Invoices/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("InvoiceId,InvoiceNumber,Status,Notes,TotalAmount,TaxAmount,ClientId,IssueDate,DueDate")] Invoice invoice)
        {
            if (id != invoice.InvoiceId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(invoice);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InvoiceExists(invoice.InvoiceId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClientId"] = new SelectList(_context.Clients, "ClientId", "Name", invoice.ClientId);
            return View(invoice);
        }

        // GET: Invoices/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .FirstOrDefaultAsync(m => m.InvoiceId == id);
            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        // POST: Invoices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
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
