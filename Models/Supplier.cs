using System.ComponentModel.DataAnnotations;

namespace GSoftPosNew.Models
{
    public class Supplier
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Supplier Name is required")]
        [StringLength(100)]
        public string SupplierName { get; set; }

        public decimal? OpeningBalance { get; set; } = 0;

        [StringLength(100)]
        public string? Company { get; set; }

        [StringLength(50)]
        public string? SupplierType { get; set; }


        [StringLength(100)]
        public string? ContactPerson { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Url]
        public string? Website { get; set; }

        [StringLength(50)]
        public string? TaxNumber { get; set; }

        [StringLength(20)]
        public string MobileNumber { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Billing Address is required")]
        public string BillingAddress { get; set; }

        [Required(ErrorMessage = "Billing Country is required")]
        [StringLength(50)]
        public string BillingCountry { get; set; }

        [Required(ErrorMessage = "Billing City is required")]
        [StringLength(50)]
        public string BillingCity { get; set; }

        public string? ShippingAddress { get; set; }

        [StringLength(50)]
        public string? ShippingCountry { get; set; }

        [StringLength(50)]
        public string? ShippingCity { get; set; }

        public string? Description { get; set; }

        // If you need to store a file path:
        public string? LogoPath { get; set; }

        // URL to serve that logo:
        public string? LogoUrl { get; set; }
    }
}
