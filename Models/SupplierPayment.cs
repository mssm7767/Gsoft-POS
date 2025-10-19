namespace GSoftPosNew.Models
{

    public class SupplierPayment
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public decimal? Amount { get; set; }        // Nullable (question mark lagao)
        public decimal? Advance { get; set; }       // Nullable
        public string PaymentMethod { get; set; }
        public string? Narration { get; set; }
        public DateTime? PaymentDate { get; set; }  // Nullable
        public decimal? Remaining { get; set; }     // Nullable
        public string? ReceivedBy { get; set; }
        public Supplier Supplier { get; set; }
    }

}
