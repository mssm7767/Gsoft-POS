namespace GSoftPosNew.Models
{
    public class IngredientCategory
    {
        public int Id { get; set; }

        // Name ko default empty de diya, taake kabhi null na ho
        public string Name { get; set; } = string.Empty;   // required

        // Description optional rakhenge (null allow)
        public string? Description { get; set; }

        // Navigation
        public ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
    }
}
