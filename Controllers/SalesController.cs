using GSoftPosNew.Data;
using GSoftPosNew.Models;
using GSoftPosNew.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Drawing.Printing;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace GSoftPosNew.Controllers
{
    public class SalesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ICompositeViewEngine _viewEngine;

        public SalesController(AppDbContext context, ICompositeViewEngine viewEngine)
        {
            _context = context;
            _viewEngine = viewEngine;
        }
        public IActionResult Index(DateTime? fromDate, DateTime? toDate, string search)
        {
            var query = _context.Sales.AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(s => s.SaleDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(s => s.SaleDate <= toDate.Value);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(s => s.InvoiceNumber.Contains(search) || s.CashierId.Contains(search));

            var model = new SalesFilterViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                Search = search,
                Sales = query.OrderByDescending(s => s.SaleDate).ToList()
            };

            return View(model);
        }

        public IActionResult POS()
        {
            var items = _context.Items.ToList();
            var categories = items.Select(i => i.GenericName).Distinct().ToList();
            ViewBag.CustomerList = _context.Customers.ToList();
            ViewBag.CustomerPayments = _context.CustomerPayments.ToList();

            ViewBag.Categories = categories;
            ViewBag.CashierId = User.Identity?.Name ?? "Unknown";

            var saleInvNo = _context.Sales.OrderByDescending(s => s.Id).Select(s => s.InvoiceNumber).FirstOrDefault();

            ViewBag.InvoiceLastDigit = !string.IsNullOrEmpty(saleInvNo)
                                            ? int.Parse(saleInvNo[saleInvNo.Length - 1].ToString())
                                            : 0;


            ViewBag.ShopName = _context.ShopSettings.OrderByDescending(s => s.Id).Select(s => s.ShopName).FirstOrDefault();

            return View(items);
        }

        public async Task <IActionResult> POSTouch()
        {
            var items = _context.Items.ToList();

            ViewBag.Customer = await _context.Customers.ToListAsync();
            var categoriesForJs = _context.Categories
                        .Select(c => new
                        {
                            c.Id,
                            c.Name,
                            c.ImagePath,
                            c.Description,
                            c.Discount,
                            Items = c.Items.Select(i => new
                            {
                                i.Id,
                                i.ItemCode,
                                i.ReferenceCode,
                                i.ItemName,
                                i.Flavour,
                                i.GenericName,
                                i.SupplierId,
                                SupplierName = i.Supplier != null ? i.Supplier.SupplierName : "",
                                i.Unit,
                                i.UnitPrice,
                                i.LocationId,
                                i.SalePrice,
                                i.PurchasePrice,
                                i.PackPrice,
                                i.PackSize,
                                i.Quantity,
                                i.MarkupPercentage,
                                i.CostPrice,
                                i.TaxRate,
                                i.StockQuantity,
                                i.LowStockThreshold,
                                i.ExpiryDate,
                                i.ImagePath
                            }).ToList()
                        })
                        .OrderBy(c => c.Name)
                        .ToList();

            ViewBag.CategoriesForJs = categoriesForJs;

            var saleInvNo = _context.Sales.OrderByDescending(s => s.Id).Select(s => s.InvoiceNumber).FirstOrDefault();
            ViewBag.InvoiceLastDigit = !string.IsNullOrEmpty(saleInvNo)
                                            ? int.Parse(saleInvNo[saleInvNo.Length - 1].ToString())
                                            : 0;
            ViewBag.ShopName = _context.ShopSettings.OrderByDescending(s => s.Id).Select(s => s.ShopName).FirstOrDefault();

            return View(items);
        }
        
        public IActionResult POSTouchNew()
        {
            var items = _context.Items.ToList();
            var categories = items.Select(i => i.GenericName).Distinct().ToList();
            ViewBag.CustomerList = _context.Customers.ToList();
            ViewBag.CustomerPayments = _context.CustomerPayments.ToList();

            ViewBag.Categories = categories;
            ViewBag.CashierId = User.Identity?.Name ?? "Unknown";

            var saleInvNo = _context.Sales.OrderByDescending(s => s.Id).Select(s => s.InvoiceNumber).FirstOrDefault();

            ViewBag.InvoiceLastDigit = !string.IsNullOrEmpty(saleInvNo)
                                            ? int.Parse(saleInvNo[saleInvNo.Length - 1].ToString())
                                            : 0;


            ViewBag.ShopName = _context.ShopSettings.OrderByDescending(s => s.Id).Select(s => s.ShopName).FirstOrDefault();

            return View(items);
        }

        [HttpPost]
        public async Task<IActionResult> SaveSale([FromBody] Sale sale)
        {
            if (sale == null || sale.SaleItems == null || !sale.SaleItems.Any())
            {
                using var reader = new StreamReader(Request.Body);
                var rawBody = await reader.ReadToEndAsync();
                return BadRequest("Invalid sale data. Raw request: " + rawBody);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (string.IsNullOrEmpty(sale.InvoiceNumber))
                    sale.InvoiceNumber = GenerateInvoiceNumber();

                sale.SaleDate = DateTime.Now;
                sale.CashierId = User.Identity?.Name ?? "Unknown";
                sale.Id = 0;
                sale.CustomerId = sale.custId;

                if (sale.SaleType == "Return")
                {
                    if (sale.Payment != null)
                        sale.Payment.Id = 0;

                    foreach (var item in sale.SaleItems)
                        item.Id = 0;

                    // Save Sale
                    _context.Sales.Add(sale);
                    await _context.SaveChangesAsync();

                    foreach (var item in sale.SaleItems)
                        item.SaleId = sale.Id;

                    if (sale.Payment != null)
                        sale.Payment.SaleId = sale.Id;

                    await _context.SaveChangesAsync();
                    if (sale.Payment?.PaymentMethod?.ToLower() == "credit")
                    {
                        var customer = await _context.Customers.FindAsync(sale.custId);
                        if (customer != null)
                        {
                            decimal paidAmount = sale.tender_amount;
                            decimal totalAmount = sale.Total;
                            decimal balance = totalAmount + paidAmount; // +ve = Remaining, -ve = Advance

                            var existingPayment = await _context.CustomerPayments
                                .FirstOrDefaultAsync(p => p.CustomerId == sale.custId);

                            if (existingPayment != null)
                            {
                                existingPayment.PaymentDate = DateTime.Now;
                                existingPayment.Narration = $"Return Invoice {sale.InvoiceNumber}";

                                if (balance > 0)
                                {
                                    existingPayment.Remaining += balance;
                                    existingPayment.Sale = (existingPayment.Sale ?? 0) - totalAmount;
                                }
                                else if (balance < 0)
                                {
                                    // Customer overpaid → Advance
                                    decimal extraAdvance = Math.Abs(balance);

                                    if (existingPayment.Remaining > 0)
                                    {
                                        // Use remaining to offset advance first
                                        if (existingPayment.Remaining >= extraAdvance)
                                        {
                                            existingPayment.Remaining += extraAdvance;
                                            extraAdvance = 0; // fully covered
                                        }
                                        else
                                        {
                                            extraAdvance += existingPayment.Remaining;
                                            existingPayment.Remaining = 0;
                                        }
                                    }

                                    existingPayment.Advance += extraAdvance;
                                    existingPayment.Sale = (existingPayment.Sale ?? 0) + totalAmount;
                                }
                                else
                                {
                                    existingPayment.Sale = (existingPayment.Sale ?? 0) - totalAmount;
                                }

                                _context.CustomerPayments.Update(existingPayment);
                            }
                            else
                            {
                                // First-time payment
                                var paymentEntry = new CustomerPayment
                                {
                                    CustomerId = sale.custId,
                                    ReceivedBy = sale.CashierId,
                                    Amount = paidAmount,
                                    PaymentMethod = "Credit",
                                    Narration = $"Credit Sale Invoice {sale.InvoiceNumber}",
                                    PaymentDate = DateTime.Now,
                                    Advance = balance < 0 ? Math.Abs(balance) : 0,
                                    Remaining = balance > 0 ? balance : 0
                                };

                                _context.CustomerPayments.Add(paymentEntry);
                            }

                            // Update customer's balance → store only Remaining (what customer owes)
                            customer.OpeningBalance = (existingPayment?.Remaining ?? (balance > 0 ? balance : 0));

                            await _context.SaveChangesAsync();
                        }
                    }

                    foreach (var item in sale.SaleItems)
                    {
                        var dbItem = await _context.Items.FindAsync(item.ItemId);
                        if (dbItem != null)
                        {
                            //if (dbItem.Quantity < item.Quantity)
                            //    throw new Exception($"Not enough stock for item {dbItem.ItemName}");

                            dbItem.Quantity += item.Quantity;
                            _context.Items.Update(dbItem);
                        }
                    }
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        sale.Id,
                        sale.InvoiceNumber,
                        ReceiptUrl = Url.Action("Receipt", "Sales", new { id = sale.Id }),
                        Message = "Sale saved successfully"
                    });
                }
                else
                {
                    if (sale.Payment != null)
                        sale.Payment.Id = 0;

                    foreach (var item in sale.SaleItems)
                        item.Id = 0;

                    // Save Sale
                    _context.Sales.Add(sale);
                    await _context.SaveChangesAsync();

                    foreach (var item in sale.SaleItems)
                        item.SaleId = sale.Id;

                    if (sale.Payment != null)
                        sale.Payment.SaleId = sale.Id;

                    await _context.SaveChangesAsync();

                    var vanStockList = new List<CustomerVanStock>();
                    foreach (var item in sale.SaleItems)
                    {
                        var vanStock = new CustomerVanStock
                        {
                            CustomerId = sale.CustomerId,
                            ItemId = item.ItemId,
                            Quantity = item.Quantity,
                        };

                        vanStockList.Add(vanStock);
                    }

                    await _context.AddRangeAsync(vanStockList);
                    await _context.SaveChangesAsync();

                    if (sale.Payment?.PaymentMethod?.ToLower() == "credit")
                    {
                        var customer = await _context.Customers.FindAsync(sale.custId);
                        if (customer != null)
                        {
                            decimal paidAmount = sale.tender_amount;
                            decimal totalAmount = sale.Total;
                            decimal balance = totalAmount - paidAmount; // +ve = Remaining, -ve = Advance

                            var existingPayment = await _context.CustomerPayments
                                .FirstOrDefaultAsync(p => p.CustomerId == sale.custId);

                            if (existingPayment != null)
                            {
                                existingPayment.Amount += paidAmount;
                                existingPayment.PaymentDate = DateTime.Now;
                                existingPayment.Narration = $"Credit Sale Invoice {sale.InvoiceNumber}";

                                if (balance > 0)
                                {

                                    existingPayment.Remaining -= balance;
                                    existingPayment.Sale = (existingPayment.Sale ?? 0) + totalAmount;
                                }
                                else if (balance < 0)
                                {
                                    // Customer overpaid → Advance
                                    decimal extraAdvance = Math.Abs(balance);

                                    if (existingPayment.Remaining > 0)
                                    {
                                        // Use remaining to offset advance first
                                        if (existingPayment.Remaining >= extraAdvance)
                                        {
                                            existingPayment.Remaining += extraAdvance;
                                            extraAdvance = 0; // fully covered
                                        }
                                        else
                                        {
                                            extraAdvance += existingPayment.Remaining;
                                            existingPayment.Remaining = 0;
                                        }
                                    }

                                    existingPayment.Advance += extraAdvance;
                                    existingPayment.Sale = (existingPayment.Sale ?? 0) + totalAmount;
                                }
                                else
                                {
                                    existingPayment.Sale = (existingPayment.Sale ?? 0) + totalAmount;
                                }

                                _context.CustomerPayments.Update(existingPayment);
                            }
                            else
                            {
                                // First-time payment
                                var paymentEntry = new CustomerPayment
                                {
                                    CustomerId = sale.custId,
                                    ReceivedBy = sale.CashierId,
                                    Amount = paidAmount,
                                    PaymentMethod = "Credit",
                                    Narration = $"Credit Sale Invoice {sale.InvoiceNumber}",
                                    PaymentDate = DateTime.Now,
                                    Advance = balance < 0 ? Math.Abs(balance) : 0,
                                    Remaining = balance > 0 ? balance : 0
                                };

                                _context.CustomerPayments.Add(paymentEntry);
                            }

                            // Update customer's balance → store only Remaining (what customer owes)
                            customer.OpeningBalance = (existingPayment?.Remaining ?? (balance > 0 ? balance : 0));

                            await _context.SaveChangesAsync();
                        }
                    }

                    foreach (var item in sale.SaleItems)
                    {
                        var dbItem = await _context.Items.FindAsync(item.ItemId);
                        if (dbItem != null)
                        {
                            //if (dbItem.Quantity < item.Quantity)
                            //    throw new Exception($"Not enough stock for item {dbItem.ItemName}");

                            dbItem.Quantity -= item.Quantity;
                            _context.Items.Update(dbItem);
                        }
                    }
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        sale.Id,
                        sale.InvoiceNumber,
                        ReceiptUrl = Url.Action("Receipt", "Sales", new { id = sale.Id }),
                        Message = "Sale saved successfully"
                    });
                }

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, "Error saving sale: " + errorMessage);
            }
        }
        
        
        [HttpPost]
        public async Task<IActionResult> KOTSale([FromBody] Sale sale)
        {
            if (sale == null || sale.SaleItems == null || !sale.SaleItems.Any())
            {
                using var reader = new StreamReader(Request.Body);
                var rawBody = await reader.ReadToEndAsync();
                return BadRequest("Invalid sale data. Raw request: " + rawBody);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (string.IsNullOrEmpty(sale.InvoiceNumber))
                    sale.InvoiceNumber = GenerateInvoiceNumber();

                sale.SaleDate = DateTime.Now;
                sale.CashierId = User.Identity?.Name ?? "Unknown";
                sale.Id = 0;
                sale.CustomerId = sale.custId;

                if (sale.SaleType == "Return")
                {
                    if (sale.Payment != null)
                        sale.Payment.Id = 0;

                    foreach (var item in sale.SaleItems)
                        item.Id = 0;

                    // Save Sale
                    _context.Sales.Add(sale);
                    await _context.SaveChangesAsync();

                    foreach (var item in sale.SaleItems)
                        item.SaleId = sale.Id;

                    if (sale.Payment != null)
                        sale.Payment.SaleId = sale.Id;

                    await _context.SaveChangesAsync();
                    if (sale.Payment?.PaymentMethod?.ToLower() == "credit")
                    {
                        var customer = await _context.Customers.FindAsync(sale.custId);
                        if (customer != null)
                        {
                            decimal paidAmount = sale.tender_amount;
                            decimal totalAmount = sale.Total;
                            decimal balance = totalAmount + paidAmount; // +ve = Remaining, -ve = Advance

                            var existingPayment = await _context.CustomerPayments
                                .FirstOrDefaultAsync(p => p.CustomerId == sale.custId);

                            if (existingPayment != null)
                            {
                                existingPayment.PaymentDate = DateTime.Now;
                                existingPayment.Narration = $"Return Invoice {sale.InvoiceNumber}";

                                if (balance > 0)
                                {
                                    existingPayment.Remaining += balance;
                                    existingPayment.Sale = (existingPayment.Sale ?? 0) - totalAmount;
                                }
                                else if (balance < 0)
                                {
                                    // Customer overpaid → Advance
                                    decimal extraAdvance = Math.Abs(balance);

                                    if (existingPayment.Remaining > 0)
                                    {
                                        // Use remaining to offset advance first
                                        if (existingPayment.Remaining >= extraAdvance)
                                        {
                                            existingPayment.Remaining += extraAdvance;
                                            extraAdvance = 0; // fully covered
                                        }
                                        else
                                        {
                                            extraAdvance += existingPayment.Remaining;
                                            existingPayment.Remaining = 0;
                                        }
                                    }

                                    existingPayment.Advance += extraAdvance;
                                    existingPayment.Sale = (existingPayment.Sale ?? 0) + totalAmount;
                                }
                                else
                                {
                                    existingPayment.Sale = (existingPayment.Sale ?? 0) - totalAmount;
                                }

                                _context.CustomerPayments.Update(existingPayment);
                            }
                            else
                            {
                                // First-time payment
                                var paymentEntry = new CustomerPayment
                                {
                                    CustomerId = sale.custId,
                                    ReceivedBy = sale.CashierId,
                                    Amount = paidAmount,
                                    PaymentMethod = "Credit",
                                    Narration = $"Credit Sale Invoice {sale.InvoiceNumber}",
                                    PaymentDate = DateTime.Now,
                                    Advance = balance < 0 ? Math.Abs(balance) : 0,
                                    Remaining = balance > 0 ? balance : 0
                                };

                                _context.CustomerPayments.Add(paymentEntry);
                            }

                            // Update customer's balance → store only Remaining (what customer owes)
                            customer.OpeningBalance = (existingPayment?.Remaining ?? (balance > 0 ? balance : 0));

                            await _context.SaveChangesAsync();
                        }
                    }

                    foreach (var item in sale.SaleItems)
                    {
                        var dbItem = await _context.Items.FindAsync(item.ItemId);
                        if (dbItem != null)
                        {
                            //if (dbItem.Quantity < item.Quantity)
                            //    throw new Exception($"Not enough stock for item {dbItem.ItemName}");

                            dbItem.Quantity += item.Quantity;
                            _context.Items.Update(dbItem);
                        }
                    }
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        sale.Id,
                        sale.InvoiceNumber,
                        ReceiptUrl = Url.Action("KOTReceipt", "Sales", new { id = sale.Id }),
                        Message = "Sale saved successfully"
                    });
                }
                else
                {
                    if (sale.Payment != null)
                        sale.Payment.Id = 0;

                    foreach (var item in sale.SaleItems)
                        item.Id = 0;

                    // Save Sale
                    _context.Sales.Add(sale);
                    await _context.SaveChangesAsync();

                    foreach (var item in sale.SaleItems)
                        item.SaleId = sale.Id;

                    if (sale.Payment != null)
                        sale.Payment.SaleId = sale.Id;

                    await _context.SaveChangesAsync();
                    if (sale.Payment?.PaymentMethod?.ToLower() == "credit")
                    {
                        var customer = await _context.Customers.FindAsync(sale.custId);
                        if (customer != null)
                        {
                            decimal paidAmount = sale.tender_amount;
                            decimal totalAmount = sale.Total;
                            decimal balance = totalAmount - paidAmount; // +ve = Remaining, -ve = Advance

                            var existingPayment = await _context.CustomerPayments
                                .FirstOrDefaultAsync(p => p.CustomerId == sale.custId);

                            if (existingPayment != null)
                            {
                                existingPayment.Amount += paidAmount;
                                existingPayment.PaymentDate = DateTime.Now;
                                existingPayment.Narration = $"Credit Sale Invoice {sale.InvoiceNumber}";

                                if (balance > 0)
                                {

                                    existingPayment.Remaining -= balance;
                                    existingPayment.Sale = (existingPayment.Sale ?? 0) + totalAmount;
                                }
                                else if (balance < 0)
                                {
                                    // Customer overpaid → Advance
                                    decimal extraAdvance = Math.Abs(balance);

                                    if (existingPayment.Remaining > 0)
                                    {
                                        // Use remaining to offset advance first
                                        if (existingPayment.Remaining >= extraAdvance)
                                        {
                                            existingPayment.Remaining += extraAdvance;
                                            extraAdvance = 0; // fully covered
                                        }
                                        else
                                        {
                                            extraAdvance += existingPayment.Remaining;
                                            existingPayment.Remaining = 0;
                                        }
                                    }

                                    existingPayment.Advance += extraAdvance;
                                    existingPayment.Sale = (existingPayment.Sale ?? 0) + totalAmount;
                                }
                                else
                                {
                                    existingPayment.Sale = (existingPayment.Sale ?? 0) + totalAmount;
                                }

                                _context.CustomerPayments.Update(existingPayment);
                            }
                            else
                            {
                                // First-time payment
                                var paymentEntry = new CustomerPayment
                                {
                                    CustomerId = sale.custId,
                                    ReceivedBy = sale.CashierId,
                                    Amount = paidAmount,
                                    PaymentMethod = "Credit",
                                    Narration = $"Credit Sale Invoice {sale.InvoiceNumber}",
                                    PaymentDate = DateTime.Now,
                                    Advance = balance < 0 ? Math.Abs(balance) : 0,
                                    Remaining = balance > 0 ? balance : 0
                                };

                                _context.CustomerPayments.Add(paymentEntry);
                            }

                            // Update customer's balance → store only Remaining (what customer owes)
                            customer.OpeningBalance = (existingPayment?.Remaining ?? (balance > 0 ? balance : 0));

                            await _context.SaveChangesAsync();
                        }
                    }

                    foreach (var item in sale.SaleItems)
                    {
                        var dbItem = await _context.Items.FindAsync(item.ItemId);
                        if (dbItem != null)
                        {
                            //if (dbItem.Quantity < item.Quantity)
                            //    throw new Exception($"Not enough stock for item {dbItem.ItemName}");

                            dbItem.Quantity -= item.Quantity;
                            _context.Items.Update(dbItem);
                        }
                    }
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        sale.Id,
                        sale.InvoiceNumber,
                        ReceiptUrl = Url.Action("KOTReceipt", "Sales", new { id = sale.Id }),
                        Message = "Sale saved successfully"
                    });
                }

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, "Error saving sale: " + errorMessage);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOnly([FromBody] Sale sale)
        {
            if (sale == null || sale.SaleItems == null || !sale.SaleItems.Any())
            {
                using var reader = new StreamReader(Request.Body);
                var rawBody = await reader.ReadToEndAsync();
                return BadRequest("Invalid sale data. Raw request: " + rawBody);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (string.IsNullOrEmpty(sale.InvoiceNumber))
                    sale.InvoiceNumber = GenerateInvoiceNumber();

                sale.SaleDate = DateTime.Now;
                sale.CashierId = User.Identity?.Name ?? "Unknown";
                sale.Id = 0;
                sale.CustomerId = sale.custId;

                if (sale.SaleType == "Return")
                {
                    if (sale.Payment != null)
                        sale.Payment.Id = 0;

                    foreach (var item in sale.SaleItems)
                        item.Id = 0;

                    // Save Sale
                    _context.Sales.Add(sale);
                    await _context.SaveChangesAsync();

                    foreach (var item in sale.SaleItems)
                        item.SaleId = sale.Id;

                    if (sale.Payment != null)
                        sale.Payment.SaleId = sale.Id;

                    await _context.SaveChangesAsync();

                    if (sale.Payment?.PaymentMethod?.ToLower() == "credit")
                    {
                        var customer = await _context.Customers.FindAsync(sale.custId);
                        if (customer != null)
                        {
                            decimal paidAmount = sale.tender_amount;
                            decimal totalAmount = sale.Total;
                            decimal balance = totalAmount + paidAmount; // +ve = Remaining, -ve = Advance

                            var existingPayment = await _context.CustomerPayments
                                .FirstOrDefaultAsync(p => p.CustomerId == sale.custId);

                            if (existingPayment != null)
                            {
                                existingPayment.PaymentDate = DateTime.Now;
                                existingPayment.Narration = $"Return Invoice {sale.InvoiceNumber}";

                                if (balance > 0)
                                {
                                    existingPayment.Remaining += balance;
                                    existingPayment.Sale = (existingPayment.Sale ?? 0) - totalAmount;
                                }
                                else if (balance < 0)
                                {
                                    // Customer overpaid → Advance
                                    decimal extraAdvance = Math.Abs(balance);

                                    if (existingPayment.Remaining > 0)
                                    {
                                        // Use remaining to offset advance first
                                        if (existingPayment.Remaining >= extraAdvance)
                                        {
                                            existingPayment.Remaining += extraAdvance;
                                            extraAdvance = 0; // fully covered
                                        }
                                        else
                                        {
                                            extraAdvance += existingPayment.Remaining;
                                            existingPayment.Remaining = 0;
                                        }
                                    }

                                    existingPayment.Advance += extraAdvance;
                                    existingPayment.Sale = (existingPayment.Sale ?? 0) + totalAmount;
                                }
                                else
                                {
                                    existingPayment.Sale = (existingPayment.Sale ?? 0) - totalAmount;
                                }

                                _context.CustomerPayments.Update(existingPayment);
                            }
                            else
                            {
                                // First-time payment
                                var paymentEntry = new CustomerPayment
                                {
                                    CustomerId = sale.custId,
                                    ReceivedBy = sale.CashierId,
                                    Amount = paidAmount,
                                    PaymentMethod = "Credit",
                                    Narration = $"Credit Sale Invoice {sale.InvoiceNumber}",
                                    PaymentDate = DateTime.Now,
                                    Advance = balance < 0 ? Math.Abs(balance) : 0,
                                    Remaining = balance > 0 ? balance : 0
                                };

                                _context.CustomerPayments.Add(paymentEntry);
                            }

                            // Update customer's balance → store only Remaining (what customer owes)
                            customer.OpeningBalance = (existingPayment?.Remaining ?? (balance > 0 ? balance : 0));

                            await _context.SaveChangesAsync();
                        }
                    }

                    foreach (var item in sale.SaleItems)
                    {
                        var dbItem = await _context.Items.FindAsync(item.ItemId);
                        if (dbItem != null)
                        {
                            //if (dbItem.Quantity < item.Quantity)
                            //    throw new Exception($"Not enough stock for item {dbItem.ItemName}");

                            dbItem.Quantity += item.Quantity;
                            _context.Items.Update(dbItem);
                        }
                    }
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        sale.Id,
                        sale.InvoiceNumber,
                        ReceiptUrl = Url.Action("POS", "Sales")
                    });
                }
                else
                {
                    if (sale.Payment != null)
                        sale.Payment.Id = 0;

                    foreach (var item in sale.SaleItems)
                        item.Id = 0;

                    // Save Sale
                    _context.Sales.Add(sale);
                    await _context.SaveChangesAsync();

                    foreach (var item in sale.SaleItems)
                        item.SaleId = sale.Id;

                    if (sale.Payment != null)
                        sale.Payment.SaleId = sale.Id;

                    await _context.SaveChangesAsync();

                    var vanStockList = new List<CustomerVanStock>();
                    foreach (var item in sale.SaleItems)
                    {
                        var vanStock = new CustomerVanStock
                        {
                            CustomerId = sale.CustomerId,
                            ItemId = item.ItemId,
                            Quantity = item.Quantity,
                        };

                        vanStockList.Add(vanStock);
                    }

                    await _context.AddRangeAsync(vanStockList);
                    await _context.SaveChangesAsync();

                    if (sale.Payment?.PaymentMethod?.ToLower() == "credit")
                    {
                        var customer = await _context.Customers.FindAsync(sale.custId);
                        if (customer != null)
                        {
                            decimal paidAmount = sale.tender_amount;
                            decimal totalAmount = sale.Total;
                            decimal balance = totalAmount - paidAmount; // +ve = Remaining, -ve = Advance

                            var existingPayment = await _context.CustomerPayments
                                .FirstOrDefaultAsync(p => p.CustomerId == sale.custId);

                            if (existingPayment != null)
                            {
                                existingPayment.Amount += paidAmount;
                                existingPayment.PaymentDate = DateTime.Now;
                                existingPayment.Narration = $"Credit Sale Invoice {sale.InvoiceNumber}";

                                if (balance > 0)
                                {

                                    existingPayment.Remaining -= balance;
                                    existingPayment.Sale = (existingPayment.Sale ?? 0) + totalAmount;
                                }
                                else if (balance < 0)
                                {
                                    // Customer overpaid → Advance
                                    decimal extraAdvance = Math.Abs(balance);

                                    if (existingPayment.Remaining > 0)
                                    {
                                        // Use remaining to offset advance first
                                        if (existingPayment.Remaining >= extraAdvance)
                                        {
                                            existingPayment.Remaining += extraAdvance;
                                            extraAdvance = 0; // fully covered
                                        }
                                        else
                                        {
                                            extraAdvance += existingPayment.Remaining;
                                            existingPayment.Remaining = 0;
                                        }
                                    }

                                    existingPayment.Advance += extraAdvance;
                                    existingPayment.Sale = (existingPayment.Sale ?? 0) + totalAmount;
                                }
                                else
                                {
                                    existingPayment.Sale = (existingPayment.Sale ?? 0) + totalAmount;
                                }

                                _context.CustomerPayments.Update(existingPayment);
                            }
                            else
                            {
                                // First-time payment
                                var paymentEntry = new CustomerPayment
                                {
                                    CustomerId = sale.custId,
                                    ReceivedBy = sale.CashierId,
                                    Amount = paidAmount,
                                    PaymentMethod = "Credit",
                                    Narration = $"Credit Sale Invoice {sale.InvoiceNumber}",
                                    PaymentDate = DateTime.Now,
                                    Advance = balance < 0 ? Math.Abs(balance) : 0,
                                    Remaining = balance > 0 ? balance : 0
                                };

                                _context.CustomerPayments.Add(paymentEntry);
                            }

                            // Update customer's balance → store only Remaining (what customer owes)
                            customer.OpeningBalance = (existingPayment?.Remaining ?? (balance > 0 ? balance : 0));

                            await _context.SaveChangesAsync();
                        }
                    }

                    foreach (var item in sale.SaleItems)
                    {
                        var dbItem = await _context.Items.FindAsync(item.ItemId);
                        if (dbItem != null)
                        {
                            if (dbItem.Quantity < item.Quantity)
                                throw new Exception($"Not enough stock for item {dbItem.ItemName}");

                            dbItem.Quantity -= item.Quantity;
                            _context.Items.Update(dbItem);
                        }
                    }
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        sale.Id,
                        sale.InvoiceNumber,
                        ReceiptUrl = Url.Action("POS", "Sales")
                    });
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, "Error saving sale: " + errorMessage);
            }
        }


        public async Task<IActionResult> CustomerReport()
        {
            var customers = await _context.Customers.ToListAsync();

            var payments = await _context.CustomerPayments
                .OrderBy(p => p.PaymentDate)
                .ThenBy(p => p.Id)
                .ToListAsync();

            var dto = new List<CustomerReportDto>();

            foreach (var c in customers)
            {
                var customerPayments = payments.Where(p => p.CustomerId == c.Id);

                decimal debit = 0, credit = 0, advance = 0;

                foreach (var p in customerPayments)
                {
                    // Sales
                    debit += p.Sale ?? 0;

                    // Payments received
                    credit += p.Amount;   // ensure you have this field

                    // Advance / overpaid
                    advance += p.Advance;
                }

                decimal balance = credit + advance - debit;

                dto.Add(new CustomerReportDto
                {
                    CustomerId = c.Id,
                    CustomerName = c.CustomerName,
                    Debit = debit,
                    Credit = credit,
                    Advance = advance,
                    Balance = balance
                });
            }

            return View(dto);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> ProcessPayment(
            [FromForm] decimal SubTotal,
            [FromForm] decimal Tax,
            [FromForm] decimal Discount,
            [FromForm] decimal TotalDue,
            [FromForm] string PaymentMethod = "Cash",
            [FromForm] string ItemsJson = "")
        {
            try
            {
                var saleItems = JsonSerializer.Deserialize<List<SaleItemModel>>(ItemsJson)
                    ?? new List<SaleItemModel>();

                var sale = new Sale
                {
                    InvoiceNumber = GenerateInvoiceNumber(),
                    CashierId = User.Identity.Name ?? "N/A",
                    SubTotal = SubTotal,
                    Tax = Tax,
                    Discount = Discount,
                    Total = TotalDue,
                    Payment = new Payment
                    {
                        Amount = TotalDue,
                        PaymentMethod = PaymentMethod
                    },
                    SaleItems = saleItems.Select(item => new SaleItem
                    {
                        ItemId = item.ItemId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        DiscountPercent = item.DiscountPercent,
                        TaxAmount = item.TaxAmount,
                        LineTotal = item.LineTotal
                    }).ToList()
                };

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Receipt), new { id = sale.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while processing your payment.");
                return RedirectToAction(nameof(POS));
            }
        }

        // GET: /Sale/Edit/{id}
        public IActionResult Edit(int id)
        {
            var sale = _context.Sales
                .Include(s => s.SaleItems)
                .ThenInclude(i => i.Item)
                .Include(s => s.Payment)
                .FirstOrDefault(s => s.Id == id);

            if (sale == null)
                return NotFound();

            return View(sale);
        }


        // POST: /Sale/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Sale model)
        {
            if (id != model.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            var existing = _context.Sales
                .Include(s => s.SaleItems)
                .Include(s => s.Payment)
                .FirstOrDefault(s => s.Id == id);

            if (existing == null)
                return NotFound();

            // Update main fields
            existing.InvoiceNumber = model.InvoiceNumber;
            existing.SaleDate = model.SaleDate;
            existing.CashierId = model.CashierId;
            existing.SubTotal = model.SubTotal;
            existing.Tax = model.Tax;
            existing.Discount = model.Discount;
            existing.Total = model.Total;

            // Update Payment details
            //if (existing.Payment != null && model.Payment != null)
            //{
            //    existing.Payment.AmountPaid = model.Payment.AmountPaid;
            //    existing.Payment.Method = model.Payment.Method;
            //    // add other payment-related updates
            //}

            // Optionally: update SaleItems if you're editing line items in the same view

            _context.SaveChanges();
            TempData["Message"] = "Sale updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // Add this model class to your project
        public class SaleItemModel
        {
            public int ItemId { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal DiscountPercent { get; set; }
            public decimal TaxAmount { get; set; }
            public decimal LineTotal { get; set; }
        }
        public IActionResult Receipt(int id)
        {
            var sale = _context.Sales
                               .Include(s => s.Payment)
                               .Include(s => s.SaleItems)
                               .FirstOrDefault(s => s.Id == id);

            ViewBag.Setting = _context.ShopSettings.OrderByDescending(s => s.Id).FirstOrDefault() ?? new ShopSetting();

            if (sale == null)
                return NotFound();
            ViewBag.Balance = _context.Customers.Where(c => c.Id == sale.CustomerId).Select(c => c.OpeningBalance).FirstOrDefault();

            var vm = new SaleReceiptViewModel
            {
                SaleId = sale.Id,
                InvoiceNumber = sale.InvoiceNumber,
                SaleDate = sale.SaleDate,
                CashierName = sale.CashierId,
                SubTotal = sale.SubTotal,
                Tax = sale.Tax,
                Discount = sale.Discount,
                Total = sale.Total,

                // client-side fetching
                CustomerId = sale.CustomerId,
                CustomerName = _context.Customers
                                       .AsEnumerable()
                                       .Where(c => c.Id == sale.CustomerId)
                                       .Select(c => c.CustomerName)
                                       .FirstOrDefault() ?? "Walk-in Customer",
                CustomerPhone = _context.Customers
                                        .AsEnumerable()
                                        .Where(c => c.Id == sale.CustomerId)
                                        .Select(c => c.ContactNumber)
                                        .FirstOrDefault(),

                PaymentMethod = sale.Payment?.PaymentMethod ?? "Cash",
                PaidAmount = sale.Payment?.Amount ?? sale.Total,
                Change = (sale.Payment?.Amount ?? sale.Total) - sale.Total,

                Items = (from si in _context.SaleItems
                         join i in _context.Items on si.ItemId equals i.Id
                         where si.SaleId == sale.Id
                         select new SaleItemReceiptVM
                         {
                             SrNo = 0,
                             ItemName = i.ItemName,
                             UnitPrice = si.UnitPrice,
                             Quantity = si.Quantity,
                             LineTotal = si.LineTotal
                         }).ToList()
            };

            // assign SrNo
            for (int i = 0; i < vm.Items.Count; i++)
            {
                vm.Items[i].SrNo = i + 1;
            }

            return View(vm);
        }

        public IActionResult LastDaySales()
        {
            // Step 1: Get the last sale date
            var lastSaleDate = _context.Sales
                                       .OrderByDescending(s => s.SaleDate)
                                       .Select(s => s.SaleDate.Date) // only date part
                                       .FirstOrDefault();

            if (lastSaleDate == default)
                return NotFound("No sales found.");

            // Step 2: Get all sales on that date
            var sales = _context.Sales
                                .Include(s => s.Payment)
                                .Include(s => s.SaleItems)
                                .Where(s => s.SaleDate.Date == lastSaleDate)
                                .ToList();

            // Optional: Map to view models
            var vmList = sales.Select(sale => new SaleReceiptViewModel
            {
                SaleId = sale.Id,
                InvoiceNumber = sale.InvoiceNumber,
                SaleDate = sale.SaleDate,
                CashierName = sale.CashierId,
                SubTotal = sale.SubTotal,
                Tax = sale.Tax,
                Discount = sale.Discount,
                Total = sale.Total,
                CustomerId = sale.CustomerId,
                CustomerName = _context.Customers
                                       .Where(c => c.Id == sale.CustomerId)
                                       .Select(c => c.CustomerName)
                                       .FirstOrDefault() ?? "Walk-in Customer",
                CustomerPhone = _context.Customers
                                        .Where(c => c.Id == sale.CustomerId)
                                        .Select(c => c.ContactNumber)
                                        .FirstOrDefault(),
                PaymentMethod = sale.Payment?.PaymentMethod ?? "Cash",
                PaidAmount = sale.Payment?.Amount ?? sale.Total,
                Change = (sale.Payment?.Amount ?? sale.Total) - sale.Total,

                Items = (from si in _context.SaleItems
                         join i in _context.Items on si.ItemId equals i.Id
                         where si.SaleId == sale.Id
                         select new SaleItemReceiptVM
                         {
                             SrNo = 0,
                             ItemName = i.ItemName,
                             UnitPrice = si.UnitPrice,
                             Quantity = si.Quantity,
                             LineTotal = si.LineTotal
                         }).ToList()
            }).ToList();

            // assign SrNo for each sale's items
            foreach (var vm in vmList)
            {
                for (int i = 0; i < vm.Items.Count; i++)
                    vm.Items[i].SrNo = i + 1;
            }

            return View(vmList); // send list of sales for the last date
        }


        public IActionResult KOTReceipt(int id)
        {
            var sale = _context.Sales
                               .Include(s => s.Payment)
                               .Include(s => s.SaleItems)
                               .FirstOrDefault(s => s.Id == id);

            ViewBag.Setting = _context.ShopSettings.OrderByDescending(s => s.Id).FirstOrDefault() ?? new ShopSetting();

            if (sale == null)
                return NotFound();
            ViewBag.Balance = _context.Customers.Where(c => c.Id == sale.CustomerId).Select(c => c.OpeningBalance).FirstOrDefault();

            var vm = new SaleReceiptViewModel
            {
                SaleId = sale.Id,
                InvoiceNumber = sale.InvoiceNumber,
                SaleDate = sale.SaleDate,
                CashierName = sale.CashierId,
                Waiter = sale.Waiter,
                TableNo = sale.TableNo,
                SubTotal = sale.SubTotal,
                Tax = sale.Tax,
                Discount = sale.Discount,
                Total = sale.Total,

                // client-side fetching
                CustomerId = sale.CustomerId,
                CustomerName = _context.Customers
                                       .AsEnumerable()
                                       .Where(c => c.Id == sale.CustomerId)
                                       .Select(c => c.CustomerName)
                                       .FirstOrDefault() ?? "Walk-in Customer",
                CustomerPhone = _context.Customers
                                        .AsEnumerable()
                                        .Where(c => c.Id == sale.CustomerId)
                                        .Select(c => c.ContactNumber)
                                        .FirstOrDefault(),

                PaymentMethod = sale.Payment?.PaymentMethod ?? "Cash",
                PaidAmount = sale.Payment?.Amount ?? sale.Total,
                Change = (sale.Payment?.Amount ?? sale.Total) - sale.Total,

                Items = (from si in _context.SaleItems
                         join i in _context.Items on si.ItemId equals i.Id
                         where si.SaleId == sale.Id
                         select new SaleItemReceiptVM
                         {
                             SrNo = 0,
                             ItemName = i.ItemName,
                             UnitPrice = si.UnitPrice,
                             Quantity = si.Quantity,
                             LineTotal = si.LineTotal
                         }).ToList()
            };

            // assign SrNo
            for (int i = 0; i < vm.Items.Count; i++)
            {
                vm.Items[i].SrNo = i + 1;
            }

            return View(vm);
        }


        private string GenerateInvoiceNumber()
        {
            var now = DateTime.Now;
            return $"INV-{now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
        }

        [HttpGet]
        public IActionResult GetProductsJson()
        {
            // Maan ke chaliye aapke DbContext mein Products DbSet hai:
            // var list = _context.Products.Select(p => new {
            //     id = p.Id,
            //     name = p.Name,
            //     price = p.SalePrice,
            //     imageUrl = string.IsNullOrEmpty(p.ImagePath) ? "/images/default.png" : p.ImagePath
            // }).ToList();
            // return Json(list);

            // Agar abhi Products table nahi hai, to dummy data return karo:
            var dummy = new[] {
                new { id = 1, name = "Jeans", price = 35.00m, imageUrl = "/images/products/jeans.jpg" },
                new { id = 2, name = "T-Shirt", price = 15.00m, imageUrl = "/images/products/tshirt.jpg" },
                new { id = 3, name = "Sunglasses", price = 199.00m, imageUrl = "/images/products/sunglasses.jpg" },
                new { id = 4, name = "Watch", price = 350.00m, imageUrl = "/images/products/watch.jpg" },
                // add more dummy items or fetch real once DB ready
            };
            return Json(dummy);
        }
        [HttpGet]
        public IActionResult SearchItems(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new List<Item>());
            }

            var items = _context.Items
                .Where(i => i.ItemName.Contains(term) || i.ItemCode.Contains(term))
                .Take(5)
                .Select(i => new
                {
                    i.Id,
                    i.ItemName,
                    i.ItemCode,
                    i.SalePrice
                })
                .ToList();

            return Json(items);
        }


        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            // ✅ Check if current user is Admin
            if (!User.IsInRole("Admin"))
            {
                // Return a JSON result (handled by JS SweetAlert)
                return Json(new { success = false, message = "Only Admin users can delete sales." });
            }

            var sale = _context.Sales
                               .Include(s => s.Payment)
                               .Include(s => s.SaleItems)
                               .FirstOrDefault(s => s.Id == id);

            if (sale == null)
                return Json(new { success = false, message = "Sale not found." });

            if (sale.CustomerId != null && sale.CustomerId != 0)
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Id == sale.CustomerId);

                if (customer != null)
                {
                    customer.OpeningBalance = customer.OpeningBalance + sale.Total;
                    ViewBag.Balance = customer.OpeningBalance;

                    var payment = await _context.CustomerPayments
                        .FirstOrDefaultAsync(p => p.CustomerId == sale.CustomerId);

                    if (payment != null)
                    {
                        decimal currentSale = payment.Sale ?? 0m;
                        decimal currentAmount = payment.Amount;
                        payment.Sale = currentSale - sale.Total;
                        payment.Remaining = currentAmount - (payment.Sale ?? 0m);
                    }

                    await _context.SaveChangesAsync();
                }
            }

            var saleItems = (from si in _context.SaleItems
                             join i in _context.Items on si.ItemId equals i.Id
                             where si.SaleId == sale.Id
                             select new { Item = i, SaleItem = si }).ToList();

            foreach (var entry in saleItems)
            {
                entry.Item.Quantity += entry.SaleItem.Quantity;
            }

            _context.Sales.Remove(sale);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Sale deleted successfully." });
        }

    }
}
