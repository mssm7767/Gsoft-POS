namespace GSoftPosNew.ViewModels
{
    public class CustomerSummaryReportVM
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<CustomerSummaryVM> Customers { get; set; } = new List<CustomerSummaryVM>();
    }
}
