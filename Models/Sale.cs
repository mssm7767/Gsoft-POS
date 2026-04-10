using System.ComponentModel.DataAnnotations.Schema;

namespace GSoftPosNew.Models
{


    // Models/Sale.cs
    public class Sale
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime SaleDate { get; set; } = DateTime.Now;
        public string CashierId { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public string? SaleType { get; set; } = "Sale";
        public List<SaleItem> SaleItems { get; set; } = new List<SaleItem>();

        
        public Payment Payment { get; set; } = new Payment();
        [NotMapped]
        public Customer Customers { get; set; } = new Customer();

        public int CustomerId { get; set; }

        public string? CustomerDisplayName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }

        [NotMapped]
        public int custId { get; set; }

        [NotMapped]
        public decimal tender_amount { get; set; }

        [NotMapped]
        public string? TerminalName { get; set; }

        public string? Waiter { get; set; }
        public string? TableNo { get; set; }
        public string? OrderType { get; set; }
        public decimal? ServiceCharges { get; set; }
    }
}
