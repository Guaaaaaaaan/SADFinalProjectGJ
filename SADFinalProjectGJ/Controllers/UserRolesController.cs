using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SADFinalProjectGJ.Data;
using SADFinalProjectGJ.Models; // 确保引用你的模型命名空间
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SADFinalProjectGJ.Controllers
{
    // 只有属于 "Admin" 角色的用户才能访问此控制器
    // 如果你还没有创建这个角色，可以先注释掉这个属性，等角色创建好后再开启
    [Authorize(Roles = "Admin")]
    public class UserRolesController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context; // <--- 必须有这个

        public UserRolesController(UserManager<IdentityUser> userManager,
                                   RoleManager<IdentityRole> roleManager,
                                   ApplicationDbContext context) // <--- 构造函数里也要有
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // 显示所有用户及其角色的列表
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRolesViewModel = new List<UserRolesViewModel>();

            foreach (var user in users)
            {
                var thisViewModel = new UserRolesViewModel();
                thisViewModel.UserId = user.Id;
                thisViewModel.Email = user.Email;
                thisViewModel.UserName = user.UserName;
                thisViewModel.Roles = await _userManager.GetRolesAsync(user);
                userRolesViewModel.Add(thisViewModel);
            }
            return View(userRolesViewModel);
        }

        // 管理特定用户的角色 (GET)
        public async Task<IActionResult> Manage(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return View("Error");
            }

            var model = new List<ManageUserRolesViewModel>();
            foreach (var role in _roleManager.Roles)
            {
                var userRolesViewModel = new ManageUserRolesViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name
                };

                // 判断用户当前是否拥有该角色
                if (await _userManager.IsInRoleAsync(user, role.Name))
                {
                    userRolesViewModel.Selected = true;
                }
                else
                {
                    userRolesViewModel.Selected = false;
                }
                model.Add(userRolesViewModel);
            }

            ViewBag.UserId = userId;
            ViewBag.UserName = user.UserName;
            return View(model);
        }

        // 管理特定用户的角色 (POST)
        [HttpPost]
        public async Task<IActionResult> Manage(List<ManageUserRolesViewModel> model, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return View("Error");
            }

            // --- 修改开始：添加冲突检测逻辑 ---

            // 1. 获取用户在界面上勾选的所有角色名
            var selectedRoles = model.Where(x => x.Selected).Select(y => y.RoleName).ToList();

            // ================== 【修改部分开始】 ==================
            // 原有的代码只检查了 Client 和 FinanceStaff，现在替换为检查三个角色

            // 定义所有互斥的角色
            var exclusiveRoles = new List<string> { "Admin", "FinanceStaff", "Client" };

            // 计算当前选中的角色中，有几个是属于互斥组的
            int conflictCount = selectedRoles.Count(r => exclusiveRoles.Contains(r));

            // 如果选中的互斥角色超过 1 个，就报错
            if (conflictCount > 1)
            {
                // 添加错误信息
                ModelState.AddModelError("", "角色冲突：Admin, FinanceStaff 和 Client 只能选择其中一个，不能多选。");

                // 重新填充 ViewBag (否则页面用户名会丢失)
                ViewBag.UserId = userId;
                ViewBag.UserName = user.UserName;

                // 返回页面，阻止保存
                return View(model);
            }
            // ================== 【修改部分结束】 ==================

            // 下面是原有的保存逻辑，保持不变
            var roles = await _userManager.GetRolesAsync(user);
            var result = await _userManager.RemoveFromRolesAsync(user, roles); // 先移除所有旧角色

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot remove user existing roles");
                return View(model);
            }

            // 添加选中的新角色
            result = await _userManager.AddToRolesAsync(user, selectedRoles);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot add selected roles to user");
                return View(model);
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string userId)
        {
            // 1. 查找用户
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // 2. 防止自杀 (Admin 不能删自己)
            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Error"] = "操作失败：你不能删除自己的账号！";
                return RedirectToAction("Index");
            }

            // ==========================================
            // 3. 安全检测：检查是否有外键关联数据
            // ==========================================

            // 检查这个用户是否是一个 "Client" (在 Clients 表里有记录)
            // 并且检查这个 Client 是否有关联的发票
            var client = await _context.Clients
                .Include(c => c.Invoices) // 顺便查一下发票
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client != null)
            {
                // 情况 A: 这是一个客户，而且他有发票
                if (client.Invoices != null && client.Invoices.Any())
                {
                    TempData["Error"] = $"无法删除！该用户是一位客户，且名下有 {client.Invoices.Count} 张发票。请先删除这些发票，或归档处理。";
                    return RedirectToAction("Index");
                }

                // 情况 B: 这是一个客户，虽然没发票，但 Clients 表里有他的档案
                // 如果你希望连空档案也不让删，就用下面这句：
                TempData["Error"] = "无法删除！该用户在系统中拥有客户档案 (Client Profile)。请先在 Clients 列表中删除其档案。";
                return RedirectToAction("Index");
            }

            // ==========================================
            // 4. 如果没有关联数据，执行删除
            // ==========================================
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "用户已成功删除。";
            }
            else
            {
                // 显示具体的错误信息
                TempData["Error"] = "删除失败：" + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction("Index");
        }
    }
}