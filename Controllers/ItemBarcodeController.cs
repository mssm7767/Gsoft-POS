using GSoftPosNew.Data;
using GSoftPosNew.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GSoftPosNew.Controllers
{
    public class ItemBarcodeController : Controller
    {
        private readonly AppDbContext _context;

        public ItemBarcodeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var barcodes = _context.ItemBarcodes
                .Include(b => b.Item)
                .ToList();

            ViewBag.ShopName = _context.ShopSettings.OrderByDescending(s => s.Id).Select(s => s.ShopName).FirstOrDefault();

            return View(barcodes);
        }

        public IActionResult Create()
        {
            ViewData["Items"] = new SelectList(_context.Items, "Id", "ItemName");
            ViewBag.ShopName = _context.ShopSettings.OrderByDescending(s => s.Id).Select(s => s.ShopName).FirstOrDefault();

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ItemBarcodeModel model)
        {
            //if (ModelState.IsValid)
            //{
                // Lookup the Item Name from the ItemId
                var item = _context.Items.FirstOrDefault(i => i.Id == model.ItemId);
                if (item == null)
                {
                    ModelState.AddModelError("ItemId", "Invalid item selected.");
                }
                else
                {
                    model.ItemName = item.ItemName;

                    _context.ItemBarcodes.Add(model);
                    _context.SaveChanges();
                    return RedirectToAction("Index");
                }
            //}

            ViewData["Items"] = new SelectList(_context.Items, "Id", "ItemName", model.ItemId);
            return View(model);
        }

    }

}
