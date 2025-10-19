namespace GSoftPosNew.ViewModels
{// ViewModels/PurchaseBySupplierReportVM.cs
    public class PurchaseBySupplierReportVM
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<SupplierPurchaseData> Suppliers { get; set; } = new List<SupplierPurchaseData>();
    }
}
