namespace GSoftPosNew.Models
{
    public class CustomerVanStock
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int ItemId { get; set; }
        public int Quantity { get; set; }
        public DateTime PurchasedDate { get; set; } = DateTime.Now;

        public ItemModel Item { get; set; }
        public Customer Customer { get; set; }
    }
}
