
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;


namespace GSoftPosNew.Models
{
    public class Purchase
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Supplier is required")]
        public int SupplierId { get; set; }

        // اگر آپ نے Supplier اینٹیٹی بنائی ہے تو یہ navigation پراپرٹی بھی رکھ سکتے ہیں
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


        // اگر آپ لین آئٹمز بھی ماڈل کرنا چاہیں:
        // public ICollection<PurchaseLineItem> LineItems { get; set; }
        public List<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
        [NotMapped]
        public List<ItemModel> ProductItems { get; set; } = new List<ItemModel>();
        [NotMapped]
        public ItemModel Item { get;  set; } = new ItemModel();
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

    public class PurchaseItem
    {
        public int Id { get; set; }


        public int ItemId { get; set; }
        [NotMapped]
        public ItemModel Item { get; set; }
        [NotMapped]
        public string ItemName { get; set; }
        [NotMapped]
        public string ItemCode { get; set; }


        [JsonPropertyName("qty")] // ✅ maps "qty" → Quantity
        public int Quantity { get; set; }

        [JsonPropertyName("mrp")] // ✅ maps "qty" → Quantity
        public decimal UnitCost { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal TaxPercent { get; set; }
        public decimal LineTotal { get; set; }
        [NotMapped]
        public string GenericName { get; set; }
        public int PurchaseId { get; set; }
    }

}
