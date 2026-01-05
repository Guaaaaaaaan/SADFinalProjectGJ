using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using SADFinalProjectGJ.Data;
using SADFinalProjectGJ.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SADFinalProjectGJ.Services
{
    public class InvoiceReminderService : BackgroundService
    {
        // 我们需要 IServiceProvider 来手动获取数据库连接（因为后台任务是单例的，而数据库是范围例的）
        private readonly IServiceProvider _serviceProvider;

        public InvoiceReminderService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 只要系统没关闭，就一直循环
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // === 🕒 定时任务开始 ===
                    await CheckOverdueInvoices();
                }
                catch (Exception ex)
                {
                    // 即使报错也不要让后台任务崩溃，记录一下就行
                    Console.WriteLine($"Background Job Error: {ex.Message}");
                }

                // === 💤 休息时间 ===
                // 为了演示方便，我们设置为每 1 分钟检查一次
                // 如果是正式上线，这里通常是 Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task CheckOverdueInvoices()
        {
            // 创建一个新的作用域 (Scope) 来使用数据库
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                // 1. 查找所有“已过期”且“未支付”的发票
                // 条件：DueDate < 现在，且状态既不是 Paid 也不是 Overdue
                var overdueInvoices = await context.Invoices
                    .Include(i => i.Client) // 必须带上 Client 才能发邮件
                    .Where(i => i.DueDate < DateTime.Now
                                && i.Status != InvoiceStatus.Paid
                                && i.Status != InvoiceStatus.Overdue)
                            //Change from string to Enum
                            //&& i.Status != "Paid"
                            //&& i.Status != "Overdue")
                                .ToListAsync();

                if (overdueInvoices.Any())
                {
                    foreach (var invoice in overdueInvoices)
                    {
                        // A. 更新状态为 "Overdue"
                        //Change from string to Enum
                        //invoice.Status = "Overdue";
                        invoice.Status = InvoiceStatus.Overdue;

                        // B. 发送催款邮件
                        if (invoice.Client != null && !string.IsNullOrEmpty(invoice.Client.AccountEmail))
                        {
                            string subject = $"URGENT: Invoice {invoice.InvoiceNumber} is Overdue";
                            string body = $"Dear {invoice.Client.Name},<br/><br/>" +
                                          $"This is a reminder that your invoice <b>{invoice.InvoiceNumber}</b> was due on {invoice.DueDate:d}.<br/>" +
                                          $"Please arrange payment of {invoice.TotalAmount:C} immediately.<br/><br/>" +
                                          $"Thank you.";

                            await emailService.SendEmailAsync(invoice.Client.AccountEmail, subject, body);

                            // C. 记录通知历史
                            context.Notifications.Add(new Notification
                            {
                                RecipientEmail = invoice.Client.AccountEmail,
                                Subject = subject,
                                Message = $"Overdue reminder sent for {invoice.InvoiceNumber}",
                                SentDate = DateTime.Now,
                                Status = "Sent",
                                UserId = invoice.Client.UserId
                            });

                            Console.WriteLine($"[Auto-Reminder] Processed Invoice {invoice.InvoiceNumber}");
                        }
                    }

                    // 统一保存数据库更改
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}