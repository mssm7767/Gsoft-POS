// Controllers/CustomerController.cs
using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using GSoftPosNew.Data;
using GSoftPosNew.Models;
using Microsoft.EntityFrameworkCore;
using GSoftPosNew.Repositories;
using System.Threading.Tasks;

namespace GSoftPosNew.Controllers
{
    public class CustomerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public CustomerController(AppDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // Helper: load grid list
        private void LoadCustomerList()
        {
            ViewBag.CustomerList = _context.Customers
                                           .OrderBy(c => c.CustomerName)
                                           .ToList();
        }

        // ---------- CREATE (GET) ----------
        [HttpGet]
        public IActionResult Create()
        {
            LoadCustomerList();
            return View(new Customer());
        }

        // ---------- CREATE (POST) ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer model, IFormFile? PictureFile)
        {
            // Save to database
            try
            {
                

                _context.Customers.Add(model);
                _context.SaveChanges();

                if (model.UserName != null && model.Password != null)
                {
                    var hashPassword = _passwordHasher.HashPassword(model.Password);

                    var user = new User
                    {
                        CustomerId = model.Id,
                        FullName = model.CustomerName,
                        Username = model.UserName,
                        PasswordHash = hashPassword,
                        Role = "Customer",
                        IsActive = false,
                        EmailConfirmed = false,
                    };

                    _context.Users.Add(user);

                    await _context.SaveChangesAsync();
                }


                var payment = new CustomerPayment
                    {
                        CustomerId = model.Id,
                        ReceivedBy = "System", // or current user
                        Amount = 0, // no cash received
                        Advance = 0,
                        PaymentMethod = "Opening Balance",
                        Narration = "Opening balance credited to account",
                        PaymentDate = DateTime.Now,
                        Remaining = model.OpeningBalance ?? 0 // starting credit balance
                    };

                    _context.CustomerPayments.Add(payment);
                    _context.SaveChanges();
               

                TempData["Success"] = $"Customer \"{model.CustomerName}\" saved successfully!";
                TempData["LastCustomerName"] = model.CustomerName; // for highlight in grid

                return RedirectToAction(nameof(Create));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Save failed: " + (ex.InnerException?.Message ?? ex.Message);
                LoadCustomerList();
                return View(model);
            }

        }


        // ---------- EDIT (GET) ----------
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var entity = _context.Customers.FirstOrDefault(x => x.Id == id);
            if (entity == null)
            {
                TempData["Error"] = "Customer not found.";
                return RedirectToAction(nameof(Create));
            }

            LoadCustomerList();
            // same Create view reuse karein (isEdit true ho jayega kyunki Id > 0)
            return View("Create", entity);
        }

        // ---------- EDIT (POST) ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Customer model, IFormFile? PictureFile)
        {
            if (id != model.Id)
            {
                TempData["Error"] = "Invalid request.";
                return RedirectToAction(nameof(Create));
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fix the highlighted errors and try again.";
                LoadCustomerList();
                return View("Create", model);
            }

            var entity = _context.Customers.FirstOrDefault(x => x.Id == id);
            if (entity == null)
            {
                TempData["Error"] = "Customer not found.";
                return RedirectToAction(nameof(Create));
            }

            // Optional image upload (replace)
            if (PictureFile != null && PictureFile.Length > 0)
            {
                try
                {
                    var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "customers");
                    Directory.CreateDirectory(uploads);

                    var safeName = Path.GetFileName(PictureFile.FileName);
                    var unique = $"{Guid.NewGuid()}_{safeName}";
                    var fullPath = Path.Combine(uploads, unique);

                    using var fs = new FileStream(fullPath, FileMode.Create);
                    PictureFile.CopyTo(fs);

                    // (Optional) old file delete karna ho to path resolve karke check kar lo
                    // if (!string.IsNullOrWhiteSpace(entity.PictureUrl)) { ... File.Delete ... }

                    entity.PictureUrl = "/uploads/customers/" + unique;
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Image upload failed: " + ex.Message;
                    LoadCustomerList();
                    return View("Create", model);
                }
            }

