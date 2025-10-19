using GSoftPosNew.Models;

namespace GSoftPosNew.ViewModels
{
    public class SalesSummaryReportVM
    {
        public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-7);
        public DateTime EndDate { get; set; } = DateTime.Today;
        public decimal TotalSales { get; set; }
        public int TotalTransactions { get; set; }
        public decimal AverageSale { get; set; }
        public List<DailySalesSummary> DailySummaries { get; set; } = new List<DailySalesSummary>();
        public decimal TotalTax { get; set; }
        public decimal TotalDiscount { get; set; }
        public int TransactionCount { get; set; }
        public List<DailySalesData> DailyData { get; set; } = new List<DailySalesData>();

    }
}
