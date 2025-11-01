using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using GSoftPosNew.Data;
using GSoftPosNew.Models;
using Microsoft.EntityFrameworkCore;

namespace GSoftPosNew.Controllers
{
    public class PurchaseController : Controller
    {
        private readonly AppDbContext _context;

        public PurchaseController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Purchase/Index
        [HttpGet]
        public IActionResult Index()
        {
            var purchases = _context.Purchases.Include(p => p.Supplier)
                .OrderByDescending(p => p.Date)
                .ToList();

            return View(purchases);
        }

        // GET: /Purchase/Create
        [HttpGet]
        public IActionResult Create()
        {
            SetViewBags();

            return View(new Purchase());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Purchase model, IFormFile DocumentFile)
        {
            // Supplier & Country dropdowns
            ViewBag.SupplierList = GetSupplierList();
            ViewBag.CountryList = GetCountryList();
            ViewBag.InvoiceNo = "";
            ViewBag.PreviousBalance = "0.00";

            // Ensure GenericName matches ItemName
            if (model.Item != null)
                model.Item.GenericName = model.Item.ItemName;

            // Handle file upload
            if (DocumentFile != null && DocumentFile.Length > 0)
            {
                var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/purchase");
                Directory.CreateDirectory(uploads);
                var fileName = $"{Guid.NewGuid()}_{DocumentFile.FileName}";
                var filePath = Path.Combine(uploads, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                DocumentFile.CopyTo(stream);
                model.DocumentPath = $"/uploads/purchase/{fileName}";
            }

            // Handle items from JSON
            if (!string.IsNullOrWhiteSpace(model.ItemsJson))
            {
                model.Items = JsonSerializer.Deserialize<List<PurchaseItem>>(model.ItemsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (model.Items != null && model.Items.Any())
                {
                    foreach (var item in model.Items)
                    {
                        // Reset PK
                        item.Id = 0;

                        var existingProduct = _context.Items.FirstOrDefault(p => p.ItemCode == item.ItemCode);

                        if (existingProduct == null)
                        {
                            // New product case
                            var newProduct = new Item
                            {
                                ItemName = item.ItemName,
                                GenericName = item.ItemName,
                                ItemCode = item.ItemCode,
                            
                                StockQuantity = item.Quantity
                            };

                            _context.Products.Add(newProduct);
                            _context.SaveChanges(); // to get Id

                            item.ItemId = newProduct.Id;
                            item.UnitCost = newProduct.CostPrice;
                        }
                        else
                        {
                            if(model.PurchaseType == "Return")
                            {
                                // Existing product case → update stock & price
                                existingProduct.PurchasePrice = item.UnitCost;   // update cost price if needed
                                var q = existingProduct.Quantity - item.Quantity; // ✅ add to existing stock

                                existingProduct.Quantity = q;

                                _context.Items.Update(existingProduct);

                                item.ItemId = existingProduct.Id;
                                item.UnitCost = existingProduct.SalePrice;
                            }
                            else
                            {
                                // Existing product case → update stock & price
                                existingProduct.SalePrice = item.UnitCost;
                                existingProduct.MarkupPercentage = item.DiscountPercent;

                                // Calculate percentage deduction
                                existingProduct.PurchasePrice = item.UnitCost - (item.UnitCost * (item.DiscountPercent / 100m));

                                var q = existingProduct.Quantity + item.Quantity; // ✅ add to existing stock

                                if (!string.IsNullOrEmpty(existingProduct.PackSize))
                                {
                                    int packSize;
                                    if (int.TryParse(existingProduct.PackSize, out packSize))
                                    {
                                        existingProduct.Quantity = existingProduct.Quantity + (item.Quantity * packSize);
                                    }
                                    else
                                    {
                                        // fallback if PackSize is not a valid number
                                        existingProduct.Quantity = q;
                                    }
                                }
                                else
                                {
                                    existingProduct.Quantity = q;
                                }

                                _context.Items.Update(existingProduct);

                                item.ItemId = existingProduct.Id;
                                item.UnitCost = existingProduct.SalePrice;
                                item.LineTotal = existingProduct.PurchasePrice * item.Quantity;
                            }
                                
                        }
                    }


                }
            }
           

            // Save purchase with items
            _context.Purchases.Add(model);
            AddSupplierPayment(model);
            _context.SaveChanges();

            TempData["Message"] = "Purchase saved successfully!";
            return RedirectToAction(nameof(Create));
        }


        private void AddSupplierPayment(Purchase purchase)
        {
            if (purchase == null) return;

            var previousPayment = _context.SupplierPayments.Where(p => p.SupplierId == purchase.SupplierId).OrderByDescending(p => p.Id).Select(p => p.Remaining).FirstOrDefault();

            if(purchase.PurchaseType == "Return")
            {
                var remaining =  (previousPayment ?? 0) - purchase.Remaining;
                var advance = 0m;

                // If paid more than total, shift extra to Advance
                //if (remaining < 0)
                //{
                //    advance = Math.Abs(remaining); // extra paid
                //    remaining = 0;
                //}

                var payment = new SupplierPayment
                {
                    SupplierId = purchase.SupplierId,
                    Amount = purchase.Paid,    // ✅ use actual paid amount, not total
                    Advance = advance,               // ✅ store extra paid
                    PaymentMethod = "Cash",          // TODO: pass from UI
                    Narration = $"Purchase Invoice #{purchase.ReferenceNo}",
                    PaymentDate = DateTime.Now,
                    Remaining = remaining,
                    ReceivedBy = User.Identity?.Name ?? "System"
                };

                _context.SupplierPayments.Add(payment);

                // ✅ update supplier balance
                var supplier = _context.Suppliers.FirstOrDefault(s => s.Id == purchase.SupplierId);
                if (supplier != null)
                {
                    supplier.OpeningBalance = remaining; // or adjust depending on your business logic
                    _context.Suppliers.Update(supplier);
                }

                _context.SaveChanges();
            }
            else
            {
                var remaining = purchase.Remaining + (previousPayment ?? 0);
                var advance = 0m;

                // If paid more than total, shift extra to Advance
                //if (remaining < 0)
                //{
                //    advance = Math.Abs(remaining); // extra paid
                //    remaining = 0;
                //}

                var payment = new SupplierPayment
                {
                    SupplierId = purchase.SupplierId,
                    Amount = purchase.Paid,    // ✅ use actual paid amount, not total
                    Advance = advance,               // ✅ store extra paid
                    PaymentMethod = "Cash",          // TODO: pass from UI
                    Narration = $"Purchase Invoice #{purchase.ReferenceNo}",
                    PaymentDate = DateTime.Now,
                    Remaining = remaining,
                    ReceivedBy = User.Identity?.Name ?? "System"
                };

                _context.SupplierPayments.Add(payment);

                // ✅ update supplier balance
                var supplier = _context.Suppliers.FirstOrDefault(s => s.Id == purchase.SupplierId);
                if (supplier != null)
                {
                    supplier.OpeningBalance = remaining; // or adjust depending on your business logic
                    _context.Suppliers.Update(supplier);
                }

                _context.SaveChanges();
            }

            
        }


        private string GenerateReferenceNo()
        {
            // Example: "REF-20250815-001"
            return "REF-" + DateTime.Now.ToString("yyyyMMdd") + "-" + new Random().Next(100, 999);
        }


        

        public async Task<IActionResult> Details(int id)
        {
            var purchase = await _context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.Items) // if you have a navigation property
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchase != null)
            {
                foreach (var pi in purchase.Items)
                {
                    pi.Item = await _context.Items.FindAsync(pi.ItemId);
                }
            }

            if (purchase == null)
            {
                return NotFound();
            }

            return View(purchase); // ✅ Pass a single Purchase
        }


