using GSoftPosNew.Data;
using GSoftPosNew.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GSoftPosNew.Controllers
{
    [Authorize]
    public class SupplierController : Controller
    {
        private readonly AppDbContext _context;
        public SupplierController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Supplier
        public IActionResult Index()
        {
            var list = _context.Suppliers
                        .OrderBy(s => s.SupplierName)
                        .ToList();
            return View(list);
        }

        // GET: /Supplier/Create
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.SupplierList = _context.Suppliers.ToList();
            PopulateCountries();
            return View();
        }

        // POST: /Supplier/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Supplier model, IFormFile LogoFile)
        {

            if (LogoFile != null && LogoFile.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/suppliers");
                Directory.CreateDirectory(uploads);
                var fileName = Path.GetFileName(LogoFile.FileName);
                var filePath = Path.Combine(uploads, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    LogoFile.CopyTo(stream);

                model.LogoUrl = "/uploads/suppliers/" + fileName;
                model.LogoPath = "/uploads/suppliers/" + fileName;
            }
            _context.Suppliers.Add(model);
            _context.SaveChanges();
            TempData["Success"] = "Supplier added successfully!";
            return RedirectToAction(nameof(Create));
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            ViewBag.SupplierList = _context.Suppliers.ToList();
            PopulateCountries();

            var item = _context.Suppliers.Find(id);

            return View(item);
        }

        // POST: /Supplier/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Supplier model, IFormFile LogoFile)
        {
            var supplier = _context.Suppliers.Find(model.Id);

            if (supplier == null)
            {
                TempData["Error"] = "Supplier not found!";
                return RedirectToAction(nameof(Create));
            }

            // ✅ Handle Logo upload
            if (LogoFile != null && LogoFile.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/suppliers");
                Directory.CreateDirectory(uploads);

                var fileName = Path.GetFileName(LogoFile.FileName);
                var filePath = Path.Combine(uploads, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    LogoFile.CopyTo(stream);

                supplier.LogoUrl = "/uploads/suppliers/" + fileName;
                supplier.LogoPath = "/uploads/suppliers/" + fileName;
            }

            // ✅ Map all fields
            supplier.SupplierName = model.SupplierName;
            supplier.OpeningBalance = model.OpeningBalance;
            supplier.Company = model.Company;
            supplier.SupplierType = model.SupplierType;
            supplier.ContactPerson = model.ContactPerson;
            supplier.Email = model.Email;
            supplier.Website = model.Website;
            supplier.TaxNumber = model.TaxNumber;
            supplier.MobileNumber = model.MobileNumber;
            supplier.PhoneNumber = model.PhoneNumber;
            supplier.BillingAddress = model.BillingAddress;
            supplier.BillingCountry = model.BillingCountry;
            supplier.BillingCity = model.BillingCity;
            supplier.ShippingAddress = model.ShippingAddress;
            supplier.ShippingCountry = model.ShippingCountry;
            supplier.ShippingCity = model.ShippingCity;
            supplier.Description = model.Description;

            _context.Suppliers.Update(supplier);
            _context.SaveChanges();

            TempData["Success"] = "Supplier updated successfully!";
            return RedirectToAction(nameof(Create));
        }



        private void PopulateCountries()
        {
            // مثال: ڈیٹا بیس یا ہارڈ کوڈڈ کنٹریز
            var countries = new List<SelectListItem>
            {
                new SelectListItem("Select Country",""),
                new SelectListItem("Pakistan","PK"),
                new SelectListItem("United States","US"),
                new SelectListItem("United Kingdom","UK"),
                // … مزید کنٹریز …
            };
            ViewBag.CountryList = countries;
        }

        public async Task<IActionResult> Delete(int id)
        {
            var modelTodelete = await _context.Suppliers.FindAsync(id);
            if (modelTodelete != null)
            {
                var purchase = await _context.Purchases.Where(p => p.SupplierId == modelTodelete.Id).ToListAsync();
                if(purchase != null)
                {
                    var purchaseItems = new List<PurchaseItem>();
                    foreach(var p in purchase)
                    {
                        var purchaseItem = await _context.PurchaseItems.Where(pi => pi.PurchaseId == p.Id).FirstOrDefaultAsync();
                        purchaseItems.Add(purchaseItem);
                    }
                    if(purchaseItems != null)
                    {
                        _context.PurchaseItems.RemoveRange(purchaseItems);
                        _context.SaveChanges();
                    }

                    _context.Purchases.RemoveRange(purchase);
                    _context.SaveChanges();
                }
                _context.Suppliers.Remove(modelTodelete);
                _context.SaveChanges();

                TempData["Danger"] = "Supplier deleted!";
                return RedirectToAction(nameof(Create));
            }

            return RedirectToAction(nameof(Create));
        }
    }
}
