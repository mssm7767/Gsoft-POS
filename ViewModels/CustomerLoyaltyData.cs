namespace GSoftPosNew.ViewModels
{
    public class CustomerLoyaltyData
    {
        public string CustomerName { get; set; }
        public DateTime JoinDate { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalSpent { get; set; }
        public double DaysSinceLastPurchase { get; set; }

        // Computed property for loyalty tier (optional)
        public string LoyaltyTier
        {
            get
            {
                return TotalSpent switch
                {
                    > 5000 => "Platinum",
                    > 2000 => "Gold",
                    > 500 => "Silver",
                    _ => "Bronze"
                };
            }
        }

        // Computed property for average days between purchases (optional)
        public string AverageDaysBetweenPurchases
        {
            get
            {
                if (TransactionCount > 1)
                {
                    return (DaysSinceLastPurchase / (TransactionCount - 1)).ToString("0");
                }
                return "N/A";
            }
        }

        public int CustomerId { get; set; }
    }
}
