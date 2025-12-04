using SADFinalProjectGJ.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SADFinalProjectGJ.Models
{
    public class InvoiceItem
    {
        [Key]
        // FKs
        public int InvoiceItemId { get; set; }
        public int InvoiceId { get; set; }
        public int ItemId { get; set; }

        public int Quantity { get; set; }
        // 修改：double -> decimal
        [Column(TypeName = "decimal(18, 2)")]
        public decimal UnitPrice { get; set; }

        // 修改：double -> decimal
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Total { get; set; }

        // Reference Navigation Properties
        [ForeignKey("InvoiceId")] // ✅ 明确告诉 EF: InvoiceId 对应 Invoice
        public Invoice? Invoice { get; set; }

        [ForeignKey("ItemId")]    // ✅ 明确告诉 EF: ItemId 对应 Item
        public Item? Item { get; set; }
    }
}
