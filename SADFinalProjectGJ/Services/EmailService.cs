using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration; // 需要引用这个
using Microsoft.Extensions.Options; // 需要这个命名空间
using SADFinalProjectGJ.Configuration;
using System.Threading.Tasks;

namespace SADFinalProjectGJ.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        // 构造函数注入 IOptions<EmailSettings>
        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            // Testing code
            //throw new Exception("我是故意来测试 try-catch 的！");

            var emailMessage = new MimeMessage();

            // 发件人信息 (显示名字, 邮箱地址)
            // 这里的配置我们会放在 appsettings.json 里

            //var senderName = _configuration["EmailSettings:SenderName"];
            //var senderEmail = _configuration["EmailSettings:SenderEmail"];

            // 现在可以直接用属性了，不仅有代码提示，还不用担心拼写错误
            emailMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = subject;

            // 邮件正文 (支持 HTML)
            var bodyBuilder = new BodyBuilder { HtmlBody = message };
            emailMessage.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(
                    _emailSettings.Host,
                    _emailSettings.Port,
                    MailKit.Security.SecureSocketOptions.StartTls
                );

                await client.AuthenticateAsync(
                    _emailSettings.UserName,
                    _emailSettings.Password
                );

                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
        }
    }
}