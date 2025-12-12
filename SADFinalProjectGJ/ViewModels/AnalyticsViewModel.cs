// ViewModels/AnalyticsViewModel.cs
namespace SADFinalProjectGJ.ViewModels
{
    public class AnalyticsViewModel
    {
        // ==============================
        // 1. 核心 KPI (顶部卡片)
        // ==============================
        public decimal TotalRevenue { get; set; }        // 总收入
        public decimal MonthlyRevenue { get; set; }      // 本月收入
        public int PendingInvoices { get; set; }         // 待处理发票数

        // ==============================
        // 2. 趋势图数据 (Line Chart)
        // ==============================
        // 比如: ["Jan", "Feb", "Mar", "Apr", "May", "Jun"]
        public string[] RevenueTrendLabels { get; set; } = Array.Empty<string>();

        // 比如: [5000, 7000, 4500, 8000, 6000, 9500]
        public decimal[] RevenueTrendData { get; set; } = Array.Empty<decimal>();

        // ==============================
        // 3. 状态分布数据 (Pie Chart)
        // ==============================
        // 比如: ["Paid", "Draft", "Overdue"]
        public string[] StatusLabels { get; set; } = Array.Empty<string>();

        // 比如: [15, 5, 2]
        public int[] StatusData { get; set; } = Array.Empty<int>();

        // ==============================
        // 4. 畅销服务/商品 (Bar Chart)
        // ==============================
        public string[] TopItemsLabels { get; set; } = Array.Empty<string>();
        public decimal[] TopItemsRevenue { get; set; } = Array.Empty<decimal>();

        // ==============================
        // 5. 账龄分析 (Aging Report)
        // ==============================
        public int[] AgingData { get; set; } = Array.Empty<int>();
        public string[] AgingLabels { get; set; } = Array.Empty<string>();
    }
}