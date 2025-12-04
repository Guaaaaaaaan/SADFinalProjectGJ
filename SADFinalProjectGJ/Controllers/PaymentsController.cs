using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SADFinalProjectGJ.Data;
using SADFinalProjectGJ.Models;

namespace SADFinalProjectGJ.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentsController(ApplicationDbContext context)
        {
            _context = context;
        }
        public class PaymentIntentRequest
        {
            public int InvoiceId { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] PaymentIntentRequest request)
        {
            // 1. 安全检查：去数据库查这张 Invoice 到底多少钱，不要相信前端传来的金额
            var invoice = await _context.Invoices.FindAsync(request.InvoiceId);

            if (invoice == null)
            {
                return BadRequest(new { error = "找不到指定的账单 (Invoice not found)" });
            }

            // 假设你的 Invoice 模型里有一个叫 Amount 或 Total 的字段，类型是 decimal
            // Stripe 需要的是“分” (cents)，且必须是 long 类型
            // 例如：10.50 元 -> 1050 分
            // 请根据你 Invoice 实际的字段名修改 invoice.Amount
            long amountInCents = (long)(invoice.TotalAmount * 100);

            var options = new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = "sgd", // 这里的货币要和你的业务一致
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                },
                // 元数据：可以在 Stripe 后台看到这笔钱是付哪张 Invoice 的
                Metadata = new Dictionary<string, string>
                {
                    { "InvoiceId", invoice.InvoiceId.ToString() },
                    { "InvoiceNumber", invoice.InvoiceNumber }
                }
            };

            var service = new PaymentIntentService();
            try
            {
                var intent = service.Create(options);
                // 返回 ClientSecret 给前端
                return Ok(new { clientSecret = intent.ClientSecret });
            }
            catch (StripeException e)
            {
                return BadRequest(new { error = e.Message });
            }
        }
        // ==========================================
        //  【新增部分结束】
        // ==========================================

        // GET: Payments
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Payments.Include(p => p.Invoice).Include(p => p.PaymentGateway);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Payments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Invoice)
                .Include(p => p.PaymentGateway)
                .FirstOrDefaultAsync(m => m.PaymentId == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // GET: Payments/Create
        public IActionResult Create()
        {
            ViewData["InvoiceId"] = new SelectList(_context.Invoices, "InvoiceId", "InvoiceNumber");
            ViewData["GatewayName"] = new SelectList(_context.PaymentGateways, "GatewayName", "GatewayName");
            return View();
        }

        // POST: Payments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PaymentId,InvoiceId,PaymentDate,Amount,Method,TransactionId,Status,GatewayName")] Payment payment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(payment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["InvoiceId"] = new SelectList(_context.Invoices, "InvoiceId", "InvoiceNumber", payment.InvoiceId);
            ViewData["GatewayName"] = new SelectList(_context.PaymentGateways, "GatewayName", "GatewayName", payment.GatewayName);
            return View(payment);
        }

        // GET: Payments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                return NotFound();
            }
            ViewData["InvoiceId"] = new SelectList(_context.Invoices, "InvoiceId", "InvoiceNumber", payment.InvoiceId);
            ViewData["GatewayName"] = new SelectList(_context.PaymentGateways, "GatewayName", "GatewayName", payment.GatewayName);
            return View(payment);
        }

        // POST: Payments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PaymentId,InvoiceId,PaymentDate,Amount,Method,TransactionId,Status,GatewayName")] Payment payment)
        {
            if (id != payment.PaymentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(payment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PaymentExists(payment.PaymentId))
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
            ViewData["InvoiceId"] = new SelectList(_context.Invoices, "InvoiceId", "InvoiceNumber", payment.InvoiceId);
            ViewData["GatewayName"] = new SelectList(_context.PaymentGateways, "GatewayName", "GatewayName", payment.GatewayName);
            return View(payment);
        }

        // GET: Payments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Invoice)
                .Include(p => p.PaymentGateway)
                .FirstOrDefaultAsync(m => m.PaymentId == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // POST: Payments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
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
