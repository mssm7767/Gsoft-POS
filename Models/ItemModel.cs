using System.ComponentModel.DataAnnotations;

namespace GSoftPosNew.Models
{
    // Represents a single inventory item
    public class ItemModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Item Code is required")]
        public string ItemCode { get; set; }

        public string? ReferenceCode { get; set; }

        [Required(ErrorMessage = "Item Name is required")]
        public string ItemName { get; set; }
        public string? Flavour { get; set; }

        public string? GenericName { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }

        public Category Category { get; set; }

        [Required(ErrorMessage = "Supplier is required")]
        public int SupplierId { get; set; }

        public Supplier Supplier { get; set; }

        public string Unit { get; set; } = "0";
        public decimal? UnitPrice { get; set; }

        public int? LocationId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Sale Price must be non-negative")]
        public decimal SalePrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Purchase Price must be non-negative")]
        public decimal PurchasePrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Pack Price must be non-negative")]
        public decimal PackPrice { get; set; }

        public string? PackSize { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be non-negative")]
        public int Quantity { get; set; }

        [Range(0, 100, ErrorMessage = "Markup % must be between 0 and 100")]
        public decimal MarkupPercentage { get; set; }
        public decimal CostPrice { get; set; }
        public decimal TaxRate { get; set; } = 0.10m; // Default 10%
        public int StockQuantity { get; set; }
        public int LowStockThreshold { get; set; } = 5; 
        [DataType(DataType.Date)]
        [Display(Name = "Expiry Date")]
        public DateTime? ExpiryDate { get; set; }

        public string? ImagePath { get; set; }

    }
}
