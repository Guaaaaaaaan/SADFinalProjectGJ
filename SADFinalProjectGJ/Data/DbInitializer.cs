using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SADFinalProjectGJ.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SADFinalProjectGJ.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            // 1. 获取必要的服务
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // 确保数据库已创建
            await context.Database.MigrateAsync();

            // ============================================================
            // 2. 初始化角色 (Roles)
            // ============================================================
            string[] roleNames = { "Admin", "FinanceStaff", "Client" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // ============================================================
            // 3. 初始化用户 (Users)
            // ============================================================

            // A. 创建 Admin 用户
            string adminEmail = "admin@igpts.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            // 1. 如果用户不存在，先创建
            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(adminUser, "Password123!");

                // 如果创建失败（比如密码太简单），直接停止后续操作，防止报错
                if (!result.Succeeded) return;
            }


            // 2. 此时 adminUser 一定不为 null 了。
            //    不管他是刚创建的，还是老用户，只要没角色，就加上。
            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            string staffEmail = "staff@igpts.com";
            var staffUser = await userManager.FindByEmailAsync(staffEmail);
            // B. 创建 Finance Staff 用户
            if (staffUser == null)
            {
                staffUser = new IdentityUser
                {
                    UserName = "staff",
                    Email = "staff@igpts.com",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(staffUser, "Password123!");

                if (!result.Succeeded) return;
            }

            if (!await userManager.IsInRoleAsync(staffUser, "FinanceStaff"))
            {
                await userManager.AddToRoleAsync(staffUser, "FinanceStaff");
            }

            // C. 创建 Client 用户 (用于登录查看发票)

            string clientEmail = "client@igpts.com";
            var clientUser = await userManager.FindByEmailAsync(clientEmail);

            if (clientUser == null)
            {
                clientUser = new IdentityUser
                {
                    UserName = "demo_client",
                    Email = clientEmail,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(clientUser, "Password123!");
                if (!result.Succeeded) return; 
            }

            // 确保有 Client 角色
            if (!await userManager.IsInRoleAsync(clientUser, "Client"))
            {
                await userManager.AddToRoleAsync(clientUser, "Client");
            }

            // ============================================================
            // 4. 初始化业务数据 (如果数据库是空的)
            // ============================================================

            // A. 添加商品 (Items)
            if (!context.Items.Any())
            {
                var items = new Item[]
                {
                    new Item { Description = "Web Design Service", UnitPrice = 1500.00m },
                    new Item { Description = "Cloud Hosting (Yearly)", UnitPrice = 240.00m },
                    new Item { Description = "SEO Consultation", UnitPrice = 500.00m },
                    new Item { Description = "Software Maintenance", UnitPrice = 100.00m }
                };
                context.Items.AddRange(items);
                await context.SaveChangesAsync();
            }

            // B. 添加系统设置 (System Settings)
            if (!context.SystemSettings.Any())
            {
                context.SystemSettings.Add(new SystemSetting { Key = "GstRate", Value = "9", Description = "Default GST Rate (%)" });
                await context.SaveChangesAsync();
            }

            // C. 添加客户档案 (Client Profile) - 关联到上面的 Client User
            if (!context.Clients.Any() && clientUser != null)
            {
                var clientProfile = new Client
                {
                    Name = "John Doe",
                    CompanyName = "Tech Solutions Ltd.",
                    AccountEmail = "client@igpts.com", // 必须和 User Email 一致方便发邮件
                    Phone = "9123 4567",
                    Address = "10 Marina Bay, Singapore",
                    UserId = clientUser.Id // 🔥 关键：关联到 IdentityUser
                };
                context.Clients.Add(clientProfile);
                await context.SaveChangesAsync(); // 保存以获取 ClientId

                // D. 添加发票 (Invoices) - 只有有了 Client 才能加发票
                if (!context.Invoices.Any())
                {
                    // 1. 一张已支付的发票 (Paid)
                    var invoice1 = new Invoice
                    {
                        InvoiceNumber = "INV-20231001-001",
                        ClientId = clientProfile.ClientId,
                        IssueDate = DateTime.Now.AddMonths(-2),
                        DueDate = DateTime.Now.AddMonths(-2).AddDays(14),
                        Status = InvoiceStatus.Paid,
                        TotalAmount = 1740.00m,
                        TaxAmount = 156.60m, // 9%
                        InvoiceItems = new List<InvoiceItem>
                        {
                            new InvoiceItem { ItemId = context.Items.First(i => i.Description == "Web Design Service").ItemId, Quantity = 1, UnitPrice = 1500.00m, Total = 1500.00m },
                            new InvoiceItem { ItemId = context.Items.First(i => i.Description == "Cloud Hosting (Yearly)").ItemId, Quantity = 1, UnitPrice = 240.00m, Total = 240.00m }
                        }
                    };

                    // 2. 一张逾期的发票 (Overdue)
                    var invoice2 = new Invoice
                    {
                        InvoiceNumber = "INV-20231115-002",
                        ClientId = clientProfile.ClientId,
                        IssueDate = DateTime.Now.AddMonths(-1),
                        DueDate = DateTime.Now.AddDays(-5), // 5天前到期
                        Status = InvoiceStatus.Overdue,
                        TotalAmount = 500.00m,
                        TaxAmount = 45.00m,
                        InvoiceItems = new List<InvoiceItem>
                        {
                            new InvoiceItem { ItemId = context.Items.First(i => i.Description == "SEO Consultation").ItemId, Quantity = 1, UnitPrice = 500.00m, Total = 500.00m }
                        }
                    };

                    // 3. 一张草稿发票 (Draft)
                    var invoice3 = new Invoice
                    {
                        InvoiceNumber = "INV-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                        ClientId = clientProfile.ClientId,
                        IssueDate = DateTime.Now,
                        DueDate = DateTime.Now.AddDays(14),
                        Status = InvoiceStatus.Draft,
                        TotalAmount = 100.00m,
                        TaxAmount = 9.00m,
                        InvoiceItems = new List<InvoiceItem>
                        {
                            new InvoiceItem { ItemId = context.Items.First(i => i.Description == "Software Maintenance").ItemId, Quantity = 1, UnitPrice = 100.00m, Total = 100.00m }
                        }
                    };

                    context.Invoices.AddRange(invoice1, invoice2, invoice3);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}