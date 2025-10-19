namespace GSoftPosNew.Models
{
    public class DailySalesSummary
    {
        public DateTime Date { get; set; }
        public decimal TotalSales { get; set; }
        public int TransactionCount { get; set; }
    }
}
