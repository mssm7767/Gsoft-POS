namespace GSoftPosNew.Models
{
    public class ItemIngredient
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public ItemModel Item { get; set; }
        public int IngredientId { get; set; }
        public Ingredient Ingredient { get; set; }
        public decimal UseQty { get; set; }
        public string Unit { get; set; }
        public decimal CostPerUnit { get; set; }
        public decimal LineTotal { get; set; }

        
    }
}
