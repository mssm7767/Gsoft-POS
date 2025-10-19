namespace GSoftPosNew.ViewModels
{
    public class ProfitLossReportVM
    {
        public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-1);
        public DateTime EndDate { get; set; } = DateTime.Today;
        public decimal TotalSales { get; set; }
        public decimal TotalPurchases { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal GrossProfit => TotalSales - TotalPurchases;
        public decimal NetProfit => GrossProfit - TotalExpenses;
        public List<ProfitLossItemVM> Items { get; set; } = new List<ProfitLossItemVM>();
    }
}
