using GSoftPosNew.Models;

namespace GSoftPosNew.ViewModels
{
    public class PurchaseSummaryReportVM
    {
        public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-1);
        public DateTime EndDate { get; set; } = DateTime.Today;
        public decimal TotalPurchases { get; set; }
        public int TotalTransactions { get; set; }
        public decimal AveragePurchase { get; set; }
        public List<SupplierPurchaseSummary> SupplierSummaries { get; set; } = new List<SupplierPurchaseSummary>();
        //public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-1);
        //public DateTime EndDate { get; set; } = DateTime.Today;
        //public decimal TotalPurchases { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalDiscount { get; set; }
        public int PurchaseCount { get; set; }
        public List<SupplierPurchaseData> SupplierData { get; set; } = new List<SupplierPurchaseData>();
    }
}
