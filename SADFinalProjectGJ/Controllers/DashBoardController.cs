using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // 1. 引用 Identity
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SADFinalProjectGJ.Data;
using SADFinalProjectGJ.Models;      // 引用 Models (用于访问 Client 和 Invoice 实体)
using SADFinalProjectGJ.ViewModels;

namespace SADFinalProjectGJ.Controllers
{
    [Authorize]
    public class DashBoardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager; // 2. 添加 UserManager

        // 3. 在构造函数中注入 UserManager
        public DashBoardController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel();

            // 4. 获取当前登录用户
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "Identity" });

            // 5. 检查用户角色
            bool isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            bool isFinance = await _userManager.IsInRoleAsync(user, "FinanceStaff");
            bool isClient = await _userManager.IsInRoleAsync(user, "Client");

            // 6. 准备基础查询 (IQueryable)
            // 这样我们可以在后面根据条件给这些查询追加 Where 子句
            var invoiceQuery = _context.Invoices.AsQueryable();
            var clientQuery = _context.Clients.AsQueryable();

            // 7. 如果是 Client 且不是管理员/财务 (也就是普通客户)
            // 则只看自己的数据
            if (isClient && !isAdmin && !isFinance)
            {
                // 根据登录用户的 ID (UserId) 找到对应的 Client 档案
                var clientProfile = await _context.Clients
                    .FirstOrDefaultAsync(c => c.UserId == user.Id);

                if (clientProfile != null)
                {
                    // 过滤：只查该客户 ID 的发票
                    invoiceQuery = invoiceQuery.Where(i => i.ClientId == clientProfile.ClientId);
                    
                    // 过滤：只查该客户自己 (用于统计客户总数时显示 1)
                    clientQuery = clientQuery.Where(c => c.ClientId == clientProfile.ClientId);
                }
                else
                {
                    // 特殊情况：拥有 Client 角色但还没有 Client 档案
                    // 强制查询为空，避免看到别人的数据
                    invoiceQuery = invoiceQuery.Where(i => false);
                    clientQuery = clientQuery.Where(c => false);
                }
            }
            // 如果是 Admin 或 FinanceStaff，不加过滤条件，默认查询所有

            // 8. 执行统计 (针对过滤后的数据)

            // 算总账
            // 注意：SumAsync 对空集合会返回 0，无需额外的 Any() 判断，但也可以保留
            model.TotalRevenue = await invoiceQuery
                .Select(i => i.TotalAmount + i.TaxAmount)
                .SumAsync();

            // 算有多少张 Draft 或 Sent 的发票
            model.PendingInvoicesCount = await invoiceQuery
                .CountAsync(i => i.Status == "Draft" || i.Status == "Sent");

            // 算有多少客户 (Client 只能看到 1 个，管理员看到所有)
            model.TotalClientsCount = await clientQuery.CountAsync();

            // 查出最近的 5 张发票
            model.RecentInvoices = await invoiceQuery
                .Include(i => i.Client)
                .OrderByDescending(i => i.IssueDate)
                .Take(5)
                .ToListAsync();

            return View(model);
        }
    }
}