using GSoftPosNew.Data;
using GSoftPosNew.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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

            return View(items); // Points to Views/Item/Index.cshtml
        }

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
                ExistingItems = _context.Items.ToList()
            };

            ViewBag.CategoryList = vm.CategoryList;
            ViewBag.SupplierList = vm.SupplierList;
            ViewBag.UnitList = vm.UnitList;
            ViewBag.LocationList = vm.LocationList;
            ViewBag.ExistingItems = vm.ExistingItems;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddItemViewModel vm, IFormFile? ImageFile)
        {
            if (ImageFile != null && ImageFile.Length > 0)
            {
                // Generate unique file name
                var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);

                // Path to save in wwwroot/images
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                // Save relative path to DB (so Razor can render it with Url.Content)
                vm.ItemData.ImagePath = "/images/" + fileName;
            }

            if (string.IsNullOrWhiteSpace(vm.ItemData.ItemCode))
            {
                var numericCodes = _context.Items
                    .Select(i => i.ItemCode)
                    .Where(c => !string.IsNullOrEmpty(c) && Regex.IsMatch(c, @"^\d+$"))
                    .Select(c => int.Parse(c));

                int max = numericCodes.Any() ? numericCodes.Max() : 0;
                vm.ItemData.ItemCode = (max + 1).ToString("D4");
            }

            // ✅ Check if ItemCode already exists
            bool exists = _context.Items.Any(i => i.ItemCode == vm.ItemData.ItemCode);
            if (exists)
            {
                TempData["Error"] = $"Item Code '{vm.ItemData.ItemCode}' already exists!";

                ViewBag.CategoryList = vm.CategoryList;
                ViewBag.SupplierList = vm.SupplierList;
                ViewBag.UnitList = vm.UnitList;
                ViewBag.LocationList = vm.LocationList;
                ViewBag.ExistingItems = vm.ExistingItems;
                return View(vm);
            }

            //bool existsName = _context.Items.Any(i => i.ItemName == vm.ItemData.ItemName);
            //if (existsName)
            //{
            //    TempData["ErrorName"] = $"Item Name '{vm.ItemData.ItemName}' already exists!";

            //    ViewBag.CategoryList = vm.CategoryList;
            //    ViewBag.SupplierList = vm.SupplierList;
            //    ViewBag.UnitList = vm.UnitList;
            //    ViewBag.LocationList = vm.LocationList;
            //    ViewBag.ExistingItems = vm.ExistingItems;
            //    return View(vm);
            //}

            if (vm.ItemData.Quantity < 0)
                vm.ItemData.Quantity = 0;

            if (string.IsNullOrEmpty(vm.ItemData.GenericName))
                vm.ItemData.GenericName = vm.ItemData.ItemName;

            _context.Items.Add(vm.ItemData);
            _context.SaveChanges();

            TempData["Success"] = "Item saved successfully!";
            return RedirectToAction("Add");
        }

        [HttpGet]
        public JsonResult CheckItemCode(string itemCode)
        {
            bool exists = _context.Items.Any(x => x.ItemCode == itemCode);
            return Json(exists);
        }

        [HttpGet]
        public JsonResult CheckItemName(string itemName)
        {
            bool exists = _context.Items.Any(x => x.ItemName == itemName);
            return Json(exists);
        }


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

            return View(vm);

        }

        [HttpPost]
        public async Task<IActionResult> Edit(AddItemViewModel vm, IFormFile? ImageFile)
        {
            var tracked = _context.Items.FirstOrDefault(x => x.Id == vm.ItemData.Id);
            // Handle image upload
            if (ImageFile != null && ImageFile.Length > 0)
            {
                // Optional: delete old image file if exists
                if (!string.IsNullOrEmpty(tracked.ImagePath))
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", tracked.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Save new image
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

            
            if (tracked != null)
            {
                // Update the tracked entity instead of attaching a new one
                _context.Entry(tracked).CurrentValues.SetValues(vm.ItemData);
            }
            else
            {
                // Attach and mark as modified
                _context.Items.Update(vm.ItemData);
            }

            _context.SaveChanges();
            TempData["Success"] = "Item updated successfully!";
            return RedirectToAction("Add");

        }

    
        public IActionResult Delete(int id)
        {
            var item = _context.Items.Find(id);
            if (item != null)
            {
                _context.Items.Remove(item);
                _context.SaveChanges();
                TempData["Danger"] = "Item deleted.";
                return RedirectToAction("Index");
            }
            return RedirectToAction("Index");
        }

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
