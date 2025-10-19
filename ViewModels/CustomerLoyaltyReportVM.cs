namespace GSoftPosNew.ViewModels
{// ViewModels/CustomerLoyaltyReportVM.cs
    public class CustomerLoyaltyReportVM
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<CustomerLoyaltyData> Customers { get; set; } = new List<CustomerLoyaltyData>();
    }
}
