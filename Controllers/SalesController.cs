using GSoftPosNew.Data;
using GSoftPosNew.Models;
using GSoftPosNew.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

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

        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate, string search, int customerId)
        {
            var query = _context.Sales.AsQueryable();
            var queryPurchase = _context.Purchases.AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(s => s.SaleDate.Date >= fromDate.Value);
                queryPurchase = queryPurchase.Where(s => s.Date.Date >= fromDate.Value);
            }
            else
            {
                query = query.Where(s => s.SaleDate.Date == DateTime.Now.Date);
                queryPurchase = queryPurchase.Where(s => s.Date.Date == DateTime.Now.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(s => s.SaleDate.Date <= toDate.Value);
                queryPurchase = queryPurchase.Where(s => s.Date.Date <= toDate.Value);
            }
            else
            {
                query = query.Where(s => s.SaleDate.Date == DateTime.Now.Date);
                queryPurchase = queryPurchase.Where(s => s.Date.Date == DateTime.Now.Date);
            }

            if (!string.IsNullOrEmpty(search))
                query = query.Where(s => s.InvoiceNumber.Contains(search) || s.CashierId.Contains(search));

            if (customerId > 0)
                query = query.Where(s => s.CustomerId == customerId);

            var model = new SalesFilterViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                Search = search,
                CustomerId = customerId,
                Sales = query
                    .OrderByDescending(s => s.SaleDate)
                    .Include(s => s.SaleItems)
                    .ThenInclude(s => s.Item)
                    .ThenInclude(i => i.Category)
                    .ToList()
            };

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Customers = await _context.Customers.ToListAsync();
            ViewBag.Purchase = queryPurchase.Include(p => p.Items).ToList();

            return View(model);
        }

        public IActionResult POS(string search, string next, string previous)
        {

            var sale = new Sale();
            var saleItems = new List<SaleItem>();
            var customer = new Customer();
            var payment = new Payment();

            if (!string.IsNullOrWhiteSpace(search))
            {


                sale = _context.Sales.Where(s =>
                    s.InvoiceNumber != null &&
                    s.InvoiceNumber.Contains(search))
                .OrderByDescending(s => s.SaleDate)
                .FirstOrDefault();
            }

            if (!string.IsNullOrWhiteSpace(previous))
            {
                string currentInvoice = previous.Trim();
                var parts = currentInvoice.Split('-');

                if (parts.Length == 3)
                {
                    string prefix = parts[0];       // INV
                    string datePart = parts[1];     // 20260220
                    string numberPart = parts[2];   // 0001

                    if (int.TryParse(numberPart, out int numericPart))
                    {
                        numericPart--;

                        // =============================
                        // NORMAL CASE (still > 0)
                        // =============================
                        if (numericPart > 0)
                        {
                            string newInvoice =
                                $"{prefix}-{datePart}-{numericPart.ToString("D4")}";

                            sale = _context.Sales
                                .FirstOrDefault(s => s.InvoiceNumber == newInvoice);
                        }
                        else
                        {
                            // =============================
                            // IF BECOMES 0000 → GO TO PREVIOUS DAY
                            // =============================

                            if (DateTime.TryParseExact(
                                datePart,
                                "yyyyMMdd",
                                null,
                                System.Globalization.DateTimeStyles.None,
                                out DateTime invoiceDate))
                            {
                                DateTime checkDate = invoiceDate.AddDays(-1);
                                Sale foundSale = null;

                                // Get earliest sale date in database (boundary safety)
                                DateTime? earliestSaleDate = _context.Sales
                                    .Where(s => s.SaleDate != null)
                                    .Min(s => (DateTime?)s.SaleDate);

                                while (earliestSaleDate.HasValue && checkDate.Date >= earliestSaleDate.Value.Date)
                                {
                                    string dateString = checkDate.ToString("yyyyMMdd");
                                    string prefixToSearch = $"{prefix}-{dateString}-";

                                    foundSale = _context.Sales
                                        .Where(s =>
                                            s.InvoiceNumber != null &&
                                            s.InvoiceNumber.StartsWith(prefixToSearch))
                                        .OrderByDescending(s => s.InvoiceNumber)
                                        .FirstOrDefault();

                                    if (foundSale != null)
                                        break;

                                    // subtract another day
                                    checkDate = checkDate.AddDays(-1);
                                }

                                // If nothing found
                                if (foundSale == null)
                                {
                                    foundSale = new Sale();
                                }

                                sale = foundSale;
                            }
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(next))
            {
                string currentInvoice = next.Trim();
                var parts = currentInvoice.Split('-');

                if (parts.Length == 3 && int.TryParse(parts[2], out int numberPart))
                {
                    numberPart++;

                    string newInvoice =
                        $"{parts[0]}-{parts[1]}-{numberPart.ToString("D4")}";

                    sale = _context.Sales
                        .FirstOrDefault(s => s.InvoiceNumber == newInvoice);

                    // If next invoice does not exist → get FIRST invoice
                    if (sale == null)
                    {
                        sale = new Sale();
                    }
                }
            }

            if (sale != null)
            {
                saleItems = _context.SaleItems.Where(si => si.SaleId == sale.Id).ToList();
                customer = _context.Customers.Where(c => c.Id == sale.CustomerId).FirstOrDefault();
                payment = _context.Payments.Where(p => p.SaleId == sale.Id).FirstOrDefault();
            }

            sale.SaleItems = saleItems;
            sale.Customers = customer;
            sale.Payment = payment;

            ViewBag.Sale = sale;

            ViewBag.Search = search;


            var items = _context.Items.ToList();
            var categories = items.Select(i => i.GenericName).Distinct().ToList();
            ViewBag.CustomerList = _context.Customers.ToList();
            ViewBag.CustomerPayments = _context.CustomerPayments.ToList();

            ViewBag.Categories = categories;
            ViewBag.CashierId = User.Identity?.Name ?? "Unknown";

            var saleInvNo = _context.Sales
                 .Where(s => s.SaleDate.Date == DateTime.Now.Date)
                .OrderByDescending(s => s.Id)
                .Select(s => s.InvoiceNumber)
                .FirstOrDefault();

            ViewBag.InvoiceLastDigits = !string.IsNullOrEmpty(saleInvNo) && saleInvNo.Length >= 4
                ? saleInvNo.Substring(saleInvNo.Length - 4)
                : "0000";

            ViewBag.ShopName = _context.ShopSettings
                .OrderByDescending(s => s.Id)
                .Select(s => s.ShopName)
                .FirstOrDefault();

            ViewBag.SaleLowStock = _context.ShopSettings
                .OrderByDescending(s => s.Id)
                .Select(s => s.SaleLowStock)
                .FirstOrDefault();

            return View(items);
        }

        // ==================== SALES RETURN REPORT (GET) ====================
        [HttpGet]
        public IActionResult SalesReturn()
        {
            var model = new SalesReturnFilterViewModel
            {
                FromDate = DateTime.Today,
                ToDate = DateTime.Today
            };

            // Dropdowns fill
            PopulateSalesReturnDropdowns(model);

            // Default: aaj ki returns dikhao
            model.Rows = LoadSalesReturnRows(
                model.FromDate.Value,
                model.ToDate.Value,
                null,
                0,
                0
            );

            return View(model); // Views/Sales/SalesReturn.cshtml
        }

        // ==================== SALES RETURN REPORT (POST - FILTER) ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SalesReturn(SalesReturnFilterViewModel filter)
        {
            // Dates normalize
            if (filter.FromDate == null && filter.ToDate == null)
            {
                filter.FromDate = DateTime.Today;
                filter.ToDate = DateTime.Today;
            }

            if (filter.FromDate != null && filter.ToDate == null)
                filter.ToDate = filter.FromDate;

            if (filter.ToDate != null && filter.FromDate == null)
                filter.FromDate = filter.ToDate;

            // Rows load via helper (sirf Return wali sales se)
            filter.Rows = LoadSalesReturnRows(
                filter.FromDate!.Value,
                filter.ToDate!.Value,
                filter.ItemSearch,
                filter.CategoryId,
                filter.SupplierId
            );

            // Dropdowns dubara fill (POST pe bhi)
            PopulateSalesReturnDropdowns(filter);

            return View(filter);
        }

        // Sirf Return wali sales se grid ke rows banaane ka helper
        private List<SalesReturnRow> LoadSalesReturnRows(
            DateTime fromDate,
            DateTime toDate,
            string? itemSearch,
            int categoryId,
            int supplierId)
        {
            var from = fromDate.Date;
            var to = toDate.Date.AddDays(1).AddTicks(-1);

            // Base: sirf SaleType == "Return"
            var salesQuery = _context.Sales
                .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Item)
                        .ThenInclude(i => i.Category)
                .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Item)
                        .ThenInclude(i => i.Supplier)
                .Where(s => s.SaleType == "Return")
                .Where(s => s.SaleDate >= from && s.SaleDate <= to)
                .AsQueryable();

            // SaleItems flatten
            var q = from s in salesQuery
                    from si in s.SaleItems
                    let itm = si.Item
                    select new
                    {
                        Sale = s,
                        Line = si,
                        Item = itm
                    };

            // Item search (name / code)
            if (!string.IsNullOrWhiteSpace(itemSearch))
            {
                var search = itemSearch.Trim().ToLower();
                q = q.Where(x =>
                    x.Item.ItemName.ToLower().Contains(search) ||
                    x.Item.ItemCode.ToLower().Contains(search));
            }

            // Category filter
            if (categoryId > 0)
            {
                q = q.Where(x => x.Item.CategoryId == categoryId);
            }

            // Supplier filter
            if (supplierId > 0)
            {
                q = q.Where(x => x.Item.SupplierId == supplierId);
            }

            var rows = q
                .OrderBy(x => x.Sale.SaleDate)
                .ThenBy(x => x.Item.ItemName)
                .Select(x => new SalesReturnRow
                {
                    InvoiceNo = x.Sale.InvoiceNumber,
                    ItemCode = x.Item.ItemCode,
                    ItemName = x.Item.ItemName,
                    Quantity = x.Line.Quantity,
                    Amount = x.Line.LineTotal,
                    ReturnDate = x.Sale.SaleDate,
                    CategoryName = x.Item.Category != null ? x.Item.Category.Name : "",
                    SupplierName = x.Item.Supplier != null ? x.Item.Supplier.SupplierName : ""
                })
                .ToList();

            return rows;
        }

        private void PopulateSalesReturnDropdowns(SalesReturnFilterViewModel model)
        {
            // Categories dropdown
            model.Categories = _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToList();

            // Suppliers dropdown
            model.Suppliers = _context.Suppliers
                .OrderBy(s => s.SupplierName)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.SupplierName
                })
                .ToList();

            // Top bar ke liye
            ViewBag.CurrentUser = User.Identity?.Name ?? "Admin";
        }

        public async Task<IActionResult> POSTouch()
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

            var saleInvNo = _context.Sales
                .OrderByDescending(s => s.Id)
                .Select(s => s.InvoiceNumber)
                .FirstOrDefault();

            ViewBag.InvoiceLastDigit = !string.IsNullOrEmpty(saleInvNo)
                ? int.Parse(saleInvNo[saleInvNo.Length - 1].ToString())
                : 0;

            ViewBag.ShopName = _context.ShopSettings
                .OrderByDescending(s => s.Id)
                .Select(s => s.ShopName)
                .FirstOrDefault();

            ViewBag.ServiceCharges = _context.ShopSettings
                .OrderByDescending(s => s.Id)
                .Select(s => s.ServiceCharges)
                .FirstOrDefault();

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

            var saleInvNo = _context.Sales
                .OrderByDescending(s => s.Id)
                .Select(s => s.InvoiceNumber)
                .FirstOrDefault();

            ViewBag.InvoiceLastDigit = !string.IsNullOrEmpty(saleInvNo)
                ? int.Parse(saleInvNo[saleInvNo.Length - 1].ToString())
                : 0;

            ViewBag.ShopName = _context.ShopSettings
                .OrderByDescending(s => s.Id)
                .Select(s => s.ShopName)
                .FirstOrDefault();

            return View(items);
        }

        [HttpPost]
        public async Task<IActionResult> SaveSale([FromBody] Sale sale, int serviceCharges)
        {
            if (sale == null || sale.SaleItems == null || !sale.SaleItems.Any())
            {
                using var reader = new StreamReader(Request.Body);
                var rawBody = await reader.ReadToEndAsync();
                return BadRequest("Invalid sale data. Raw request: " + rawBody);
            }

            var serviceChargesValue = _context.ShopSettings
                 .OrderByDescending(s => s.Id)
                 .Select(s => s.ServiceCharges)
                 .FirstOrDefault();

            bool isApplied = false;

            if (serviceCharges == 1)
            {
                isApplied = true;
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

                sale.Discount = sale.SaleItems
                                .Where(x => x.DiscountPercent > 0)
                                .Sum(x => (x.UnitPrice * x.Quantity) * x.DiscountPercent / 100m);


                if (isApplied && serviceChargesValue.HasValue && serviceChargesValue > 0)
                {
                    sale.ServiceCharges = sale.Total * serviceChargesValue.Value / 100;
                    sale.Total += sale.ServiceCharges ?? 0;
                }

                if (sale.SaleType == "Return")
                {
                    if (sale.Payment != null)
                        sale.Payment.Id = 0;

                    foreach (var item in sale.SaleItems)
                        item.Id = 0;

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
                                    decimal extraAdvance = Math.Abs(balance);

                                    if (existingPayment.Remaining > 0)
                                    {
                                        if (existingPayment.Remaining >= extraAdvance)
                                        {
                                            existingPayment.Remaining += extraAdvance;
                                            extraAdvance = 0;
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

                            customer.OpeningBalance = (existingPayment?.Remaining ?? (balance > 0 ? balance : 0));

                            await _context.SaveChangesAsync();
                        }
                    }

                    foreach (var item in sale.SaleItems)
                    {
                        var dbItem = await _context.Items.FindAsync(item.ItemId);
                        if (dbItem != null)
                        {
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
                        if (sale.CustomerId == null || sale.CustomerId == 0)
                            continue;

                        var vanStock = new CustomerVanStock
                        {
                            CustomerId = sale.CustomerId,
                            ItemId = item.ItemId,
                            Quantity = item.Quantity
                        };

                        vanStockList.Add(vanStock);
                    }

                    if (vanStockList.Any())
                    {
                        await _context.AddRangeAsync(vanStockList);
                        await _context.SaveChangesAsync();
                    }

                    if (sale.Payment?.PaymentMethod?.ToLower() == "credit")
                    {
                        var customer = await _context.Customers.FindAsync(sale.custId);
                        if (customer != null)
                        {
                            decimal paidAmount = sale.tender_amount;
                            decimal totalAmount = sale.Total;
                            decimal balance = totalAmount - paidAmount;

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
                                    decimal extraAdvance = Math.Abs(balance);

                                    if (existingPayment.Remaining > 0)
                                    {
                                        if (existingPayment.Remaining >= extraAdvance)
                                        {
                                            existingPayment.Remaining += extraAdvance;
                                            extraAdvance = 0;
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

                            customer.OpeningBalance = (existingPayment?.Remaining ?? (balance > 0 ? balance : 0));

                            await _context.SaveChangesAsync();
                        }
                    }

                    foreach (var item in sale.SaleItems)
                    {
                        var dbItem = await _context.Items
                            .Include(i => i.ItemIngredients)
                            .Where(i => i.Id == item.ItemId)
                            .FirstOrDefaultAsync();
                        if (dbItem != null)
                        {
                            dbItem.Quantity -= item.Quantity;
                            _context.Items.Update(dbItem);

                            if (dbItem.ItemIngredients != null)
                            {
                                foreach (var ingItem in dbItem.ItemIngredients)
                                {
                                    var ingredient = _context.Ingredients
                                                        .FirstOrDefault(x => x.Id == ingItem.IngredientId);

                                    if (ingredient != null)
                                    {
                                        decimal currentStock = ingredient.PurchaseQty ?? 0;
                                        decimal newStock = currentStock - ingItem.UseQty;

                                        ingredient.PurchaseQty = newStock;
                                    }
                                }
                            }
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
                            decimal balance = totalAmount + paidAmount;

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
                                    decimal extraAdvance = Math.Abs(balance);

                                    if (existingPayment.Remaining > 0)
                                    {
                                        if (existingPayment.Remaining >= extraAdvance)
                                        {
                                            existingPayment.Remaining += extraAdvance;
                                            extraAdvance = 0;
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

                            customer.OpeningBalance = (existingPayment?.Remaining ?? (balance > 0 ? balance : 0));

                            await _context.SaveChangesAsync();
                        }
                    }

                    foreach (var item in sale.SaleItems)
                    {
                        var dbItem = await _context.Items.FindAsync(item.ItemId);
                        if (dbItem != null)
                        {
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
                            decimal balance = totalAmount - paidAmount;

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
                                    decimal extraAdvance = Math.Abs(balance);

                                    if (existingPayment.Remaining > 0)
                                    {
                                        if (existingPayment.Remaining >= extraAdvance)
                                        {
                                            existingPayment.Remaining += extraAdvance;
                                            extraAdvance = 0;
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

                            customer.OpeningBalance = (existingPayment?.Remaining ?? (balance > 0 ? balance : 0));

                            await _context.SaveChangesAsync();
                        }
                    }

                    foreach (var item in sale.SaleItems)
                    {
                        var dbItem = await _context.Items.FindAsync(item.ItemId);
                        if (dbItem != null)
                        {
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
        public async Task<IActionResult> SaveOnly([FromBody] Sale sale, int serviceCharges)
        {
            if (sale == null || sale.SaleItems == null || !sale.SaleItems.Any())
            {
                using var reader = new StreamReader(Request.Body);
                var rawBody = await reader.ReadToEndAsync();
                return BadRequest("Invalid sale data. Raw request: " + rawBody);
            }

            var serviceChargesValue = _context.ShopSettings
                .OrderByDescending(s => s.Id)
                .Select(s => s.ServiceCharges)
                .FirstOrDefault();

            using var transaction = await _context.Database.BeginTransactionAsync();

            bool isApplied = false;

            if (serviceCharges == 1)
            {
                isApplied = true;
            }

            try
            {
                if (string.IsNullOrEmpty(sale.InvoiceNumber))
                    sale.InvoiceNumber = GenerateInvoiceNumber();

                sale.SaleDate = DateTime.Now;
                sale.CashierId = User.Identity?.Name ?? "Unknown";
                sale.Id = 0;
                sale.CustomerId = sale.custId;

                sale.Discount = sale.SaleItems
                                .Where(x => x.DiscountPercent > 0)
                                .Sum(x => (x.UnitPrice * x.Quantity) * x.DiscountPercent / 100m);

                if (isApplied && serviceChargesValue.HasValue && serviceChargesValue > 0)
                {
                    sale.ServiceCharges = sale.Total * serviceChargesValue.Value / 100;
                    sale.Total += sale.ServiceCharges ?? 0;
                }

                if (sale.SaleType == "Return")
                {
                    if (sale.Payment != null)
                        sale.Payment.Id = 0;

                    foreach (var item in sale.SaleItems)
                        item.Id = 0;

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
                            decimal balance = totalAmount + paidAmount;

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
                                    decimal extraAdvance = Math.Abs(balance);

                                    if (existingPayment.Remaining > 0)
                                    {
                                        if (existingPayment.Remaining >= extraAdvance)
                                        {
                                            existingPayment.Remaining += extraAdvance;
                                            extraAdvance = 0;
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

                            customer.OpeningBalance = (existingPayment?.Remaining ?? (balance > 0 ? balance : 0));

                            await _context.SaveChangesAsync();
                        }
                    }

                    foreach (var item in sale.SaleItems)
                    {
                        var dbItem = await _context.Items.FindAsync(item.ItemId);
                        if (dbItem != null)
                        {
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
                        if (sale.CustomerId == null || sale.CustomerId == 0)
                            continue;

                        var vanStock = new CustomerVanStock
                        {
                            CustomerId = sale.CustomerId,
                            ItemId = item.ItemId,
                            Quantity = item.Quantity
                        };

                        vanStockList.Add(vanStock);
                    }

                    if (vanStockList.Any())
                    {
                        await _context.AddRangeAsync(vanStockList);
                        await _context.SaveChangesAsync();
                    }

                    if (sale.Payment?.PaymentMethod?.ToLower() == "credit")
                    {
                        var customer = await _context.Customers.FindAsync(sale.custId);
                        if (customer != null)
                        {
                            decimal paidAmount = sale.tender_amount;
                            decimal totalAmount = sale.Total;
                            decimal balance = totalAmount - paidAmount;

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
                                    decimal extraAdvance = Math.Abs(balance);

                                    if (existingPayment.Remaining > 0)
                                    {
                                        if (existingPayment.Remaining >= extraAdvance)
                                        {
                                            existingPayment.Remaining += extraAdvance;
                                            extraAdvance = 0;
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

                            customer.OpeningBalance = (existingPayment?.Remaining ?? (balance > 0 ? balance : 0));

                            await _context.SaveChangesAsync();
                        }
                    }

                    foreach (var item in sale.SaleItems)
                    {
                        var dbItem = await _context.Items
                                                .Include(i => i.ItemIngredients)
                                                .Where(i => i.Id == item.ItemId)
                                                .FirstOrDefaultAsync();
                        if (dbItem != null)
                        {

                            dbItem.Quantity -= item.Quantity;
                            _context.Items.Update(dbItem);

                            if (dbItem.ItemIngredients != null)
                            {
                                foreach (var ingItem in dbItem.ItemIngredients)
                                {
                                    var ingredient = _context.Ingredients
                                                        .FirstOrDefault(x => x.Id == ingItem.IngredientId);

                                    if (ingredient != null)
                                    {

                                        decimal currentStock = ingredient.PurchaseQty ?? 0;
                                        decimal newStock = currentStock - ingItem.UseQty;

                                        ingredient.PurchaseQty = newStock;

                                    }
                                }
                            }
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
            int customerId = int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");

            var customers = new List<Customer>();

            if (customerId != 0)
            {
                customers = await _context.Customers
                    .Where(c => c.Id == customerId)
                    .ToListAsync();
            }
            else
            {
                customers = await _context.Customers.ToListAsync();
            }

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
                    debit += p.Sale ?? 0;
                    credit += p.Amount;
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
                var saleItems = System.Text.Json.JsonSerializer
                    .Deserialize<List<SaleItemModel>>(ItemsJson)
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
            catch
            {
                ModelState.AddModelError("", "An error occurred while processing your payment.");
                return RedirectToAction(nameof(POS));
            }
        }

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

            existing.InvoiceNumber = model.InvoiceNumber;
            existing.SaleDate = model.SaleDate;
            existing.CashierId = model.CashierId;
            existing.SubTotal = model.SubTotal;
            existing.Tax = model.Tax;
            existing.Discount = model.Discount;
            existing.Total = model.Total;

            _context.SaveChanges();
            TempData["Message"] = "Sale updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult TempReceipt(string jsonData)
        {
            ViewBag.Setting = _context.ShopSettings
                .OrderByDescending(s => s.Id)
                .FirstOrDefault() ?? new ShopSetting();

            if (string.IsNullOrEmpty(jsonData))
                return BadRequest();

            var vm = JsonConvert.DeserializeObject<SaleReceiptViewModel>(jsonData);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> TempPrint([FromBody] Sale sale)
        {
            var items = (from si in sale.SaleItems
                         join i in _context.Items on si.ItemId equals i.Id
                         select new SaleItemReceiptVM
                         {
                             SrNo = 0,
                             ItemName = i.ItemName,
                             UnitPrice = si.UnitPrice,
                             Quantity = si.Quantity,
                             LineTotal = si.LineTotal
                         })
                        .ToList();

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
                ServiceCharges = sale.ServiceCharges,
                PaymentMethod = sale.Payment?.PaymentMethod ?? "Cash",
                Waiter = sale.Waiter,
                TableNo = sale.TableNo,
                Items = items
            };

            for (int i = 0; i < vm.Items.Count; i++)
            {
                vm.Items[i].SrNo = i + 1;
            }

            return Ok(new
            {
                Success = true,
                Message = "Sale saved successfully",
                Data = vm
            });
        }

        public class SaleItemModel
        {
            public int ItemId { get; set; }
            public decimal Quantity { get; set; }
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

            ViewBag.Setting = _context.ShopSettings
                .OrderByDescending(s => s.Id)
                .FirstOrDefault() ?? new ShopSetting();

            if (sale == null)
                return NotFound();

            ViewBag.Balance = _context.Customers
                .Where(c => c.Id == sale.CustomerId)
                .Select(c => c.OpeningBalance)
                .FirstOrDefault();

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
                ServiceCharges = sale.ServiceCharges,
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
                             DiscountPercent = si.DiscountPercent,   // ✅ ADD
                             TaxAmount = si.TaxAmount,               // ✅ ADD
                             LineTotal = si.LineTotal
                         }).ToList()
            };

            for (int i = 0; i < vm.Items.Count; i++)
            {
                vm.Items[i].SrNo = i + 1;
            }

            return View(vm);
        }

        public IActionResult LastDaySales()
        {
            var lastSaleDate = _context.Sales
                .OrderByDescending(s => s.SaleDate)
                .Select(s => s.SaleDate.Date)
                .FirstOrDefault();

            if (lastSaleDate == default)
                return NotFound("No sales found.");

            var sales = _context.Sales
                .Include(s => s.Payment)
                .Include(s => s.SaleItems)
                .Where(s => s.SaleDate.Date == lastSaleDate)
                .ToList();

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

            foreach (var vm in vmList)
            {
                for (int i = 0; i < vm.Items.Count; i++)
                    vm.Items[i].SrNo = i + 1;
            }

            return View(vmList);
        }

        public IActionResult KOTReceipt(int id)
        {
            var sale = _context.Sales
                .Include(s => s.Payment)
                .Include(s => s.SaleItems)
                .FirstOrDefault(s => s.Id == id);

            ViewBag.Setting = _context.ShopSettings
                .OrderByDescending(s => s.Id)
                .FirstOrDefault() ?? new ShopSetting();

            if (sale == null)
                return NotFound();

            ViewBag.Balance = _context.Customers
                .Where(c => c.Id == sale.CustomerId)
                .Select(c => c.OpeningBalance)
                .FirstOrDefault();

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
            var dummy = new[]
            {
                new { id = 1, name = "Jeans", price = 35.00m, imageUrl = "/images/products/jeans.jpg" },
                new { id = 2, name = "T-Shirt", price = 15.00m, imageUrl = "/images/products/tshirt.jpg" },
                new { id = 3, name = "Sunglasses", price = 199.00m, imageUrl = "/images/products/sunglasses.jpg" },
                new { id = 4, name = "Watch", price = 350.00m, imageUrl = "/images/products/watch.jpg" },
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

        public IActionResult Details(int id)
        {
            var sale = _context.Sales
                .Include(s => s.Payment)
                .Include(s => s.SaleItems)
                .FirstOrDefault(s => s.Id == id);

            ViewBag.Setting = _context.ShopSettings
                .OrderByDescending(s => s.Id)
                .FirstOrDefault() ?? new ShopSetting();

            if (sale == null)
                return NotFound();

            ViewBag.Balance = _context.Customers
                .Where(c => c.Id == sale.CustomerId)
                .Select(c => c.OpeningBalance)
                .FirstOrDefault();

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

            for (int i = 0; i < vm.Items.Count; i++)
            {
                vm.Items[i].SrNo = i + 1;
            }

            return View(vm);
        }

        [HttpGet]
        public IActionResult GetFloorsWithTables()
        {
            var floors = _context.Locations
                .Select(loc => new
                {
                    id = loc.Id,
                    name = loc.Name,
                    totalTables = loc.Tables.Count(),
                    tables = loc.Tables.Select(t => new
                    {
                        id = t.Id,
                        name = t.TableName ?? t.TableCode,
                        capacity = t.Capacity ?? 0,
                        status = t.Status
                    }).ToList()
                })
                .ToList();

            return Json(floors);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!User.IsInRole("Admin"))
            {
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
