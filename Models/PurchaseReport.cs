namespace GSoftPosNew.Models
{

    // Models/PurchaseReport.cs
    public class PurchaseReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int SupplierId { get; set; }
        public List<Purchase> Purchases { get; set; } = new List<Purchase>();
        public decimal TotalPurchases => Purchases.Sum(p => p.TotalAmount);
        public int TotalItemsPurchased => Purchases.Sum(p => p.Items.Count);
        public int TotalSuppliers => Purchases.Select(p => p.SupplierId).Distinct().Count();
    }
}