        // --------------------------
        // 🔧 Private Utility Methods
        // --------------------------

        private void SetViewBags()
        {
            ViewBag.SupplierList = GetSupplierList();
            ViewBag.CountryList = GetCountryList();

            var items = _context.Items
                .Select(i => new
                {
                    i.Id,
                    i.ItemCode,
                    i.ItemName,
                    i.PurchasePrice,
                    i.Quantity,
                    i.SalePrice,
                    i.MarkupPercentage,
                    i.PackSize

                }).ToList();

            ViewBag.Items = items;
            ViewBag.ItemsJson = JsonSerializer.Serialize(items);

            ViewBag.InvoiceNo = "";           // Placeholder: replace with actual invoice number logic
            ViewBag.PreviousBalance = "0.00"; // Placeholder: replace with actual balance logic
        }

        private SelectList GetSupplierList()
        {
            return new SelectList(
                _context.Suppliers.OrderBy(s => s.SupplierName).ToList(),
                "Id",
                "SupplierName"
            );
        }

        private List<SelectListItem> GetCountryList()
        {
            return new List<SelectListItem>
            {
                new("Select Country", ""),
                new("Pakistan", "PK"),
                new("United States", "US"),
                new("United Kingdom", "UK")
            };
        }

        private string SaveUploadedFile(IFormFile file)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/purchase");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var fullPath = Path.Combine(uploadsFolder, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            file.CopyTo(stream);

            return $"/uploads/purchase/{fileName}";
        }
    }
}
