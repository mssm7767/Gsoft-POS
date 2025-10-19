namespace GSoftPosNew.ViewModels
{
    public class InventoryReportVM
    {
        public string CategoryFilter { get; set; }
        public string SearchTerm { get; set; }
        public List<InventoryItemVM> Items { get; set; } = new List<InventoryItemVM>();
        //public decimal TotalValue => Items.Sum(i => i.StockValue);
        public int LowStockItems => Items.Count(i => i.Quantity <= i.LowStockThreshold); 

        public List<InventoryItemData> InventoryItems { get; set; } = new List<InventoryItemData>();
        public decimal TotalValue => InventoryItems.Sum(i => i.StockValue);
        public int LowStockCount => InventoryItems.Count(i => i.Status == "Low Stock");
        public int OutOfStockCount => InventoryItems.Count(i => i.Status == "Out of Stock");
    }
}
