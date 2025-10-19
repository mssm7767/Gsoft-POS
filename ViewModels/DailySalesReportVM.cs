namespace GSoftPosNew.ViewModels
{
    public class DailySalesReportVM
    {
        public DateTime ReportDate { get; set; } = DateTime.Today;
        public decimal TotalSales { get; set; }
        public decimal TotalTax { get; set; }
        public List<DailySaleVM> Sales { get; set; } = new List<DailySaleVM>();
    }

    public class DailySaleVM
    {
        public DateTime SaleDate { get; set; }
        public string InvoiceNumber { get; set; }
        public string CashierId { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public PaymentInfoVM Payment { get; set; }
    }

    public class PaymentInfoVM
    {
        public string PaymentMethod { get; set; }
        // Add other payment properties if needed
        // Example:
        // public decimal Amount { get; set; }
        // public decimal Change { get; set; }
    }
}
