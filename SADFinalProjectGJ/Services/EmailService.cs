using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SADFinalProjectGJ.Configuration;
using System.Threading.Tasks;

namespace SADFinalProjectGJ.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var emailMessage = new MimeMessage();

            // Configure sender and recipient
            emailMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            emailMessage.To.Add(new MailboxAddress("", email));
            emailMessage.Subject = subject;

            // Configure email body (HTML supported)
            var bodyBuilder = new BodyBuilder { HtmlBody = message };
            emailMessage.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                // Connect to the SMTP server
                await client.ConnectAsync(
                    _emailSettings.Host,
                    _emailSettings.Port,
                    MailKit.Security.SecureSocketOptions.StartTls
                );

                // Authenticate
                await client.AuthenticateAsync(
                    _emailSettings.UserName,
                    _emailSettings.Password
                );

                // Send and disconnect
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
        }
    }
}