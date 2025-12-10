namespace GSoftPosNew.ViewModels
{
    public class SaleReceiptViewModel
    {
        // Sale Info
        public int SaleId { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime SaleDate { get; set; }
        public string CashierName { get; set; }
        public string? Waiter { get; set; }
        public string? TableNo { get; set; }

        // Totals
        public decimal SubTotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public decimal? ServiceCharges { get; set; }

        // Customer Info
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }

        // Items
        public List<SaleItemReceiptVM> Items { get; set; } = new List<SaleItemReceiptVM>();

        // Payment
        public string PaymentMethod { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Change { get; set; }
    }

    public class SaleItemReceiptVM
    {
        public int SrNo { get; set; }
        public string ItemName { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal LineTotal { get; set; }
    }

}
