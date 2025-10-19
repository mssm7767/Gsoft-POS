// Models/Customer.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GSoftPosNew.Models
{
    public class Customer
    {
   
            [Key]
            public int Id { get; set; }

            // REQUIRED
            [Required, StringLength(100)]
            public string CustomerName { get; set; } = string.Empty;

            // OPTIONAL
            [StringLength(100)]
            public string? FatherName { get; set; }

            // OPTIONAL
            [StringLength(20)]
            public string? NIC { get; set; }

            // REQUIRED
            [Required, StringLength(20)]
            public string ContactNumber { get; set; } = string.Empty;

            // REQUIRED
            [Required]
            public string Address1 { get; set; } = string.Empty;

            // OPTIONAL
            public string? Address2 { get; set; }

            // OPTIONAL
            [EmailAddress]
            public string? Email { get; set; }

            // OPTIONAL
            [Url]
            public string? Website { get; set; }

            // OPTIONAL (make nullable so default = null if not provided)
            [Column(TypeName = "decimal(18,2)")]
            public decimal? OpeningBalance { get; set; }

            // OPTIONAL
            public string? PictureUrl { get; set; }
       

    }
}
