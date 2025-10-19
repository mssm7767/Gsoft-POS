namespace GSoftPosNew.Models
{// Models/SalesReport.cs
    public class SalesReport
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<Sale> Sales { get; set; } = new List<Sale>();
        public decimal TotalSales => Sales.Sum(s => s.Total);
        public decimal TotalTax => Sales.Sum(s => s.Tax);
        public decimal TotalDiscount => Sales.Sum(s => s.Discount);
        public int TotalTransactions => Sales.Count;
    }
}
