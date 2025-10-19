namespace GSoftPosNew.ViewModels
{
    public class SupplierPurchaseData
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
        public decimal TotalPurchases { get; set; }
        public int PurchaseCount { get; set; }
    
        public decimal AveragePurchase => PurchaseCount > 0 ? TotalPurchases / PurchaseCount : 0;
    }
}
