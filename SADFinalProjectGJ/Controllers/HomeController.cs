using Microsoft.AspNetCore.Mvc;
using SADFinalProjectGJ.Models;
using SADFinalProjectGJ.Services; // 引用你的 Service
using System.Diagnostics;

namespace SADFinalProjectGJ.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IEmailService _emailService; // 1. 添加这一行

        // 2. 修改构造函数，注入 IEmailService
        public HomeController(ILogger<HomeController> logger, IEmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // ==================== ✅ 测试专用方法开始 ====================
        // 访问地址: https://localhost:端口/Home/TestEmail
        public async Task<IActionResult> TestEmail()
        {
            try
            {
                // 这里填你想接收测试邮件的邮箱（比如你自己的私人邮箱）
                // 也可以暂时写死，或者从 appsettings 里读取
                string recipient = "merrick.gjz@gmail.com";
                string subject = "IGPTS System Test Email";
                string message = "<h1>Hello!</h1><p>This is a test email from your School Project.</p>";

                await _emailService.SendEmailAsync(recipient, subject, message);

                return Content($"邮件发送成功！已发送给: {recipient}");
            }
            catch (Exception ex)
            {
                // 如果失败，会把错误信息显示在网页上，方便调试
                return Content($"邮件发送失败: {ex.Message}");
            }
        }
        // ==================== 测试专用方法结束 ====================

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}