namespace GSoftPosNew.ViewModels
{
    public class RecipeIngredientVM
    {
        public int IngredientId { get; set; }
        public decimal UseQty { get; set; }
        public string Unit { get; set; }
        public decimal CostPerUnit { get; set; }
        public decimal LineTotal { get; set; }
    }

}
