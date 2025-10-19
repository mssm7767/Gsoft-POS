namespace GSoftPosNew.Models
{

    // Models/Payment.cs
    public class Payment
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public Sale Sale { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } // Cash, Card, etc.
        public DateTime PaymentDate { get; set; } = DateTime.Now;
    }
}
