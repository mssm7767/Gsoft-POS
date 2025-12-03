namespace GSoftPosNew.Models
{
    public class Ingredient
    {
        public int Id { get; set; }

        // In dono ko hum required + default bana dete hain
        public string Name { get; set; } = string.Empty;  // Ingredient Name
        public string Code { get; set; } = string.Empty;  // Internal code

        // Category
        public int? CategoryId { get; set; }
        public IngredientCategory? Category { get; set; }

        // Purchase info
        public decimal? PurchaseQty { get; set; }        // e.g. 1, 5, 1000
        public string? PurchaseUnit { get; set; }        // kg, g, ltr, pcs
        public decimal? ConsumptionQty { get; set; }     // e.g. 10 (g / ml)
        public string? ConsumptionUnit { get; set; }     // g, ml, pcs

        public decimal? PurchasePrice { get; set; }      // total price for purchase qty
        public decimal? CostPerUnit { get; set; }        // auto calculated
    }
}
