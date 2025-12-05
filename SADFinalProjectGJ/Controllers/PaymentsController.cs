using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SADFinalProjectGJ.Data;
using SADFinalProjectGJ.Models;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SADFinalProjectGJ.Controllers
{
    [Authorize]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PaymentsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Payments
        public async Task<IActionResult> Index()
        {
            var query = _context.Payments
                .Include(p => p.Invoice)
                .ThenInclude(i => i.Client)
                .AsQueryable();

            // ✅ 1. Client 只能看到自己的付款记录
            if (User.IsInRole("Client"))
            {
                var currentUserId = _userManager.GetUserId(User);
                query = query.Where(p => p.Invoice.Client.UserId == currentUserId);
            }

            return View(await query.ToListAsync());
        }

        // GET: Payments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _context.Payments
                .Include(p => p.Invoice)
                .ThenInclude(i => i.Client) // 必须加载 Client 信息才能判断权限
                .FirstOrDefaultAsync(m => m.PaymentId == id);

            if (payment == null) return NotFound();

            // ================== 【新增安全检查】 ==================
            // 如果当前用户是 Client，检查这笔付款是否属于他
            if (User.IsInRole("Client"))
            {
                var currentUserId = _userManager.GetUserId(User);
                if (payment.Invoice.Client.UserId != currentUserId)
                {
                    return Forbid(); // 或者 return NotFound(); 防止用户通过试探 ID 猜测数据
                }
            }
            // ================== 【检查结束】 ==================

            return View(payment);
        }

        // GET: Payments/Create
        // ✅ 2. 接收 invoiceId，准备付款页面
        public async Task<IActionResult> Create(int? invoiceId)
        {
            if (invoiceId == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null)
            {
                return NotFound();
            }

            // ✅ 安全检查：如果是 Client，必须确保这发票是他的
            if (User.IsInRole("Client"))
            {
                var currentUserId = _userManager.GetUserId(User);
                if (invoice.Client.UserId != currentUserId)
                {
                    return Forbid();
                }
            }

            // ✅ 自动填充：创建一个 Payment 对象，把发票ID和金额填好传给页面
            var payment = new Payment
            {
                InvoiceId = invoice.InvoiceId,
                // 金额 = 总额 + 税
                Amount = invoice.TotalAmount + invoice.TaxAmount,
                PaymentDate = DateTime.Now,
                Method = "Credit Card", // 默认选项
                Status = "Completed"    // 模拟直接支付成功
            };

            // 为了显示发票号给用户看 (ViewBag)
            ViewBag.InvoiceNumber = invoice.InvoiceNumber;

            return View(payment);
        }

        // POST: Payments/Create with Stripe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int invoiceId) // 注意参数变简单了
        {
            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null) return NotFound();

            // 1. 设置 Stripe 支付参数
            var domain = "https://localhost:7287"; // IMPORTANT: 这里一定要改成你运行时的实际端口号

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
        {
            new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)((invoice.TotalAmount + invoice.TaxAmount) * 100), // Stripe 金额单位是分
                    Currency = "sgd", // 货币单位
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = $"Invoice #{invoice.InvoiceNumber}",
                    },
                },
                Quantity = 1,
            },
        },
                Mode = "payment",
                // 支付成功后跳回的地址
                SuccessUrl = domain + $"/Payments/PaymentSuccess?invoiceId={invoice.InvoiceId}&session_id={{CHECKOUT_SESSION_ID}}",
                // 取消支付跳回的地址
                CancelUrl = domain + $"/Invoices/Details/{invoice.InvoiceId}",
            };

            var service = new SessionService();
            Session session = service.Create(options);

            // 2. 跳转到 Stripe
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        // GET: Payments/PaymentSuccess
        public async Task<IActionResult> PaymentSuccess(int invoiceId, string session_id)
        {
            // 1. 简单检查参数
            if (invoiceId == 0 || string.IsNullOrEmpty(session_id))
            {
                return BadRequest("Invalid payment information.");
            }

            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null) return NotFound();

            // 2. 检查是否已经保存过这个 TransactionId，防止刷新页面重复扣款
            var existingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.TransactionId == session_id);

            if (existingPayment == null)
            {
                // 3. 创建本地 Payment 记录
                var payment = new Payment
                {
                    InvoiceId = invoiceId,
                    Amount = invoice.TotalAmount + invoice.TaxAmount,
                    PaymentDate = DateTime.Now,
                    Method = "Stripe",
                    Status = "Completed",
                    TransactionId = session_id // 保存 Stripe Session ID
                };
                _context.Add(payment);

                // 4. 更新发票状态为 Paid
                invoice.Status = "Paid";
                _context.Update(invoice);

                await _context.SaveChangesAsync();
            }

            // 5. 显示成功页面
            return View("Success");
        }
        // GET: Payments/Delete/5
        [Authorize(Roles = "Admin,FinanceStaff")] // 只有管理员能删付款记录
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _context.Payments
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(m => m.PaymentId == id);
            if (payment == null) return NotFound();

            return View(payment);
        }

        // POST: Payments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,FinanceStaff")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.PaymentId == id);
        }
    }
}