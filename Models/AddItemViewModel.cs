using GSoftPosNew.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace GSoftPosNew.Models
{
    // ViewModel for Add/Edit Item page
    public class AddItemViewModel
    {
        // Actual item data bound to form
        public ItemModel ItemData { get; set; } = new ItemModel();

        // Dropdown data
        public List<SelectListItem> CategoryList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> SupplierList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> UnitList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> LocationList { get; set; } = new List<SelectListItem>();

        // Existing items to display in table below form
        public List<ItemModel> ExistingItems { get; set; } = new List<ItemModel>();

        public List<RecipeIngredientVM> RecipeIngredients { get; set; } = new();
    }
}
