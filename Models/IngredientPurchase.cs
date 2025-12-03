
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace GSoftPosNew.Models
{
    public class IngredientPurchase
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Supplier is required")]
        public int SupplierId { get; set; }

        [ForeignKey("SupplierId")]
        public Supplier Supplier { get; set; }

        public string ReferenceNo { get; set; }

        [Required(ErrorMessage = "Purchase Date is required")]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Status is required")]
        public string Status { get; set; } = "Pending";

        public string DiscountType { get; set; } = "None";
        public decimal DiscountAmount { get; set; }
        public string TaxType { get; set; } = "None";
        public decimal TaxAmount { get; set; }
        public string Notes { get; set; } = "Notes";
        public string PurchaseType { get; set; } = "New";

        public decimal TotalAmount { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get; set; }

        // public ICollection<PurchaseLineItem> LineItems { get; set; }
        public List<IngredientPurchaseItem> Items { get; set; } = new List<IngredientPurchaseItem>();
        [NotMapped]
        public List<Ingredient> ProductItems { get; set; } = new List<Ingredient>();
        [NotMapped]
        public Ingredient Item { get;  set; } = new Ingredient();
        [NotMapped]
        public string ItemsJson { get; set; }  // Hidden field value


        [Display(Name = "Business Location")]
        public string? BusinessLocation { get; set; }

        [Display(Name = "Pay Term")]
        public string? PayTerm { get; set; }

        [Display(Name = "Pay Term Unit")]
        public string? PayTermUnit { get; set; }

        [Display(Name = "Document Path")]
        public string? DocumentPath { get; set; }
    }

}
