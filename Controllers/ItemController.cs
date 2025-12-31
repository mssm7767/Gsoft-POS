using GSoftPosNew.Data;
using GSoftPosNew.Models;
using GSoftPosNew.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace GSoftPosNew.Controllers
{
    [Authorize]
    public class ItemController : Controller
    {
        private readonly AppDbContext _context;

        public ItemController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Item
        public IActionResult Index()
        {
            var items = _context.Items
                .Include(i => i.Category)
                .Include(i => i.Supplier)
                .OrderBy(i => i.ItemName)
                .ToList();

            ViewBag.CategoryList = _context.Categories.ToList();

            return View(items); // Points to Views/Item/Index.cshtml
        }

        // ==================== ADD (GET) ====================
        [HttpGet]
        public IActionResult Add()
        {
            var vm = new AddItemViewModel
            {
                ItemData = new ItemModel(),
                CategoryList = GetCategoryList(),
                SupplierList = GetSupplierList(),
                UnitList = GetUnitList(),
                LocationList = GetLocationList(),
                ExistingItems = _context.Items
                    .OrderByDescending(x => x.Id)
                    .Take(20)
                    .ToList(),

            };

            ViewBag.CategoryList = vm.CategoryList;
            ViewBag.SupplierList = vm.SupplierList;
            ViewBag.UnitList = vm.UnitList;
            ViewBag.LocationList = vm.LocationList;
            ViewBag.ExistingItems = vm.ExistingItems;

            // ✅ Recipe ke liye ingredients list (UI dropdown me use hogi)
            ViewBag.RecipeIngredients = _context.Ingredients
                .Include(i => i.Category)
                .OrderBy(i => i.Name)
                .ToList();

            return View(vm);
        }

        // ==================== ADD (POST) ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddItemViewModel vm, IFormFile? ImageFile)
        {
            // ----- IMAGE UPLOAD -----
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                vm.ItemData.ImagePath = "/images/" + fileName;
            }

            // ----- AUTO ITEM CODE (agar khali ho) -----
            if (string.IsNullOrWhiteSpace(vm.ItemData.ItemCode))
            {
                var numericCodes = _context.Items
                    .Select(i => i.ItemCode)
                    .Where(c => !string.IsNullOrEmpty(c) && Regex.IsMatch(c, @"^\d+$"))
                    .Select(c => int.Parse(c));

                int max = numericCodes.Any() ? numericCodes.Max() : 0;
                vm.ItemData.ItemCode = (max + 1).ToString("D4");
            }

            // ----- DUPLICATE CODE CHECK -----
            bool exists = _context.Items.Any(i => i.ItemCode == vm.ItemData.ItemCode);
            if (exists)
            {
                TempData["Error"] = $"Item Code '{vm.ItemData.ItemCode}' already exists!";

                // dropdowns + existing items wapas set
                vm.CategoryList = GetCategoryList();
                vm.SupplierList = GetSupplierList();
                vm.UnitList = GetUnitList();
                vm.LocationList = GetLocationList();
                vm.ExistingItems = _context.Items.ToList();

                ViewBag.CategoryList = vm.CategoryList;
                ViewBag.SupplierList = vm.SupplierList;
                ViewBag.UnitList = vm.UnitList;
                ViewBag.LocationList = vm.LocationList;
                ViewBag.ExistingItems = vm.ExistingItems;

                // ✅ Recipe ingredients bhi wapas set karo (warna null aayega)
                ViewBag.RecipeIngredients = _context.Ingredients
                    .Include(i => i.Category)
                    .OrderBy(i => i.Name)
                    .ToList();

                return View(vm);
            }

            // ----- BASIC NORMALIZATION -----
            if (vm.ItemData.Quantity < 0)
                vm.ItemData.Quantity = 0;

            if (string.IsNullOrEmpty(vm.ItemData.GenericName))
                vm.ItemData.GenericName = vm.ItemData.ItemName;

            // ----- SAVE ITEM -----
            _context.Items.Add(vm.ItemData);
            _context.SaveChanges();

            // ----- SAVE RECIPE INGREDIENTS INTO ItemIngredient TABLE + UPDATE STOCK -----
            if (vm.RecipeIngredients != null && vm.RecipeIngredients.Any())
            {
                foreach (var r in vm.RecipeIngredients)
                {
                    // Get ingredient record from DB
                    var ingredient = _context.Ingredients
                        .FirstOrDefault(x => x.Id == r.IngredientId);

                    if (ingredient != null)
                    {
                        // 1️⃣ Save into ItemIngredient table
                        var itemIngredient = new ItemIngredient
                        {
                            ItemId = vm.ItemData.Id,     // <- Here is the Item ID
                            IngredientId = r.IngredientId,
                            UseQty = r.UseQty,
                            Unit = r.Unit,
                            CostPerUnit = r.CostPerUnit,
                            LineTotal = r.LineTotal
                        };

                        _context.ItemIngredients.Add(itemIngredient);

                    }
                }

                // Save ItemIngredients + stock update
                _context.SaveChanges();
            }




            TempData["Success"] = "Item saved successfully!";
            return RedirectToAction("Add");
        }

        // ==================== AJAX: CHECK ITEM CODE ====================
        [HttpGet]
        public JsonResult CheckItemCode(string itemCode)
        {
            bool exists = _context.Items.Any(x => x.ItemCode == itemCode);
            return Json(exists);
        }

        // ==================== AJAX: CHECK ITEM NAME ====================
        [HttpGet]
        public JsonResult CheckItemName(string itemName)
        {
            bool exists = _context.Items.Any(x => x.ItemName == itemName);
            return Json(exists);
        }

        // ==================== EDIT (GET) ====================
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var item = _context.Items.Find(id);
            if (item == null) return NotFound();

            var vm = new AddItemViewModel
            {
                ItemData = item,
                CategoryList = GetCategoryList(),
                SupplierList = GetSupplierList(),
                UnitList = GetUnitList(),
                LocationList = GetLocationList(),
                ExistingItems = _context.Items.ToList()
            };

            ViewBag.CategoryList = vm.CategoryList;
            ViewBag.SupplierList = vm.SupplierList;
            ViewBag.UnitList = vm.UnitList;
            ViewBag.LocationList = vm.LocationList;
            ViewBag.ExistingItems = vm.ExistingItems;

            // ✅ Edit pe bhi recipe ke liye ingredients dropdown bhar do
            ViewBag.RecipeIngredients = _context.Ingredients
                .Include(i => i.Category)
                .OrderBy(i => i.Name)
                .ToList();

            return View(vm);
        }

        // ==================== EDIT (POST) ====================
        [HttpPost]
        public async Task<IActionResult> Edit(AddItemViewModel vm, IFormFile? ImageFile)
        {
            var tracked = _context.Items.FirstOrDefault(x => x.Id == vm.ItemData.Id);
            if (tracked == null)
            {
                TempData["Error"] = "Item not found.";
                return RedirectToAction("Index");
            }

            // ----- IMAGE HANDLING -----
            if (ImageFile != null && ImageFile.Length > 0)
            {
                // Purani image delete (optional)
                if (!string.IsNullOrEmpty(tracked.ImagePath))
                {
                    var oldFilePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        tracked.ImagePath.TrimStart('/')
                    );
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Nayi image save
                var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                vm.ItemData.ImagePath = "/images/" + fileName;
            }
            else
            {
                vm.ItemData.ImagePath = tracked.ImagePath;
            }

            // ----- UPDATE ITEM -----
            _context.Entry(tracked).CurrentValues.SetValues(vm.ItemData);
            _context.SaveChanges();

            // NOTE: yahan bhi future me is item ke purane recipe rows delete karke
            // naye recipe rows add kar sakte hain, jab ItemRecipe table ready ho.

            TempData["Success"] = "Item updated successfully!";
            return RedirectToAction("Add");
        }

        // ==================== DELETE ====================
        public IActionResult Delete(int id)
        {
            var item = _context.Items.Find(id);
            if (item != null)
            {
                _context.Items.Remove(item);
                _context.SaveChanges();
                TempData["Danger"] = "Item deleted.";
            }
            return RedirectToAction("Index");
        }

        // ==================== NEXT ITEM CODE (AJAX) ====================
        [HttpGet]
        public IActionResult GetNextCode()
        {
            var numericCodes = _context.Items
                .Where(i => i.ItemCode != null && i.ItemCode != "" && EF.Functions.Like(i.ItemCode, "%[0-9]%"))
                .Select(i => i.ItemCode)
                .AsEnumerable()
                .Where(c => int.TryParse(c, out _))
                .Select(c => int.Parse(c));

            int max = numericCodes.Any() ? numericCodes.Max() : 0;
            string next = (max + 1).ToString("D4");
            return Json(next);
        }

        // ==================== HELPERS ====================
        private List<SelectListItem> GetCategoryList()
        {
            return _context.Categories
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToList();
        }

        private List<SelectListItem> GetSupplierList()
        {
            return _context.Suppliers
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SupplierName
                })
                .ToList();
        }

        private List<SelectListItem> GetUnitList()
        {
            return _context.Units
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.Name
                })
                .OrderBy(u => u.Text)
                .ToList();
        }

        private List<SelectListItem> GetLocationList()
        {
            return _context.Locations
                .Select(l => new SelectListItem
                {
                    Value = l.Id.ToString(),
                    Text = l.Name
                })
                .OrderBy(l => l.Text)
                .ToList();
        }
    }
}