            try
            {
                // Update fields
                entity.CustomerName = model.CustomerName;
                entity.FatherName = model.FatherName;
                entity.NIC = model.NIC;
                entity.ContactNumber = model.ContactNumber;
                entity.Address1 = model.Address1;
                entity.Address2 = model.Address2;
                entity.Email = model.Email;
                entity.Website = model.Website;
                entity.OpeningBalance = model.OpeningBalance;
                // entity.PictureUrl already set if new image posted

                _context.SaveChanges();

                TempData["Success"] = $"Customer \"{entity.CustomerName}\" updated successfully!";
                TempData["LastCustomerName"] = entity.CustomerName;
                return RedirectToAction(nameof(Create));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Update failed: " + (ex.InnerException?.Message ?? ex.Message);
                LoadCustomerList();
                return View("Create", model);
            }
        }

        // ---------- DELETE (POST) ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var entity = _context.Customers.FirstOrDefault(x => x.Id == id);
            if (entity == null)
            {
                TempData["Error"] = "Customer not found.";
                return RedirectToAction(nameof(Create));
            }

            try
            {
                _context.Customers.Remove(entity);
                _context.SaveChanges();

                TempData["DeleteSuccess"] = $"Customer \"{entity.CustomerName}\" deleted.";
                return RedirectToAction(nameof(Create));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Delete failed: " + (ex.InnerException?.Message ?? ex.Message);
                return RedirectToAction(nameof(Create));
            }
        }

        public async Task<IActionResult> CustomerProduct(int id)
        {
            var vanItems = await _context.CustomerVanStocks
                .Where(v => v.CustomerId == id)
                .GroupBy(v => v.ItemId)
                .Select(g => new
                {
                    ItemId = g.Key,
                    Quantity = g.Sum(x => x.Quantity),           // Total stock quantity
                    LastPurchaseDate = g.Max(x => x.PurchasedDate) // Last purchased date
                })
                .Join(
                    _context.Items,
                    v => v.ItemId,
                    i => i.Id,
                    (v, i) => new
                    {
                        ItemId = i.Id,
                        ItemCode = i.ItemCode,
                        ImagePath = i.ImagePath,
                        ItemName = i.ItemName,
                        Quantity = v.Quantity,
                        Price = i.UnitPrice,
                        PurchaseDate = v.LastPurchaseDate,
                        CustomerId = id,
                    }
                )
                .ToListAsync();



            ViewBag.CustomerItems = vanItems;


            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SellProductsBatch([FromBody] List<SaleItemDto> items)
        {
            if (items == null || items.Count == 0)
                return BadRequest("No items received.");

            var vanStockSale = new List<VanStockSale>();

            foreach (var saleItem in items)
            {
                if (saleItem.ItemId <= 0 || saleItem.Quantity <= 0)
                    continue;

                var vanStock = new VanStockSale
                {
                    ItemId = saleItem.ItemId,
                    CustomerId = saleItem.CustomerId,
                    Quantity = saleItem.Quantity,
                    ItemCode = saleItem.ItemCode,
                    ShopName = saleItem.ShopName,
                    Description = saleItem.Description,
                };

                vanStockSale.Add( vanStock );

                // Get all van stocks for this item
                var vanStocks = await _context.CustomerVanStocks
                    .Where(v => v.ItemId == saleItem.ItemId)
                    .ToListAsync();

                if (!vanStocks.Any())
                    continue; // no stock found for this item

                int qtyToReduce = saleItem.Quantity;

                foreach (var stock in vanStocks)
                {
                    if (qtyToReduce <= 0)
                        break;

                    // If this stock has enough quantity
                    if (stock.Quantity >= qtyToReduce)
                    {
                        stock.Quantity -= qtyToReduce;
                        qtyToReduce = 0;
                    }
                    else
                    {
                        // Reduce entire stock and continue
                        qtyToReduce -= stock.Quantity;
                        stock.Quantity = 0;
                    }

                    _context.CustomerVanStocks.Update(stock);
                }
            }

            await _context.SaveChangesAsync();

            await _context.AddRangeAsync(vanStockSale);

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

    }

    // DTO to receive scanned sale items
    public class SaleItemDto
    {
        public int ItemId { get; set; }
        public int CustomerId { get; set; }
        public string ItemCode { get; set; }
        public int Quantity { get; set; }
        public string ShopName { get; set; }
        public string Description { get; set; }
    }
}

