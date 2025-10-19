namespace GSoftPosNew.Models
{
    public class ItemSalesSummary
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public string Category { get; set; }
        public int QuantitySold { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AveragePrice { get; set; }
    }
}
