namespace GSoftPosNew.Models
{
    // Models/Item.cs
    public class Item
    {
        public int Id { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string GenericName { get; set; } // Category
        public decimal SalePrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal TaxRate { get; set; } = 0.10m; // Default 10%
        public int StockQuantity { get; set; }
    }
}
