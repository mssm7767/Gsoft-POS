namespace GSoftPosNew.ViewModels
{
    public class InventoryItemData
    {
        public int ItemId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Category { get; set; }
        public int Quantity { get; set; }
        public int LowStockThreshold { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal StockValue => Quantity * CostPrice;
        public string Status => Quantity <= 0 ? "Out of Stock" :
                              Quantity <= LowStockThreshold ? "Low Stock" : "In Stock";
    }
}
