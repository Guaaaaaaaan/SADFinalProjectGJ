using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SADFinalProjectGJ.Models
{
    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }

        // ---------------------------------------------------------
        // 关于 StringLength
        // ---------------------------------------------------------

        [Required]
        [StringLength(50)] // 必须写：发票号较短
        public string? InvoiceNumber { get; set; }

        [Required]
        [StringLength(20)] // 必须写：状态也很短 (e.g., "Paid")
        public string? Status { get; set; }

        // 假设你未来加个备注字段，就需要更长
        [StringLength(500)]
        public string? Notes { get; set; }


        // ---------------------------------------------------------
        // 关于 decimal (以前是 double)
        // ---------------------------------------------------------

        [Column(TypeName = "decimal(18, 2)")] // 必须写：这是给 TotalAmount 的
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18, 2)")] // 必须重复写：这是给 TaxAmount 的
        public decimal TaxAmount { get; set; }

        // ---------------------------------------------------------
        // 其他字段
        // ---------------------------------------------------------
        public int ClientId { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsArchived { get; set; } = false; // 默认为 false (未归档)


        public Client? Client { get; set; }
        public ICollection<InvoiceItem>? InvoiceItems { get; set; }
        public ICollection<Payment>? Payments { get; set; }
        public ICollection<Notification>? Notifications { get; set; }
    }
}