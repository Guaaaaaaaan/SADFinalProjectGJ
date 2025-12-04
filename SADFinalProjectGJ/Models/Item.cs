using SADFinalProjectGJ.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SADFinalProjectGJ.Models
{
    public class Item
    {
        [Key]
        public int ItemId { get; set; } // PK

        [Required]
        [StringLength(200)] // 描述不要太长
        public string? Description { get; set; }

        // 🔴 修改：double -> decimal
        [Column(TypeName = "decimal(18, 2)")]
        public decimal UnitPrice { get; set; }
        // ICollection (反向导航)
        public ICollection<InvoiceItem>? InvoiceItems { get; set; }
    }
}
