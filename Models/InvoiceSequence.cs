namespace GSoftPosNew.Models
{
    public class InvoiceSequence
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }         // The day of the invoice
        public int LastNumber { get; set; }        // Last invoice number for this day
        public bool IsClosed { get; set; }         // End-of-day flag
    }
}
