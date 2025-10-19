namespace GSoftPosNew.ViewModels
{
    public class TaxSummaryReportVM
    {
        public DateTime StartDate { get; set; } = DateTime.Today.AddMonths(-1);
        public DateTime EndDate { get; set; } = DateTime.Today;
        public List<TaxSummaryData> TaxData { get; set; } = new List<TaxSummaryData>();
        public decimal TotalTax => TaxData.Sum(t => t.TaxAmount);
    }
}
