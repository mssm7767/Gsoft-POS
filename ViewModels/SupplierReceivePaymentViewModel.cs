using GSoftPosNew.Models;
using System.ComponentModel.DataAnnotations;

namespace GSoftPosNew.ViewModels
{
    public class SupplierReceivePaymentViewModel
    {
        [Required]
        public int SupplierId { get; set; }
        public List<Supplier> SupplierList { get; set; } = new List<Supplier>();
        public decimal OldBalance { get; set; }
        [Required, DataType(DataType.Currency)]
        public decimal? Amount { get; set; }
        [DataType(DataType.Currency)]
        public decimal? Advance { get; set; }
        [Required]
        public string PaymentMethod { get; set; }
        public string Narration { get; set; }
        public DateTime? PaymentDate { get; set; }
        public List<SupplierPayment> PaymentHistory { get; set; } = new List<SupplierPayment>();
        public string ReceivedBy { get; set; }
    }
}
