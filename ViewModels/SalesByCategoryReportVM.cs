namespace GSoftPosNew.ViewModels
{
    public class SalesByCategoryReportVM
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<CategorySalesData> Categories { get; set; } = new List<CategorySalesData>();
    }

    public class CategorySalesData
    {
        public string CategoryName { get; set; }
        public int ItemsSold { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalCost { get; set; }
        public decimal Profit => TotalSales - TotalCost;
        public decimal ProfitMargin => TotalSales > 0 ? (Profit / TotalSales) * 100 : 0;
    }
}
