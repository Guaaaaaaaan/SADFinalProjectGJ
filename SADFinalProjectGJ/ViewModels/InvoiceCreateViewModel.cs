using System.ComponentModel.DataAnnotations;
using SADFinalProjectGJ.Models;
namespace SADFinalProjectGJ.ViewModels
{
    public class InvoiceCreateViewModel
    {
        [Required(ErrorMessage = "Please select a client")]
        public int ClientId { get; set; }

        [Required(ErrorMessage = "Due Date is required")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(7); // 默认两周后到期

        //GST Static 9%
        [Range(0, 100, ErrorMessage = "GST Must between 0-100.")]
        public decimal GstRate { get; set; } = 9;

        // 这是一个列表，用来装用户添加的那一堆商品
        public List<InvoiceItemEntry> Items { get; set; } = new List<InvoiceItemEntry>();
    }
}
