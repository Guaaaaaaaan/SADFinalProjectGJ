using SADFinalProjectGJ.Models;
namespace SADFinalProjectGJ.ViewModels
{
    public class DashboardViewModel
    {
        // 1. 统计卡片的数据
        public decimal TotalRevenue { get; set; } // 总预期收入
        public int PendingInvoicesCount { get; set; } // 待处理发票数量
        public int TotalClientsCount { get; set; } // 总客户数

        // 2. 表格的数据
        public List<Invoice> RecentInvoices { get; set; } = new List<Invoice>();
    }
}
