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
    public class PaymentGatewaysController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentGatewaysController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PaymentGateways
        public async Task<IActionResult> Index()
        {
            return View(await _context.PaymentGateways.ToListAsync());
        }

        // GET: PaymentGateways/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var paymentGateway = await _context.PaymentGateways
                .FirstOrDefaultAsync(m => m.GatewayName == id);
            if (paymentGateway == null)
            {
                return NotFound();
            }

            return View(paymentGateway);
        }

        // GET: PaymentGateways/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: PaymentGateways/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GatewayName,ApiKey")] PaymentGateway paymentGateway)
        {
            if (ModelState.IsValid)
            {
                _context.Add(paymentGateway);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(paymentGateway);
        }

        // GET: PaymentGateways/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var paymentGateway = await _context.PaymentGateways.FindAsync(id);
            if (paymentGateway == null)
            {
                return NotFound();
            }
            return View(paymentGateway);
        }

        // POST: PaymentGateways/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("GatewayName,ApiKey")] PaymentGateway paymentGateway)
        {
            if (id != paymentGateway.GatewayName)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(paymentGateway);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PaymentGatewayExists(paymentGateway.GatewayName))
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
            return View(paymentGateway);
        }

        // GET: PaymentGateways/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var paymentGateway = await _context.PaymentGateways
                .FirstOrDefaultAsync(m => m.GatewayName == id);
            if (paymentGateway == null)
            {
                return NotFound();
            }

            return View(paymentGateway);
        }

        // POST: PaymentGateways/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var paymentGateway = await _context.PaymentGateways.FindAsync(id);
            if (paymentGateway != null)
            {
                _context.PaymentGateways.Remove(paymentGateway);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PaymentGatewayExists(string id)
        {
            return _context.PaymentGateways.Any(e => e.GatewayName == id);
        }
    }
}
