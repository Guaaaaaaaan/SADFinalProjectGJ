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
    }
}
