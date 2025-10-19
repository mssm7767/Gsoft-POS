using System.ComponentModel.DataAnnotations;

namespace GSoftPosNew.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [StringLength(50, ErrorMessage = "Username must be between 3 and 50 characters", MinimumLength = 3)]
        public string Username { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Full name must be between 3 and 100 characters", MinimumLength = 3)]
        public string FullName { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Role { get; set; } = "Cashier"; // Default role
    }
}
