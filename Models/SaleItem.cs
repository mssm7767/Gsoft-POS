using System.ComponentModel.DataAnnotations.Schema;

namespace GSoftPosNew.Models
{
    // Models/SaleItem.cs
    public class SaleItem
    {
        public int Id { get; set; }

        public int SaleId { get; set; }
        public Sale Sale { get; set; }

        public int ItemId { get; set; }
        public ItemModel Item { get; set; }

        // ✅ Quantity ab decimal hai (0.50, 1.25, 2.75 waghera allow)
        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        // ✅ Prices ko bhi safe decimal type de dete hain (optional but best practice)
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountPercent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }
    }
}
