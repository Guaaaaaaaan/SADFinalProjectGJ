using Microsoft.AspNetCore.Identity; // 这一行最关键，不能少
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SADFinalProjectGJ.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        public string? RecipientEmail { get; set; }
        public string? Subject { get; set; }
        public string? Message { get; set; }
        public DateTime SentDate { get; set; }
        public string? Status { get; set; }

        // --- 修复了这里 ---
        // 以前是 int UserId，现在改为 string，因为系统自带的用户ID是字符串
        public string? UserId { get; set; }

        // 以前是 User，现在改为 IdentityUser
        [ForeignKey("UserId")]
        public IdentityUser? User { get; set; }
        // ------------------

        // 如果你还保留了 Invoice 的关联，可以留着下面这两行；如果报错也可以先注释掉
        public int? InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }
    }
}