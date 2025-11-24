namespace GSoftPosNew.Models
{
    public class VanStockSale
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public int CustomerId { get; set; }
        public string ItemCode { get; set; }
        public int Quantity { get; set; }
        public string? ShopName { get; set; }
        public string? Description { get; set; }
        public DateTime? SaleDate { get; set; } = DateTime.Now;

        public ItemModel Item { get; set; }
        public Customer Customer { get; set; }
    }
}
