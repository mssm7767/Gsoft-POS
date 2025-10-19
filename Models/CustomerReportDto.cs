namespace GSoftPosNew.Models
{
    public class CustomerReportDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }

        public decimal Debit { get; set; }      // Total Sales (Invoices)
        public decimal Credit { get; set; }     // Total Payments received
        public decimal Advance { get; set; }    // Overpaid by customer
        public decimal Balance { get; set; }    // Debit - Credit - Advance

        // For clarity in report
        public decimal Receivable => Balance > 0 ? Balance : 0;
        public decimal Payable => Balance < 0 ? Math.Abs(Balance) : 0;
    }



}
