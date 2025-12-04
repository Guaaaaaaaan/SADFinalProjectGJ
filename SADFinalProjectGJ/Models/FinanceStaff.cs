using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SADFinalProjectGJ.Models // 修正拼写
{
    public class FinanceStaff
    {
        [Key]
        public int StaffId { get; set; }

        // 修改1：使用 string 类型
        public string? UserId { get; set; }

        // 修改2：关联到 IdentityUser
        [ForeignKey("UserId")]
        public IdentityUser? User { get; set; }

        [StringLength(50)]
        public string Department { get; set; }

        public ICollection<Report>? GeneratedReports { get; set; }
    }
}