namespace GSoftPosNew.Models
{
    public class SupplierPurchaseSummary
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
        public decimal TotalAmount { get; set; }
        public int TransactionCount { get; set; }
    }
}
