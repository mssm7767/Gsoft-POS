using GSoftPosNew.Models;
using System.ComponentModel.DataAnnotations;

namespace GSoftPosNew.ViewModels
{
    public class ReceivePaymentViewModel
    {
        [Required]
        public int CustomerId { get; set; }
        public List<Customer> CustomerList { get; set; } = new List<Customer>();

        public decimal OldBalance { get; set; }

        [Required, DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        [DataType(DataType.Currency)]
        public decimal Advance { get; set; }

        [Required]
        public string PaymentMethod { get; set; }

        public string Narration { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        // For display history
        public List<CustomerPayment> PaymentHistory { get; set; } = new List<CustomerPayment>();

        public string ReceivedBy { get; set; }
    }
}
