using GSoftPosNew.Models;

namespace GSoftPosNew.ViewModels
{
    public class CustomerPurchasesReportVM
    {
        public CustomerVM Customer { get; set; }
        public List<SaleVM> Purchases { get; set; } = new List<SaleVM>();
    }

    public class CustomerVM
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public DateTime JoinDate { get; set; }
    }
    public class SaleVM
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal Total { get; set; }
        public List<SaleItemVM> SaleItems { get; set; } = new List<SaleItemVM>();
    }

    public class SaleItemVM
    {
        // Add properties for sale items if needed
        // For example:
        // public string ProductName { get; set; }
        // public decimal Price { get; set; }
        // public int Quantity { get; set; }
    }
}
