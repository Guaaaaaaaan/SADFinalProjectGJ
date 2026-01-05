using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SADFinalProjectGJ.Models;

namespace SADFinalProjectGJ.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }
        // --- 下面这些是你之前缺失的部分 ---
        // 告诉数据库：我们需要这些业务表！

        public DbSet<Client> Clients { get; set; }
        public DbSet<FinanceStaff> FinanceStaffs { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<PaymentGateway> PaymentGateways { get; set; }
        // NEW!!!
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        // 可选：在 OnModelCreating 里种子化一条默认税率数据，防止第一次运行报错
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Invoice 状态枚举转换为字符串存储
            builder.Entity<Invoice>()
                .Property(i => i.Status)
                .HasConversion<string>();

            // GstRate 系统设置的种子数据
            builder.Entity<SystemSetting>().HasData(
                new SystemSetting { Id = 1, Key = "GstRate", Value = "9", Description = "Default GST Rate (%)" }
            );
        }
    }
}
