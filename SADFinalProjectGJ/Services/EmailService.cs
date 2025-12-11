using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration; // 需要引用这个
using System.Threading.Tasks;

namespace SADFinalProjectGJ.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var emailMessage = new MimeMessage();

            // 发件人信息 (显示名字, 邮箱地址)
            // 这里的配置我们会放在 appsettings.json 里
            var senderName = _configuration["EmailSettings:SenderName"];
            var senderEmail = _configuration["EmailSettings:SenderEmail"];

            emailMessage.From.Add(new MailboxAddress(senderName, senderEmail));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = subject;

            // 邮件正文 (支持 HTML)
            var bodyBuilder = new BodyBuilder { HtmlBody = message };
            emailMessage.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                // 连接 Gmail SMTP 服务器
                // Host: smtp.gmail.com, Port: 587, UseSsl: false (但在 StartTls 模式下会加密)
                await client.ConnectAsync(
                    _configuration["EmailSettings:Host"],
                    int.Parse(_configuration["EmailSettings:Port"]),
                    MailKit.Security.SecureSocketOptions.StartTls
                );

                // 认证
                await client.AuthenticateAsync(
                    _configuration["EmailSettings:UserName"],
                    _configuration["EmailSettings:Password"] // 这里填刚才生成的 App Password
                );

                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
        }
    }
}