using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GSoftPosNew.Models
{
    public class Expense
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Expense date is required")]
        [DataType(DataType.Date)]
        public DateTime ExpenseDate { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Expense Category")]
        public int ExpenseCategoryId { get; set; }

        [ForeignKey("ExpenseCategoryId")]
        public ExpenseCategory ExpenseCategory { get; set; }

        [StringLength(250)]
        public string? Description { get; set; } = " ";

        [StringLength(200)]
        public string? ExpenseFor { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
        public decimal Amount { get; set; }

        [StringLength(100)]
        public string? ReferenceNumber { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        [StringLength(100)]
        public string CreatedBy { get; set; } = " ";
    }
}
