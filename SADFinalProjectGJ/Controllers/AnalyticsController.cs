using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SADFinalProjectGJ.Data;
using SADFinalProjectGJ.Models;
using SADFinalProjectGJ.ViewModels;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SADFinalProjectGJ.Controllers
{
    [Authorize(Roles = "Admin,FinanceStaff")] // 只有管理员和财务能看分析
    public class AnalyticsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AnalyticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. 获取所有发票数据 (为了计算方便，先取出来，注意：数据量特别大时建议在数据库层分组)
            var invoices = await _context.Invoices
                .Include(i => i.InvoiceItems)
                .ThenInclude(ii => ii.Item)
                .ToListAsync();

            // ==========================================
            // 2. 计算 KPI 卡片数据
            // ==========================================
            var totalRevenue = invoices.Sum(i => i.TotalAmount);

            // 本月收入 (假设 IssueDate 是创建日期)
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var monthlyRevenue = invoices
                .Where(i => i.IssueDate.Month == currentMonth && i.IssueDate.Year == currentYear)
                .Sum(i => i.TotalAmount);

            // 🔥 修复: 使用枚举判断 Pending 数量 (Draft 或 Sent)
            var pendingCount = invoices.Count(i => i.Status == InvoiceStatus.Draft || i.Status == InvoiceStatus.Sent);

            // ==========================================
            // 3. 准备图表数据 - 收入趋势 (最近 6 个月)
            // ==========================================
            var last6Months = Enumerable.Range(0, 6)
                .Select(i => DateTime.Now.AddMonths(-i))
                .OrderBy(d => d) // 从最早的月份开始排
                .ToList();

            var trendLabels = last6Months.Select(d => d.ToString("MMM yyyy")).ToArray();
            var trendData = new List<decimal>();

            foreach (var date in last6Months)
            {
                var monthSum = invoices
                    .Where(i => i.IssueDate.Month == date.Month && i.IssueDate.Year == date.Year)
                    .Sum(i => i.TotalAmount);
                trendData.Add(monthSum);
            }


            // ==========================================
            // 4. 准备图表数据 - 状态分布 (Pie Chart)
            // ==========================================
            var statusGroups = invoices.GroupBy(i => i.Status)
                                        .Select(g => new { Status = g.Key, Count = g.Count() })
                                        .ToList();

            var statusLabels = statusGroups.Select(g => g.Status.ToString()).ToArray();
            var statusData = statusGroups.Select(g => g.Count).ToArray();


            // ==========================================
            // 5. 准备图表数据 - 畅销商品 Top 5 (Bar Chart)
            // ==========================================
            // 注意：这里需要查 InvoiceItems 表
            var topItems = await _context.InvoiceItems
                .Include(ii => ii.Item)
                .GroupBy(ii => ii.Item.Description) // 按商品名分组
                .Select(g => new
                {
                    Name = g.Key,
                    Revenue = g.Sum(x => x.Total) // 按销售总额排序
                })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToListAsync();

            var topItemsLabels = topItems.Select(x => x.Name).ToArray();
            var topItemsRevenue = topItems.Select(x => x.Revenue).ToArray();

            // 6. 准备图表数据 - 账龄分析 (Aging Report)
            // ==========================================
            var overdueInvoices = invoices.Where(i => i.Status == InvoiceStatus.Overdue && i.DueDate < DateTime.Now).ToList();

            var agingData = new int[3]; // [1-30天, 31-60天, 60+天]
            var agingLabels = new string[] { "1-30 Days", "31-60 Days", "60+ Days" };

            foreach (var inv in overdueInvoices)
            {
                var daysOverdue = (DateTime.Now - inv.DueDate).Days;

                if (daysOverdue <= 30)
                    agingData[0]++;
                else if (daysOverdue <= 60)
                    agingData[1]++;
                else
                    agingData[2]++;
            }

            // ==========================================
            // 7. 填充 ViewModel 并返回
            // ==========================================
            var viewModel = new AnalyticsViewModel
            {
                TotalRevenue = totalRevenue,
                MonthlyRevenue = monthlyRevenue,
                PendingInvoices = pendingCount,

                RevenueTrendLabels = trendLabels,
                RevenueTrendData = trendData.ToArray(),

                StatusLabels = statusLabels!, // 这里已经是 string[] 了
                StatusData = statusData,

                TopItemsLabels = topItemsLabels!,
                TopItemsRevenue = topItemsRevenue,

                AgingLabels = agingLabels,
                AgingData = agingData
            };

            return View(viewModel);
        }
        // GET: Analytics/ExportToExcel
        public async Task<IActionResult> ExportToExcel()
        {
            // 1. 获取数据 (包含客户信息)
            var invoices = await _context.Invoices
                .Include(i => i.Client)
                .OrderByDescending(i => i.IssueDate) // 按日期倒序
                .ToListAsync();

            // 2. 创建 Excel 工作簿
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Invoices");

                // --- 设置表头样式 ---
                var currentRow = 1;
                worksheet.Cell(currentRow, 1).Value = "Invoice #";
                worksheet.Cell(currentRow, 2).Value = "Client";
                worksheet.Cell(currentRow, 3).Value = "Date";
                worksheet.Cell(currentRow, 4).Value = "Due Date";
                worksheet.Cell(currentRow, 5).Value = "Status";
                worksheet.Cell(currentRow, 6).Value = "Total Amount";

                // 给表头加粗、加背景色
                var headerRange = worksheet.Range("A1:F1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // --- 填充数据 ---
                foreach (var inv in invoices)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = inv.InvoiceNumber;
                    worksheet.Cell(currentRow, 2).Value = inv.Client?.Name ?? "N/A";

                    // 日期格式化
                    worksheet.Cell(currentRow, 3).Value = inv.IssueDate;
                    worksheet.Cell(currentRow, 3).Style.DateFormat.Format = "yyyy-MM-dd";

                    worksheet.Cell(currentRow, 4).Value = inv.DueDate;
                    worksheet.Cell(currentRow, 4).Style.DateFormat.Format = "yyyy-MM-dd";

                    worksheet.Cell(currentRow, 5).Value = inv.Status.ToString();

                    // 金额格式化 (货币)
                    worksheet.Cell(currentRow, 6).Value = inv.TotalAmount;
                    worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "$ #,##0.00";

                    // 根据状态给文字上色 (可选)
                    if (inv.Status == InvoiceStatus.Paid)
                        worksheet.Cell(currentRow, 5).Style.Font.FontColor = XLColor.Green;

                    if (inv.Status == InvoiceStatus.Overdue)
                        worksheet.Cell(currentRow, 5).Style.Font.FontColor = XLColor.Red;
                }

                // 自动调整列宽
                worksheet.Columns().AdjustToContents();

                // 3. 生成文件流并返回下载
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    string fileName = $"Invoices_Report_{DateTime.Now:yyyyMMdd}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }
    }
}