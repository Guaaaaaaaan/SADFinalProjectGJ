using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SADFinalProjectGJ.Models
{
    public class Report
    {
        [Key]
        public int ReportId { get; set; }
        public string? Type { get; set; }

        // 这里保留 int 是对的，因为它是指向 FinanceStaff 的 StaffId (int)
        public int GeneratedBy { get; set; }

        public DateTime GeneratedDate { get; set; }
        public string? Format { get; set; }

        [ForeignKey("GeneratedBy")]
        public FinanceStaff? Generator { get; set; }
    }
}