using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GSoftPosNew.Models
{
    public class CustomerPayment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public Customer Customer { get; set; }

        public string ReceivedBy { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public decimal Advance { get; set; }

        [Required]
        public string PaymentMethod { get; set; }

        public string Narration { get; set; }

        public DateTime PaymentDate { get; set; }

        public decimal Remaining { get; set; } // Optional, ledger remaining for reporting
        public decimal? Sale { get; set; } // Optional, ledger remaining for reporting

    }
}
