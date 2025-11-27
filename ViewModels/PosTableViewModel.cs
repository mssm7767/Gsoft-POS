using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GSoftPosNew.ViewModels
{
    public class PosTableViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Location")]
        public int? LocationId { get; set; }

        [Required]
        [MaxLength(50)]
        [Display(Name = "Table Name")]
        public string TableName { get; set; }

        [MaxLength(20)]
        [Display(Name = "Table Code")]
        public string TableCode { get; set; }

        [Display(Name = "Seating Capacity")]
        public int? Capacity { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(200)]
        public string Notes { get; set; }

        // 🔹 yahi dropdown ka source hai
        public List<SelectListItem> LocationList { get; set; } = new();

        // 🔹 neeche list dikhane ke liye
        public List<PosTableListItem> ExistingTables { get; set; } = new();
    }

    public class PosTableListItem
    {
        public int Id { get; set; }
        public string LocationName { get; set; }
        public string TableName { get; set; }
        public string TableCode { get; set; }
        public int? Capacity { get; set; }
        public bool IsActive { get; set; }
    }
}
