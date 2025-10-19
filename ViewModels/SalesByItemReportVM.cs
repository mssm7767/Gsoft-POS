using GSoftPosNew.Models;

namespace GSoftPosNew.ViewModels
{
    public class SalesByItemReportVM
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<ItemSalesSummary> Items { get; set; } = new List<ItemSalesSummary>();
        public List<ItemSalesData> ItemSales { get; set; } = new List<ItemSalesData>();
    }
}
