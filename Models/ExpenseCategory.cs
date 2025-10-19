using System.ComponentModel.DataAnnotations;

namespace GSoftPosNew.Models
{
    public class ExpenseCategory
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; }   // ✅ بس یہی رکھیں

        public string? Description { get; set; }

        public string? ExpenseFor { get; set; } = " ";

        public string CreatedBy { get; set; } = "Admin";
    }
}
