namespace GSoftPosNew.ViewModels
{
    public class TaxSummaryData
    {
        public string TaxName { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public int TransactionCount { get; set; }
    }
}
