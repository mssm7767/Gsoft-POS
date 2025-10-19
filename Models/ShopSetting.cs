using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GSoftPosNew.Models
{
    public class ShopSetting
    {

        [Key]
        public int Id { get; set; }

        public string? ShopName { get; set; }
        public string? NTN { get; set; }
        public string? STRN { get; set; }
        public string? POSId { get; set; }

        public string? Email { get; set; }

        public string? Contact1 { get; set; }

        public string? Contact2 { get; set; }

        public string? Address { get; set; }

        public string? Message { get; set; }

        // Store logo filename (in DB)
        public string? LogoPath { get; set; }

        [NotMapped]
        public IFormFile LogoFile { get; set; }

    }
}
