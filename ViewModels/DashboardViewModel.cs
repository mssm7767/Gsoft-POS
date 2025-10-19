using GSoftPosNew.Models;

namespace GSoftPosNew.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalItems { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal TotalRevenuesOrPayments { get; set; }
        public decimal TotalPurchases { get; set; }
        public int TotalLowStockItems { get; set; }
        public int TotalExpiryItems { get; set; }
        public decimal TotalStockValue { get; set; }
        public List<ItemModel> ItemsList { get; set; } = new List<ItemModel>();

        // For charts
        public List<string> SalesMonths { get; set; } = new();
        public List<decimal> SalesData { get; set; } = new();
        public List<string> TopProducts { get; set; } = new();
        public List<int> TopProductSales { get; set; } = new();
    }

}
