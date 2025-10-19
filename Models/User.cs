using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GSoftPosNew.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty; // nvarchar(MAX), NOT NULL

        [Required, Display(Name = "Username")]
        public string Username { get; set; } = string.Empty; // nvarchar(MAX), NOT NULL

        [Phone]
        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; } // nvarchar(MAX), NULL

        [Display(Name = "Address")]
        public string? Address { get; set; } // nvarchar(MAX), NULL

        [EmailAddress]
        public string? Email { get; set; } // nvarchar(MAX), NULL

        [Display(Name = "Profile Picture URL")]
        public string? ProfilePicUrl { get; set; } // nvarchar(MAX), NULL

        [Display(Name = "Profile Image Path")]
        public string? ProfileImagePath { get; set; } // nvarchar(MAX), NULL

        [Display(Name = "Role Id")]
        public int? RoleId { get; set; } // int, NULL

        public bool IsActive { get; set; } = false; // bit, NOT NULL

        public DateTime? LastLogin { get; set; } // datetime2(7), NULL

        public string? Role { get; set; } // nvarchar(MAX), NULL

        public bool EmailConfirmed { get; set; } // bit, NOT NULL

        [Required]
        public string PasswordHash { get; set; } = string.Empty; // nvarchar(MAX), NOT NULL

        [Display(Name = "License Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? LicenseExpiryDate { get; set; } // datetime2(7), NULL

        // Not stored in DB (for UI only)
        [NotMapped]
        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [NotMapped]
        public IFormFile? ProfilePic { get; set; }
    }


}
