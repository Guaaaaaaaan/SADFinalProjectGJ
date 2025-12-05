using Microsoft.AspNetCore.Identity; // 引用 Identity
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SADFinalProjectGJ.Models // 修正拼写
{
    public class Client
    {
        [Key]
        public int ClientId { get; set; }

        // 修改1：使用 string 类型来匹配 IdentityUser 的 ID
        public string? UserId { get; set; }

        // ✅ 新增：把 Client 数据和登录账号关联起来
        // 这里存 Client 的登录邮箱 (因为它比较直观，注册时也好填)
        public string? AccountEmail { get; set; }
        // 修改2：添加导航属性，建立外键关联
        [ForeignKey("UserId")]
        public IdentityUser? User { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(100)]
        public string CompanyName { get; set; }

        [StringLength(20)] // 电话号码通常不会超过20位
        public string Phone { get; set; }

        [StringLength(255)] // 地址可以长一点
        public string Address { get; set; }

        public ICollection<Invoice>? Invoices { get; set; }
        public ICollection<Notification>? Notifications { get; set; }
    }
}