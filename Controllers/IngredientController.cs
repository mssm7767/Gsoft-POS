using GSoftPosNew.Data;
using GSoftPosNew.Models;
using GSoftPosNew.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace GSoftPosNew.Controllers
{
    public class IngredientController : Controller
    {
        private readonly AppDbContext _context;

        public IngredientController(AppDbContext context)
        {
            _context = context;
        }

        // ====== PRIVATE HELPER: ViewModel build karega ======
        private async Task<IngredientSetupViewModel> BuildSetupViewModelAsync()
        {
            var categories = await _context.IngredientCategories
                .OrderBy(c => c.Name)
                .ToListAsync();

            var ingredients = await _context.Ingredients
                .Include(i => i.Category)
                .OrderBy(i => i.Name)
                .ToListAsync();

            var units = await _context.Units
                .OrderBy(u => u.Name)
                .ToListAsync();

            ViewBag.UnitList = units;

            return new IngredientSetupViewModel
            {
                Categories = categories,
                Ingredients = ingredients
            };
        }

        // ====== MAIN SCREEN: /Ingredient/Setup ======
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = await _context.Ingredients.ToListAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Setup()
        {
            var vm = await BuildSetupViewModelAsync();
            return View(vm);
        }

        // ====== CATEGORY SAVE ======
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCategory(IngredientCategory model)
        {

            if (model.Id == 0)
            {
                _context.IngredientCategories.Add(model);
                TempData["ToastMessage"] = "Category added successfully.";
            }
            else
            {
                _context.IngredientCategories.Update(model);
                TempData["ToastMessage"] = "Category updated successfully.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Setup));
        }

        // ====== CATEGORY DELETE ======
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var cat = await _context.IngredientCategories
                .Include(c => c.Ingredients)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cat == null)
            {
                TempData["ToastMessage"] = "Category not found.";
                return RedirectToAction(nameof(Setup));
            }

            if (cat.Ingredients != null && cat.Ingredients.Any())
            {
                TempData["ToastMessage"] = "Cannot delete category, ingredients are linked.";
                return RedirectToAction(nameof(Setup));
            }

            _context.IngredientCategories.Remove(cat);
            await _context.SaveChangesAsync();

            TempData["ToastMessage"] = "Category deleted.";
            return RedirectToAction(nameof(Setup));
        }

        // ====== INGREDIENT SAVE ======
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveIngredient(Ingredient model)
        {
            // Backend pe CostPerUnit calculate
            if (model.PurchaseQty.HasValue &&
                model.ConsumptionQty.HasValue &&
                model.PurchasePrice.HasValue &&
                model.PurchaseQty > 0 &&
                model.ConsumptionQty > 0)
            {
                var effectiveUnits = model.PurchaseQty.Value / model.ConsumptionQty.Value;
                if (effectiveUnits > 0)
                {
                    model.CostPerUnit = model.PurchasePrice.Value / effectiveUnits;
                }
            }

            if (model.Id == 0)
            {
                _context.Ingredients.Add(model);
                TempData["ToastMessage"] = "Ingredient added successfully.";
            }
            else
            {
                _context.Ingredients.Update(model);
                TempData["ToastMessage"] = "Ingredient updated successfully.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Setup));
        }

        // ====== INGREDIENT DELETE ======
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteIngredient(int id)
        {
            var ing = await _context.Ingredients.FindAsync(id);
            if (ing == null)
            {
                TempData["ToastMessage"] = "Ingredient not found.";
                return RedirectToAction(nameof(Setup));
            }

            _context.Ingredients.Remove(ing);
            await _context.SaveChangesAsync();
            TempData["ToastMessage"] = "Ingredient deleted.";
            return RedirectToAction(nameof(Setup));
        }

        // ====== PURCHASE SCREEN API: /Ingredient/GetIngredientsForPurchase ======
        [HttpGet]
        public async Task<IActionResult> GetIngredientsForPurchase()
        {
            var list = await _context.Ingredients
                .Include(i => i.Category)
                .OrderBy(i => i.Name)
                .ToListAsync();

            var result = list.Select(i => new
            {
                id = i.Id,
                name = i.Name,
                code = i.Code,
                categoryName = i.Category != null ? i.Category.Name : "",
                unit = !string.IsNullOrWhiteSpace(i.ConsumptionUnit)
                        ? i.ConsumptionUnit
                        : i.PurchaseUnit,
                costPerUnit = i.CostPerUnit ?? 0m
            });

            return Json(result);
        }
    }
}
