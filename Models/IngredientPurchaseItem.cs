using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;


namespace GSoftPosNew.Models
{
    public class IngredientPurchaseItem
    {
        public int Id { get; set; }


        public int IngredientId { get; set; }
        [NotMapped]
        public Ingredient Ingredient { get; set; }
        [NotMapped]
        public string IngredientName { get; set; }
        [NotMapped]
        public string ItemCode { get; set; }


        [JsonPropertyName("qty")] // ✅ maps "qty" → Quantity
        public int Quantity { get; set; }

        [JsonPropertyName("mrp")] // ✅ maps "qty" → Quantity
        public decimal UnitCost { get; set; }
        [JsonPropertyName("disPct")]
        public decimal DiscountPercent { get; set; }
        public decimal TaxPercent { get; set; }
        public decimal LineTotal { get; set; }
        [NotMapped]
        public string? GenericName { get; set; }
        public int PurchaseId { get; set; }
    }

}
