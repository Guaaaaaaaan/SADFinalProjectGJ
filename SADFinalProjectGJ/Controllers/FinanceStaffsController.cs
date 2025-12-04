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
    public class FinanceStaffsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FinanceStaffsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: FinanceStaffs
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.FinanceStaffs.Include(f => f.User);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: FinanceStaffs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var financeStaff = await _context.FinanceStaffs
                .Include(f => f.User)
                .FirstOrDefaultAsync(m => m.StaffId == id);
            if (financeStaff == null)
            {
                return NotFound();
            }

            return View(financeStaff);
        }

        // GET: FinanceStaffs/Create
        public IActionResult Create()
        {
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: FinanceStaffs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StaffId,UserId,Department")] FinanceStaff financeStaff)
        {
            if (ModelState.IsValid)
            {
                _context.Add(financeStaff);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", financeStaff.UserId);
            return View(financeStaff);
        }

        // GET: FinanceStaffs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var financeStaff = await _context.FinanceStaffs.FindAsync(id);
            if (financeStaff == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", financeStaff.UserId);
            return View(financeStaff);
        }

        // POST: FinanceStaffs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StaffId,UserId,Department")] FinanceStaff financeStaff)
        {
            if (id != financeStaff.StaffId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(financeStaff);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FinanceStaffExists(financeStaff.StaffId))
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
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", financeStaff.UserId);
            return View(financeStaff);
        }

        // GET: FinanceStaffs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var financeStaff = await _context.FinanceStaffs
                .Include(f => f.User)
                .FirstOrDefaultAsync(m => m.StaffId == id);
            if (financeStaff == null)
            {
                return NotFound();
            }

            return View(financeStaff);
        }

        // POST: FinanceStaffs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var financeStaff = await _context.FinanceStaffs.FindAsync(id);
            if (financeStaff != null)
            {
                _context.FinanceStaffs.Remove(financeStaff);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool FinanceStaffExists(int id)
        {
            return _context.FinanceStaffs.Any(e => e.StaffId == id);
        }
    }
}
