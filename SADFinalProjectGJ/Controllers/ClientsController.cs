using Microsoft.AspNetCore.Authorization; // ✅ 需要引用
using Microsoft.AspNetCore.Identity;      // ✅ 需要引用
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SADFinalProjectGJ.Data;
using SADFinalProjectGJ.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SADFinalProjectGJ.Controllers
{
    // ✅ 1. 强制登录
    [Authorize]
    public class ClientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        // ✅ 2. 声明 UserManager
        private readonly UserManager<IdentityUser> _userManager;

        // ✅ 3. 注入 UserManager
        public ClientsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Clients
        public async Task<IActionResult> Index()
        {
            var query = _context.Clients.Include(c => c.User).AsQueryable();

            // ✅ 4. Index 逻辑：Client 只能看到自己
            if (User.IsInRole("Client"))
            {
                var currentUserId = _userManager.GetUserId(User);
                // 只显示 UserId 匹配当前登录用户的 Client 档案
                query = query.Where(c => c.UserId == currentUserId);
            }
            // Admin 和 FinanceStaff 不需要过滤，看全部

            return View(await query.ToListAsync());
        }

        // GET: Clients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var client = await _context.Clients
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.ClientId == id);

            if (client == null) return NotFound();

            // ✅ 5. 安全检查：防止 Client 看别人的详情
            if (User.IsInRole("Client"))
            {
                var currentUserId = _userManager.GetUserId(User);
                // 如果这个档案不属于当前登录用户，拒绝访问
                if (client.UserId != currentUserId)
                {
                    return Forbid(); // 或者 return NotFound();
                }
            }

            return View(client);
        }

        // GET: Clients/Create
        // ✅ 6. 只有管理员和财务可以创建新客户 (Client 自己不能创建别的客户)
        [Authorize(Roles = "Admin,FinanceStaff")]
        public IActionResult Create()
        {
            // 这里通常不需要让 Staff 选 UserID，或者是从下拉列表选未绑定的 User
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName"); // 建议显示 UserName 而不是 Id
            return View();
        }

        // POST: Clients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,FinanceStaff")] // ✅ 保护 POST
        public async Task<IActionResult> Create([Bind("ClientId,UserId,Name,CompanyName,Phone,Address")] Client client)
        {
            if (ModelState.IsValid)
            {
                _context.Add(client);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", client.UserId);
            return View(client);
        }

        // GET: Clients/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var client = await _context.Clients.FindAsync(id);
            if (client == null) return NotFound();

            // ✅ 7. 安全检查：Client 只能改自己的资料
            if (User.IsInRole("Client"))
            {
                var currentUserId = _userManager.GetUserId(User);
                if (client.UserId != currentUserId)
                {
                    return Forbid();
                }
            }

            // 注意：通常不建议让用户在 Edit 界面修改绑定的 User 账号，所以下面这一行只对 Admin 有意义
            if (User.IsInRole("Admin") || User.IsInRole("FinanceStaff"))
            {
                ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", client.UserId);
            }

            return View(client);
        }

        // POST: Clients/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ClientId,UserId,Name,CompanyName,Phone,Address")] Client client)
        {
            if (id != client.ClientId) return NotFound();

            // ✅ 8. 二次安全检查
            if (User.IsInRole("Client"))
            {
                var currentUserId = _userManager.GetUserId(User);
                // 必须重新从数据库查一下这个 ID 到底是不是这个人的，防止伪造 POST 请求
                var dbClient = await _context.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.ClientId == id);
                if (dbClient == null || dbClient.UserId != currentUserId)
                {
                    return Forbid();
                }

                // 强制确保 Client 不能把 UserId 改成别人的
                client.UserId = currentUserId;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(client);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClientExists(client.ClientId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            // 如果出错返回页面
            if (User.IsInRole("Admin") || User.IsInRole("FinanceStaff"))
            {
                ViewData["UserId"] = new SelectList(_context.Users, "Id", "UserName", client.UserId);
            }
            return View(client);
        }

        // GET: Clients/Delete/5
        // ✅ 9. 只有 Admin/FinanceStaff 可以删除客户，客户不能删除自己
        [Authorize(Roles = "Admin,FinanceStaff")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var client = await _context.Clients
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.ClientId == id);
            if (client == null) return NotFound();

            return View(client);
        }

        // POST: Clients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,FinanceStaff")] // ✅ 保护 POST
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client != null)
            {
                _context.Clients.Remove(client);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ClientExists(int id)
        {
            return _context.Clients.Any(e => e.ClientId == id);
        }
    }
}