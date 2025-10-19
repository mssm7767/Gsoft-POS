namespace GSoftPosNew.ViewModels
{
    public class CustomerSummaryVM
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
        public decimal AverageSpend => TransactionCount > 0 ? TotalSpent / TransactionCount : 0;
    }
}
