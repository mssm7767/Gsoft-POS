using GSoftPosNew.Data;
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

        // ============================================================
        // ✅ INDEX
        // ============================================================
        public IActionResult Index()
        {
            return View();  // ✅ ab heavy list yahan se nahi jayegi
        }


        // ============================================================
        // ✅ ADD (GET)
        // ============================================================
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
    .AsNoTracking()
    .Include(x => x.Category)
    .Include(x => x.Supplier)
    .OrderByDescending(x => x.Id)
    .Take(15)
    .ToList()

            };

            ViewBag.CategoryList = vm.CategoryList;
            ViewBag.SupplierList = vm.SupplierList;
            ViewBag.UnitList = vm.UnitList;
            ViewBag.LocationList = vm.LocationList;

            ViewBag.RecipeIngredients = _context.Ingredients
                .AsNoTracking()
                .Include(i => i.Category)
                .OrderBy(i => i.Name)
                .ToList();

            return View(vm);
        }

        // ============================================================
        // ✅ ADD (POST) NORMAL (reload)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddItemViewModel vm, IFormFile? ImageFile, List<string> MultiBarcodes)
        {
            if (vm?.ItemData == null)
            {
                TempData["Error"] = "Invalid data.";
                return RedirectToAction("Add");
            }

            vm.ItemData.ItemCode = (vm.ItemData.ItemCode ?? "").Trim();
            vm.ItemData.ItemName = (vm.ItemData.ItemName ?? "").Trim();

            // ✅ image upload
            if (ImageFile != null && ImageFile.Length > 0)
                vm.ItemData.ImagePath = await SaveImageAndGetPathAsync(ImageFile);

            // ✅ auto code
            if (string.IsNullOrWhiteSpace(vm.ItemData.ItemCode))
                vm.ItemData.ItemCode = await GetNextNumericCodeAsync();

            // ✅ duplicate code check (insert)
            bool exists = await _context.Items.AnyAsync(i => i.ItemCode == vm.ItemData.ItemCode);
            if (exists)
            {
                TempData["Error"] = $"Item Code '{vm.ItemData.ItemCode}' already exists!";

                // reload lists
                vm.CategoryList = GetCategoryList();
                vm.SupplierList = GetSupplierList();
                vm.UnitList = GetUnitList();
                vm.LocationList = GetLocationList();
                vm.ExistingItems = _context.Items
      .AsNoTracking()
      .Include(x => x.Category)
      .Include(x => x.Supplier)
      .OrderByDescending(x => x.Id)
      .Take(15)
      .ToList();


                ViewBag.CategoryList = vm.CategoryList;
                ViewBag.SupplierList = vm.SupplierList;
                ViewBag.UnitList = vm.UnitList;
                ViewBag.LocationList = vm.LocationList;

                ViewBag.RecipeIngredients = _context.Ingredients
                    .AsNoTracking()
                    .Include(i => i.Category)
                    .OrderBy(i => i.Name)
                    .ToList();

                return View(vm);
            }

            if (vm.ItemData.Quantity < 0) vm.ItemData.Quantity = 0;
            if (string.IsNullOrWhiteSpace(vm.ItemData.GenericName))
                vm.ItemData.GenericName = vm.ItemData.ItemName;

            _context.Items.Add(vm.ItemData);
            await _context.SaveChangesAsync();

            SaveMultiBarcodesInternal(vm.ItemData.Id, MultiBarcodes);
            SaveRecipeInternal(vm);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Item saved successfully!";
            return RedirectToAction("Add");
        }

        // ============================================================
        // ✅ ADD AJAX (INSERT + UPDATE)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAjax(AddItemViewModel vm, IFormFile? ImageFile, int ExistingItemId)
        {
            if (vm?.ItemData == null)
                return BadRequest(new { ok = false, error = "Invalid data." });

            // ✅ remove navigation validation
            ModelState.Remove("ItemData.Category");
            ModelState.Remove("ItemData.Supplier");
            ModelState.Remove("ItemData.Unit");
            ModelState.Remove("ItemData.Location");

            vm.ItemData.ItemCode = (vm.ItemData.ItemCode ?? "").Trim();
            vm.ItemData.ItemName = (vm.ItemData.ItemName ?? "").Trim();

            // =========================
            // ✅ UPDATE MODE
            // =========================
            if (vm.ItemData.Id > 0)
            {
                var tracked = await _context.Items.FirstOrDefaultAsync(x => x.Id == vm.ItemData.Id);
                if (tracked == null)
                    return NotFound(new { ok = false, error = "Item not found." });

                // ✅ IMPORTANT: code duplicate check in update mode (ignore same id)
                if (!string.IsNullOrWhiteSpace(vm.ItemData.ItemCode))
                {
                    bool codeDup = await _context.Items.AnyAsync(x => x.ItemCode == vm.ItemData.ItemCode && x.Id != tracked.Id);
                    if (codeDup)
                        return BadRequest(new { ok = false, error = $"Item Code '{vm.ItemData.ItemCode}' already exists!" });
                }

                // ✅ image
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    DeleteOldImageIfAny(tracked.ImagePath);
                    vm.ItemData.ImagePath = await SaveImageAndGetPathAsync(ImageFile);
                }
                else
                {
                    vm.ItemData.ImagePath = tracked.ImagePath;
                }

                vm.ItemData.Id = tracked.Id;
                _context.Entry(tracked).CurrentValues.SetValues(vm.ItemData);
                await _context.SaveChangesAsync();

                // ✅ recipe update
                try
                {
                    var oldRows = _context.ItemIngredients.Where(x => x.ItemId == tracked.Id);
                    _context.ItemIngredients.RemoveRange(oldRows);
                    await _context.SaveChangesAsync();

                    SaveRecipeInternal(vm);
                    await _context.SaveChangesAsync();
                }
                catch { }

                return Json(new
                {
                    ok = true,
                    itemId = tracked.Id,
                    itemCode = tracked.ItemCode,
                    message = "Item updated successfully!"
                });
            }

            // =========================
            // ✅ INSERT MODE
            // =========================
            if (string.IsNullOrWhiteSpace(vm.ItemData.ItemCode))
                vm.ItemData.ItemCode = await GetNextNumericCodeAsync();

            bool exists = await _context.Items.AnyAsync(i => i.ItemCode == vm.ItemData.ItemCode);
            if (exists)
                return BadRequest(new { ok = false, error = $"Item Code '{vm.ItemData.ItemCode}' already exists!" });

            if (ImageFile != null && ImageFile.Length > 0)
                vm.ItemData.ImagePath = await SaveImageAndGetPathAsync(ImageFile);

            if (vm.ItemData.Quantity < 0) vm.ItemData.Quantity = 0;
            if (string.IsNullOrWhiteSpace(vm.ItemData.GenericName))
                vm.ItemData.GenericName = vm.ItemData.ItemName;

            _context.Items.Add(vm.ItemData);
            await _context.SaveChangesAsync();

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
        // ✅ GET ITEM BY CODE (for barcode scanning load)
        // ============================================================
        [HttpGet]
        public IActionResult GetItemByCode(string code)
        {
            code = (code ?? "").Trim();
            if (string.IsNullOrWhiteSpace(code))
                return Json(new { ok = true, exists = false });

            var it = _context.Items
                .AsNoTracking()
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

        // ============================================================
        // ✅ INLINE EDIT (STOCK TABLE)
        // ============================================================
        [HttpGet]
        public IActionResult GetItemForEditAjax(int id)
        {
            if (id <= 0) return BadRequest(new { ok = false, error = "Invalid id" });

            var it = _context.Items
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    id = x.Id,
                    itemCode = x.ItemCode,
                    referenceCode = x.ReferenceCode,
                    itemName = x.ItemName,
                    flavour = x.Flavour,

                    categoryId = x.CategoryId,
                    supplierId = x.SupplierId,
                    unitId = x.UnitId,
                    locationId = x.LocationId,

                    salePrice = x.SalePrice,
                    purchasePrice = x.PurchasePrice,
                    markupPercentage = x.MarkupPercentage,

                    quantity = x.Quantity,

                    packSize = x.PackSize,
                    unitPrice = x.UnitPrice,
                    packPrice = x.PackPrice
                })
                .FirstOrDefault();

            if (it == null) return NotFound(new { ok = false, error = "Item not found" });

            return Json(new { ok = true, item = it });
        }

        // ============================================================
        // ✅ DELETE AJAX (FAST + FK SAFE)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            if (id <= 0) return BadRequest(new { ok = false, error = "Invalid id" });

            var item = await _context.Items.FirstOrDefaultAsync(x => x.Id == id);
            if (item == null) return NotFound(new { ok = false, error = "Item not found" });

            try
            {
                var mbs = _context.MultiBarcodes.Where(x => x.ItemId == id);
                _context.MultiBarcodes.RemoveRange(mbs);

                var recipe = _context.ItemIngredients.Where(x => x.ItemId == id);
                _context.ItemIngredients.RemoveRange(recipe);

                await _context.SaveChangesAsync();
            }
            catch { }

            try { DeleteOldImageIfAny(item.ImagePath); } catch { }

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            return Json(new { ok = true, message = "Item deleted." });
        }

        // ============================================================
        // ✅ STOCK SEARCH AJAX
        // ============================================================
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

        // ============================================================
        // ✅ NEXT ITEM CODE
        // ============================================================
        [HttpGet]
        public IActionResult GetNextCode()
        {
            int maxId = _context.Items.Max(x => (int?)x.Id) ?? 0;
            int next = maxId + 1;

            // 6 digit serial
            return Content(next.ToString("D6"));
        }


        // ============================================================
        // ✅ CHECK NAME / CODE (FIXED for EDIT mode)
        // ============================================================
        [HttpGet]
        public JsonResult CheckItemCode(string itemCode, int id = 0)
        {
            itemCode = (itemCode ?? "").Trim();
            if (string.IsNullOrWhiteSpace(itemCode))
                return Json(false);

            bool exists = _context.Items.Any(x => x.ItemCode == itemCode && x.Id != id);
            return Json(exists);
        }

        [HttpGet]
        public JsonResult CheckItemName(string itemName, int id = 0)
        {
            itemName = (itemName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(itemName))
                return Json(false);

            // ✅ Ignore same item during update
            bool exists = _context.Items.Any(x => x.ItemName == itemName && x.Id != id);
            return Json(exists);
        }

        // ============================================================
        // ✅ SAVE MULTI BARCODES AJAX
        // ============================================================
        public class SaveMultiBarcodesDto
        {
            public int ItemId { get; set; }
            public List<string> MultiBarcodes { get; set; } = new();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveMultiBarcodesAjax([FromBody] SaveMultiBarcodesDto model)
        {
            if (model == null || model.ItemId <= 0)
                return Json(new { ok = false, error = "Invalid item id" });

            if (model.MultiBarcodes == null || model.MultiBarcodes.Count == 0)
                return Json(new { ok = false, error = "No barcodes received" });

            var cleaned = model.MultiBarcodes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (cleaned.Count == 0)
                return Json(new { ok = false, error = "No valid barcodes" });

            var itemExists = _context.Items.Any(x => x.Id == model.ItemId);
            if (!itemExists)
                return Json(new { ok = false, error = "Item not found" });

            var existingGlobal = _context.MultiBarcodes
                .Where(m => cleaned.Contains(m.Barcode))
                .Select(m => new { m.Barcode, m.ItemId })
                .ToList();

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

        // ============================================================
        // ✅ EDIT (GET/POST) (legacy optional)
        // ============================================================
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
    .AsNoTracking()
    .Include(x => x.Category)
    .Include(x => x.Supplier)
    .OrderByDescending(x => x.Id)
    .Take(15)
    .ToList()

            };

            ViewBag.CategoryList = vm.CategoryList;
            ViewBag.SupplierList = vm.SupplierList;
            ViewBag.UnitList = vm.UnitList;
            ViewBag.LocationList = vm.LocationList;

            ViewBag.RecipeIngredients = _context.Ingredients
                .AsNoTracking()
                .Include(i => i.Category)
                .OrderBy(i => i.Name)
                .ToList();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AddItemViewModel vm, IFormFile? ImageFile)
        {
            var tracked = await _context.Items.FirstOrDefaultAsync(x => x.Id == vm.ItemData.Id);
            if (tracked == null)
            {
                TempData["Error"] = "Item not found.";
                return RedirectToAction("Index");
            }

            if (ImageFile != null && ImageFile.Length > 0)
            {
                DeleteOldImageIfAny(tracked.ImagePath);
                vm.ItemData.ImagePath = await SaveImageAndGetPathAsync(ImageFile);
            }
            else
            {
                vm.ItemData.ImagePath = tracked.ImagePath;
            }

            vm.ItemData.Id = tracked.Id;
            _context.Entry(tracked).CurrentValues.SetValues(vm.ItemData);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Item updated successfully!";
            return RedirectToAction("Add");
        }

        // ============================================================
        // ✅ NORMAL DELETE (legacy)
        // ============================================================
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

        // ============================================================
        // ✅ HELPERS
        // ============================================================
        private List<SelectListItem> GetCategoryList()
        {
            return _context.Categories
                .AsNoTracking()
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToList();
        }

        private List<SelectListItem> GetSupplierList()
        {
            return _context.Suppliers
                .AsNoTracking()
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.SupplierName })
                .ToList();
        }

        private List<SelectListItem> GetUnitList()
        {
            return _context.Units
                .AsNoTracking()
                .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.Name })
                .OrderBy(u => u.Text)
                .ToList();
        }

        private List<SelectListItem> GetLocationList()
        {
            return _context.Locations
                .AsNoTracking()
                .Select(l => new SelectListItem { Value = l.Id.ToString(), Text = l.Name })
                .OrderBy(l => l.Text)
                .ToList();
        }

        private async Task<string> GetNextNumericCodeAsync()
        {
            var codes = await _context.Items
                .AsNoTracking()
                .Select(i => i.ItemCode)
                .ToListAsync();

            var nums = codes
                .Where(c => !string.IsNullOrWhiteSpace(c) && Regex.IsMatch(c.Trim(), @"^\d+$"))
                .Select(c =>
                {
                    if (long.TryParse(c.Trim(), out var n)) return n;
                    return 0;
                })
                .Where(n => n > 0);

            long max = nums.Any() ? nums.Max() : 0;

            // ✅ 00001, 00002 ... 00010 (5 digits)
            return (max + 1).ToString("D5");
        }


        private async Task<string> SaveImageAndGetPathAsync(IFormFile file)
        {
            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var filePath = Path.Combine(folder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return "/images/" + fileName;
        }

        private void DeleteOldImageIfAny(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath)) return;

            var oldFilePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                imagePath.TrimStart('/')
            );

            if (System.IO.File.Exists(oldFilePath))
                System.IO.File.Delete(oldFilePath);
        }

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
        }

        public class ItemRowDto
        {
            public int Id { get; set; }
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
            public string Flavour { get; set; }
            public string CategoryName { get; set; }
            public string SupplierName { get; set; }
            public decimal PurchasePrice { get; set; }
            public decimal SalePrice { get; set; }
            public decimal Quantity { get; set; }
            public int LowStockThreshold { get; set; }
            public bool IsExpired { get; set; }
            public string Barcode { get; set; }
        }

        public class ItemListResponseDto
        {
            public int TotalCount { get; set; }
            public List<ItemRowDto> Items { get; set; } = new();
        }

        [HttpGet]
        public async Task<IActionResult> SearchItemsAjax(
            string search = "",
            string supplier = "",
            string category = "",
            string flavour = "",
            bool low = false,
            int page = 1,
            int pageSize = 25)
        {
            page = page <= 0 ? 1 : page;
            pageSize = (pageSize <= 0 || pageSize > 200) ? 25 : pageSize;

            var q = _context.Items
                .AsNoTracking()
                .Select(x => new
                {
                    x.Id,
                    x.ItemCode,
                    x.ItemName,
                    x.Flavour,
                    CategoryName = x.Category != null ? x.Category.Name : "",
                    SupplierName = x.Supplier != null ? x.Supplier.SupplierName : "",
                    x.PurchasePrice,
                    x.SalePrice,
                    x.Quantity,
                    x.LowStockThreshold,
                    x.ExpiryDate,
                    Barcode = (string)null // ✅ agar aapke model me Barcode field hai to yahan x.Barcode likh do
                })
                .AsQueryable();

            search = (search ?? "").Trim();
            supplier = (supplier ?? "").Trim();
            category = (category ?? "").Trim();
            flavour = (flavour ?? "").Trim();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                q = q.Where(x =>
                    (x.ItemName ?? "").ToLower().Contains(s) ||
                    (x.ItemCode ?? "").ToLower().Contains(s) ||
                    (x.Barcode ?? "").ToLower().Contains(s)
                );
            }

            if (!string.IsNullOrWhiteSpace(supplier))
            {
                var s = supplier.ToLower();
                q = q.Where(x => (x.SupplierName ?? "").ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                var c = category.ToLower();
                q = q.Where(x => (x.CategoryName ?? "").ToLower().Contains(c));
            }

            if (!string.IsNullOrWhiteSpace(flavour))
            {
                var f = flavour.ToLower();
                q = q.Where(x => (x.Flavour ?? "").ToLower().Contains(f));
            }

            if (low)
                q = q.Where(x => x.Quantity <= 5);

            var totalCount = await q.CountAsync();

            var rows = await q
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ItemRowDto
                {
                    Id = x.Id,
                    ItemCode = x.ItemCode,
                    ItemName = x.ItemName,
                    Flavour = x.Flavour,
                    CategoryName = x.CategoryName,
                    SupplierName = x.SupplierName,
                    PurchasePrice = x.PurchasePrice,
                    SalePrice = x.SalePrice,
                    Quantity = x.Quantity,
                    LowStockThreshold = x.LowStockThreshold,
                    IsExpired = x.ExpiryDate.HasValue && x.ExpiryDate.Value < DateTime.Today,
                    Barcode = x.Barcode
                })
                .ToListAsync();

            return Json(new ItemListResponseDto
            {
                TotalCount = totalCount,
                Items = rows
            });
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
