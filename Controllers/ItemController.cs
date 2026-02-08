using GSoftPosNew.Data;
using GSoftPosNew.Migrations;
using GSoftPosNew.Models;
using GSoftPosNew.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GSoftPosNew.Controllers
{
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
            return View(items);
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
                    .Include(x => x.Category)
                    .Include(x => x.Supplier)
                    .OrderByDescending(x => x.Id)
                    .ToList()
            };

            ViewBag.CategoryList = vm.CategoryList;
            ViewBag.SupplierList = vm.SupplierList;
            ViewBag.UnitList = vm.UnitList;
            ViewBag.LocationList = vm.LocationList;

            ViewBag.RecipeIngredients = _context.Ingredients
                .Include(i => i.Category)
                .OrderBy(i => i.Name)
                .ToList();

            return View(vm);
        }

        // ==================== ADD (POST) NORMAL (reload flow) ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddItemViewModel vm, IFormFile? ImageFile, List<string> MultiBarcodes)
        {
            if (vm?.ItemData == null)
            {
                TempData["Error"] = "Invalid data.";
                return RedirectToAction("Add");
            }

            // IMAGE UPLOAD
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                var filePath = Path.Combine(folder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }
                vm.ItemData.ImagePath = "/images/" + fileName;
            }

            // AUTO ITEM CODE
            if (string.IsNullOrWhiteSpace(vm.ItemData.ItemCode))
            {
                var numericCodes = _context.Items
                    .Select(i => i.ItemCode)
                    .Where(c => !string.IsNullOrEmpty(c) && Regex.IsMatch(c, @"^\d+$"))
                    .Select(c => int.Parse(c));

                int max = numericCodes.Any() ? numericCodes.Max() : 0;
                vm.ItemData.ItemCode = (max + 1).ToString("D4");
            }

            vm.ItemData.ItemCode = (vm.ItemData.ItemCode ?? "").Trim();

            // DUPLICATE CODE CHECK
            bool exists = _context.Items.Any(i => i.ItemCode == vm.ItemData.ItemCode);
            if (exists)
            {
                TempData["Error"] = $"Item Code '{vm.ItemData.ItemCode}' already exists!";

                vm.CategoryList = GetCategoryList();
                vm.SupplierList = GetSupplierList();
                vm.UnitList = GetUnitList();
                vm.LocationList = GetLocationList();
                vm.ExistingItems = _context.Items
                    .Include(x => x.Category)
                    .Include(x => x.Supplier)
                    .OrderByDescending(x => x.Id)
                    .ToList();

                ViewBag.CategoryList = vm.CategoryList;
                ViewBag.SupplierList = vm.SupplierList;
                ViewBag.UnitList = vm.UnitList;
                ViewBag.LocationList = vm.LocationList;

                ViewBag.RecipeIngredients = _context.Ingredients
                    .Include(i => i.Category)
                    .OrderBy(i => i.Name)
                    .ToList();

                return View(vm);
            }

            // NORMALIZATION
            if (vm.ItemData.Quantity < 0) vm.ItemData.Quantity = 0;
            if (string.IsNullOrWhiteSpace(vm.ItemData.GenericName))
                vm.ItemData.GenericName = vm.ItemData.ItemName;

            // SAVE ITEM
            _context.Items.Add(vm.ItemData);
            await _context.SaveChangesAsync();

            // SAVE MULTI BARCODES (if provided)
            SaveMultiBarcodesInternal(vm.ItemData.Id, MultiBarcodes);

            // SAVE RECIPE INGREDIENTS
            SaveRecipeInternal(vm);
            await _context.SaveChangesAsync(); // ✅ important

            TempData["Success"] = "Item saved successfully!";
            return RedirectToAction("Add");
        }

        // ============================================================
        // ✅ ADD AJAX (NO RELOAD) => INSERT + UPDATE (ExistingItemId se)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAjax(AddItemViewModel vm, IFormFile? ImageFile, int ExistingItemId)
        {
            if (vm?.ItemData == null)
                return BadRequest(new { ok = false, error = "Invalid data." });

            if (vm?.ItemData == null)
                return BadRequest(new { ok = false, error = "Invalid data." });

            // ✅ yahan paste karo
            ModelState.Remove("ItemData.Category");
            ModelState.Remove("ItemData.Supplier");
            ModelState.Remove("ItemData.Unit");
            ModelState.Remove("ItemData.Location");

            //if (!ModelState.IsValid)
            //{
            //    var err = ModelState.Values.SelectMany(v => v.Errors)
            //        .Select(e => e.ErrorMessage).FirstOrDefault() ?? "Validation failed.";
            //    return BadRequest(new { ok = false, error = err });
            //}



            // ✅ trim code
            vm.ItemData.ItemCode = (vm.ItemData.ItemCode ?? "").Trim();

            // ✅ UPDATE MODE (ExistingItemId > 0)
            if (vm.ItemData.Id > 0)
            {
                var tracked = _context.Items.FirstOrDefault(x => x.Id == vm.ItemData.Id);
                if (tracked == null)
                {
                    TempData["Error"] = "Item not found.";
                    return RedirectToAction("Index");
                }

                if (ImageFile != null && ImageFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(tracked.ImagePath))
                    {
                        var oldFilePath = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            tracked.ImagePath.TrimStart('/')
                        );
                        if (System.IO.File.Exists(oldFilePath))
                            System.IO.File.Delete(oldFilePath);
                    }

                    var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                    var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    var filePath = Path.Combine(folder, fileName);
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

                // ✅ make sure vm.ItemData.Id matches tracked
                vm.ItemData.Id = tracked.Id;

                _context.Entry(tracked).CurrentValues.SetValues(vm.ItemData);
                await _context.SaveChangesAsync();

                // ✅ OPTIONAL: update recipe for this item (delete old then add new)
                // (Agar recipe feature use ho raha hai)
                try
                {
                    // remove old recipe rows
                    var oldRows = _context.ItemIngredients.Where(x => x.ItemId == tracked.Id);
                    _context.ItemIngredients.RemoveRange(oldRows);
                    await _context.SaveChangesAsync();

                    // add new
                    SaveRecipeInternal(vm);
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    // ignore if recipe tables not used
                }

                return Json(new
                {
                    ok = true,
                    itemId = tracked.Id,
                    itemCode = tracked.ItemCode,
                    message = "Item updated successfully!"
                });
            }

            // ✅ INSERT MODE (New item)
            // AUTO ITEM CODE
            if (string.IsNullOrWhiteSpace(vm.ItemData.ItemCode))
            {
                var numericCodes = _context.Items
                    .Select(i => i.ItemCode)
                    .Where(c => !string.IsNullOrEmpty(c) && Regex.IsMatch(c, @"^\d+$"))
                    .Select(c => int.Parse(c));

                int max = numericCodes.Any() ? numericCodes.Max() : 0;
                vm.ItemData.ItemCode = (max + 1).ToString("D4");
                vm.ItemData.ItemCode = (vm.ItemData.ItemCode ?? "").Trim();
            }

            // DUPLICATE CODE CHECK
            bool exists = await _context.Items.AnyAsync(i => i.ItemCode == vm.ItemData.ItemCode);
            if (exists)
                return BadRequest(new { ok = false, error = $"Item Code '{vm.ItemData.ItemCode}' already exists!" });

            // IMAGE UPLOAD
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                var filePath = Path.Combine(folder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }
                vm.ItemData.ImagePath = "/images/" + fileName;
            }

            // NORMALIZATION
            if (vm.ItemData.Quantity < 0) vm.ItemData.Quantity = 0;
            if (string.IsNullOrWhiteSpace(vm.ItemData.GenericName))
                vm.ItemData.GenericName = vm.ItemData.ItemName;

            // SAVE ITEM
            _context.Items.Add(vm.ItemData);
            await _context.SaveChangesAsync();

            // SAVE RECIPE
            SaveRecipeInternal(vm);
            await _context.SaveChangesAsync();

            return Json(new
            {
                ok = true,
                itemId = vm.ItemData.Id,
                itemCode = vm.ItemData.ItemCode,
                message = "Item saved successfully!"
            });
        }


        // ============================================================
        // ✅ SAVE MULTI BARCODES AJAX (NO RELOAD)
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveMultiBarcodesAjax([FromBody] SaveMultiBarcodesDto model)
        {
            if (model == null || model.ItemId <= 0)
                return Json(new { ok = false, error = "Invalid item id" });

            if (model.MultiBarcodes == null || model.MultiBarcodes.Count == 0)
                return Json(new { ok = false, error = "No barcodes received" });

            // ✅ Clean + distinct
            var cleaned = model.MultiBarcodes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (cleaned.Count == 0)
                return Json(new { ok = false, error = "No valid barcodes" });

            // ✅ Item exist check
            var itemExists = _context.Items.Any(x => x.Id == model.ItemId);
            if (!itemExists)
                return Json(new { ok = false, error = "Item not found" });

            // ✅ Check duplicates in DB (GLOBAL)
            // Agar aap chahtay ho ke 1 barcode sirf 1 item ke sath ho => global check zaroori hai
            var existingGlobal = _context.MultiBarcodes
                .Where(m => cleaned.Contains(m.Barcode))
                .Select(m => new { m.Barcode, m.ItemId })
                .ToList();

            // ✅ If barcode already linked to another item -> BLOCK and tell user
            var conflict = existingGlobal
                .Where(x => x.ItemId != model.ItemId)
                .Select(x => x.Barcode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (conflict.Any())
            {
                return Json(new
                {
                    ok = false,
                    error = "These barcodes already exist for another item: " + string.Join(", ", conflict)
                });
            }

            // ✅ For same item duplicates -> just skip
            var alreadyForSameItem = existingGlobal
                .Where(x => x.ItemId == model.ItemId)
                .Select(x => x.Barcode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var toInsert = cleaned
                .Where(bc => !alreadyForSameItem.Contains(bc, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (toInsert.Count == 0)
                return Json(new { ok = true, count = 0, message = "Nothing new to save." });

            foreach (var bc in toInsert)
            {
                _context.MultiBarcodes.Add(new MultiBarcodes
                {
                    ItemId = model.ItemId,
                    Barcode = bc
                });
            }

            _context.SaveChanges();
            return Json(new { ok = true, count = toInsert.Count, message = "Multi barcodes saved." });
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult SaveMultiBarcodesAjax(int itemId, List<string> barcodes)
        //{
        //    if (itemId <= 0)
        //        return BadRequest(new { ok = false, error = "Invalid itemId." });

        //    var item = _context.Items.FirstOrDefault(x => x.Id == itemId);
        //    if (item == null)
        //        return BadRequest(new { ok = false, error = "Item not found." });

        //    var cleaned = (barcodes ?? new List<string>())
        //        .Where(x => !string.IsNullOrWhiteSpace(x))
        //        .Select(x => x.Trim())
        //        .Distinct(StringComparer.OrdinalIgnoreCase)
        //        .ToList();

        //    if (cleaned.Count == 0)
        //        return Json(new { ok = true, count = 0, message = "No barcodes to save." });

        //    foreach (var bc in cleaned)
        //    {
        //        bool already = _context.MultiBarcodes.Any(m => m.ItemId == itemId && m.Barcode == bc);
        //        if (already) continue;

        //        _context.MultiBarcodes.Add(new MultiBarcodes
        //        {
        //            ItemId = itemId,
        //            Barcode = bc
        //        });
        //    }

        //    _context.SaveChanges();
        //    return Json(new { ok = true, count = cleaned.Count, message = "Multi barcodes saved." });
        //}

        // ==================== ✅ DELETE AJAX (NO RELOAD) ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAjax(int id)
        {
            if (id <= 0) return BadRequest(new { ok = false, error = "Invalid id" });

            var item = _context.Items.FirstOrDefault(x => x.Id == id);
            if (item == null) return NotFound(new { ok = false, error = "Item not found" });

            _context.Items.Remove(item);
            _context.SaveChanges();

            return Json(new { ok = true, message = "Item deleted." });
        }

        // ==================== ✅ VIEW CALLS THIS: CheckCode ====================
        [HttpGet]
        public IActionResult CheckCode(string code)
        {
            code = (code ?? "").Trim();
            if (string.IsNullOrWhiteSpace(code))
                return Json(new { exists = false });

            var item = _context.Items
                .Include(x => x.Supplier)
                .Include(x => x.Category)
                .FirstOrDefault(x => x.ItemCode == code);

            if (item == null)
                return Json(new { exists = false });

            return Json(new
            {
                exists = true,
                item = new
                {
                    id = item.Id,
                    itemCode = item.ItemCode,
                    referenceCode = item.ReferenceCode,
                    itemName = item.ItemName,
                    flavour = item.Flavour,
                    salePrice = item.SalePrice,
                    purchasePrice = item.PurchasePrice,
                    markupPercentage = item.MarkupPercentage,
                    quantity = item.Quantity,
                    packSize = item.PackSize,
                    unitPrice = item.UnitPrice,
                    packPrice = item.PackPrice,
                    categoryId = item.CategoryId,
                    supplierId = item.SupplierId
                }
            });
        }

        // ==================== ✅ Stock Search Ajax (Recent 20 + DB Search) ====================
        [HttpGet]
        public async Task<IActionResult> SearchStockAjax(
            string search = "",
            string supplier = "",
            string category = "",
            int take = 0,
            bool recent = false)
        {
            search = (search ?? "").Trim();
            supplier = (supplier ?? "").Trim();
            category = (category ?? "").Trim();

            if (take <= 0) take = 20;

            var q = _context.Items
                .AsNoTracking()
                .Include(x => x.Supplier)
                .Include(x => x.Category)
                .AsQueryable();

            bool noFilter = string.IsNullOrWhiteSpace(search)
                            && string.IsNullOrWhiteSpace(supplier)
                            && string.IsNullOrWhiteSpace(category);

            if (recent && noFilter)
            {
                var recentData = await q
                    .OrderByDescending(x => x.Id)
                    .Take(take)
                    .Select(x => new
                    {
                        id = x.Id,
                        itemCode = x.ItemCode,
                        itemName = x.ItemName,
                        quantity = x.Quantity,
                        purchasePrice = x.PurchasePrice,
                        salePrice = x.SalePrice,
                        supplierName = x.Supplier != null ? x.Supplier.SupplierName : "",
                        categoryName = x.Category != null ? x.Category.Name : ""
                    })
                    .ToListAsync();

                return Json(recentData);
            }

            // Normal search
            if (!string.IsNullOrWhiteSpace(search))
            {
                q = q.Where(x =>
                    x.ItemName.Contains(search) ||
                    x.ItemCode.Contains(search) ||
                    (x.Supplier != null && x.Supplier.SupplierName.Contains(search)) ||
                    (x.Category != null && x.Category.Name.Contains(search))
                );
            }

            if (!string.IsNullOrWhiteSpace(supplier))
                q = q.Where(x => x.Supplier != null && x.Supplier.SupplierName == supplier);

            if (!string.IsNullOrWhiteSpace(category))
                q = q.Where(x => x.Category != null && x.Category.Name == category);

            var data = await q
                .OrderByDescending(x => x.Id)
                .Take(take)
                .Select(x => new
                {
                    id = x.Id,
                    itemCode = x.ItemCode,
                    itemName = x.ItemName,
                    quantity = x.Quantity,
                    purchasePrice = x.PurchasePrice,
                    salePrice = x.SalePrice,
                    supplierName = x.Supplier != null ? x.Supplier.SupplierName : "",
                    categoryName = x.Category != null ? x.Category.Name : ""
                })
                .ToListAsync();

            return Json(data);
        }

        // ==================== AJAX: CHECK ITEM CODE (old) ====================
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
                ExistingItems = _context.Items
                    .Include(x => x.Category)
                    .Include(x => x.Supplier)
                    .OrderByDescending(x => x.Id)
                    .ToList()
            };

            ViewBag.CategoryList = vm.CategoryList;
            ViewBag.SupplierList = vm.SupplierList;
            ViewBag.UnitList = vm.UnitList;
            ViewBag.LocationList = vm.LocationList;

            ViewBag.RecipeIngredients = _context.Ingredients
                .Include(i => i.Category)
                .OrderBy(i => i.Name)
                .ToList();

            return View(vm);
        }

        // ==================== EDIT (POST) ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AddItemViewModel vm, IFormFile? ImageFile)
        {
            var tracked = _context.Items.FirstOrDefault(x => x.Id == vm.ItemData.Id);
            if (tracked == null)
            {
                TempData["Error"] = "Item not found.";
                return RedirectToAction("Index");
            }

            if (ImageFile != null && ImageFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(tracked.ImagePath))
                {
                    var oldFilePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        tracked.ImagePath.TrimStart('/')
                    );
                    if (System.IO.File.Exists(oldFilePath))
                        System.IO.File.Delete(oldFilePath);
                }

                var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                var filePath = Path.Combine(folder, fileName);
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

            // ✅ make sure vm.ItemData.Id matches tracked
            vm.ItemData.Id = tracked.Id;

            _context.Entry(tracked).CurrentValues.SetValues(vm.ItemData);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Item updated successfully!";
            return RedirectToAction("Add");
        }

        // ==================== DELETE (POST) NORMAL ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
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
                .Select(i => i.ItemCode)
                .AsEnumerable()
                .Where(c => !string.IsNullOrWhiteSpace(c) && int.TryParse(c, out _))
                .Select(int.Parse);

            int max = numericCodes.Any() ? numericCodes.Max() : 0;
            string next = (max + 1).ToString("D4");
            return Json(next);
        }

        // ==================== HELPERS ====================
        private List<SelectListItem> GetCategoryList()
        {
            return _context.Categories
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToList();
        }

        private List<SelectListItem> GetSupplierList()
        {
            return _context.Suppliers
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.SupplierName })
                .ToList();
        }

        private List<SelectListItem> GetUnitList()
        {
            return _context.Units
                .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Name })
                .OrderBy(u => u.Text)
                .ToList();
        }

        private List<SelectListItem> GetLocationList()
        {
            return _context.Locations
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name })
                .OrderBy(l => l.Text)
                .ToList();
        }

        [HttpGet]
        public IActionResult GetItemByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Json(new { ok = false, exists = false });

            code = code.Trim();

            // ✅ apne DB context ka name yahan use karo (example: _context)
            var it = _context.Items
                .Where(x => x.ItemCode == code)
                .Select(x => new
                {
                    id = x.Id,
                    itemCode = x.ItemCode,
                    referenceCode = x.ReferenceCode,
                    itemName = x.ItemName,
                    flavour = x.Flavour,

                    categoryId = x.CategoryId,
                    supplierId = x.SupplierId,

                    salePrice = x.SalePrice,
                    purchasePrice = x.PurchasePrice,
                    markupPercentage = x.MarkupPercentage,

                    quantity = x.Quantity,

                    packSize = x.PackSize,
                    unitId = x.UnitId,

                    locationId = x.LocationId,

                    unitPrice = x.UnitPrice,
                    packPrice = x.PackPrice
                })
                .FirstOrDefault();

            if (it == null)
                return Json(new { ok = true, exists = false });

            return Json(new { ok = true, exists = true, item = it });
        }


        // ==================== INTERNAL HELPERS (private) ====================
        private void SaveMultiBarcodesInternal(int itemId, List<string> multiBarcodes)
        {
            if (multiBarcodes == null || multiBarcodes.Count == 0) return;

            var cleaned = multiBarcodes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var bc in cleaned)
            {
                bool already = _context.MultiBarcodes.Any(m => m.ItemId == itemId && m.Barcode == bc);
                if (already) continue;

                _context.MultiBarcodes.Add(new MultiBarcodes
                {
                    ItemId = itemId,
                    Barcode = bc
                });
            }

            _context.SaveChanges();
        }

        private void SaveRecipeInternal(AddItemViewModel vm)
        {
            if (vm?.RecipeIngredients == null || !vm.RecipeIngredients.Any()) return;

            foreach (var r in vm.RecipeIngredients)
            {
                if (r.IngredientId <= 0) continue;

                var ingredient = _context.Ingredients.FirstOrDefault(x => x.Id == r.IngredientId);
                if (ingredient == null) continue;

                _context.ItemIngredients.Add(new ItemIngredient
                {
                    ItemId = vm.ItemData.Id,
                    IngredientId = r.IngredientId,
                    UseQty = r.UseQty,
                    Unit = r.Unit,
                    CostPerUnit = r.CostPerUnit,
                    LineTotal = r.LineTotal
                });
            }
        }
    }
}
