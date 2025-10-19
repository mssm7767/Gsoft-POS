namespace GSoftPosNew.ViewModels
{// ViewModels/SupplierPurchasesReportVM.cs
    public class SupplierPurchasesReportVM
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<PurchaseDetail> Purchases { get; set; } = new List<PurchaseDetail>();
    }

    public class PurchaseDetail
    {
        public int PurchaseId { get; set; }
        public DateTime Date { get; set; }
        public string ReferenceNo { get; set; }
        public string Status { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
