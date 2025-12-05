using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SADFinalProjectGJ.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        public int InvoiceId { get; set; }

        public DateTime PaymentDate { get; set; }
        // 🔴 修改：double -> decimal
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        [StringLength(50)] // e.g. "Credit Card", "Bank Transfer"
        public string? Method { get; set; }

        [StringLength(100)] // Stripe 的 ID 可能会比较长
        public string? TransactionId { get; set; }

        [StringLength(20)] // e.g. "Success", "Failed"
        public string? Status { get; set; }
        [ForeignKey("InvoiceId")]
        public Invoice? Invoice { get; set; }

        public string? GatewayName { get; set; }
        [ForeignKey("GatewayName")]
        public PaymentGateway? PaymentGateway { get; set; }
    }
}