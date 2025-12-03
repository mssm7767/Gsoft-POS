using System.Collections.Generic;
using GSoftPosNew.Models;

namespace GSoftPosNew.ViewModels
{
    public class IngredientSetupViewModel
    {
        public IEnumerable<IngredientCategory> Categories { get; set; }
        public IEnumerable<Ingredient> Ingredients { get; set; }
    }
}
