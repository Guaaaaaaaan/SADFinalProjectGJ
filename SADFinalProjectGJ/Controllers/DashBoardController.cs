using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SADFinalProjectGJ.Data;
using SADFinalProjectGJ.ViewModels; // 引用刚才建的 ViewModel

namespace SADFinalProjectGJ.Controllers
{
    [Authorize] // 保护起来，只有登录才能看
    public class DashBoardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashBoardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. 准备一个空的盒子
            var model = new DashboardViewModel();

            // 2. 算总账 (总额 + 税)
            if (_context.Invoices.Any())
            {
                model.TotalRevenue = await _context.Invoices
                    .SumAsync(i => i.TotalAmount + i.TaxAmount);
            }

            // 3. 算有多少张 Draft 或 Sent 的发票
            model.PendingInvoicesCount = await _context.Invoices
                .CountAsync(i => i.Status == "Draft" || i.Status == "Sent");

            // 4. 算有多少客户
            model.TotalClientsCount = await _context.Clients.CountAsync();

            // 5. 查出最近的 5 张发票
            model.RecentInvoices = await _context.Invoices
                .Include(i => i.Client) // 带上客户名字
                .OrderByDescending(i => i.IssueDate) // 按时间倒序
                .Take(5)
                .ToListAsync();

            // 6. 把盒子交给页面
            return View(model);
        }
    }
}