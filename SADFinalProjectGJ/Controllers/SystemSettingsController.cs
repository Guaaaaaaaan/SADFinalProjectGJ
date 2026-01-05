using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SADFinalProjectGJ.Data;
using SADFinalProjectGJ.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SADFinalProjectGJ.Controllers
{
    [Authorize(Roles = "Admin")] // 只有管理员能改设置
    public class SystemSettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SystemSettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.SystemSettings.ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, string value)
        {
            var setting = await _context.SystemSettings.FindAsync(id);
            if (setting == null) return NotFound();

            setting.Value = value; // 更新值
            _context.Update(setting);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Setting updated successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}