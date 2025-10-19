namespace GSoftPosNew.ViewModels
{
    public class ItemSalesData
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public int QuantitySold { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalCost { get; set; }
        public decimal Profit => TotalSales - TotalCost;
        public decimal ProfitMargin => TotalSales > 0 ? (Profit / TotalSales) * 100 : 0;
    }
}
