using GSoftPosNew.Data;
using GSoftPosNew.Models;
using GSoftPosNew.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace GSoftPosNew.Controllers
{

    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        // SALES REPORTS

        public IActionResult Sales()
        {
            var model = new SalesReport
            {
                StartDate = DateTime.Today.AddDays(-7),
                EndDate = DateTime.Today
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult Sales(SalesReport model)
        {
            model.Sales = _context.Sales
                .Where(p => p.SaleDate.Date >= model.StartDate.Date && p.SaleDate.Date <= model.EndDate.Date)
                .Include(s => s.SaleItems)
                .ThenInclude(si => si.Item)
                .OrderByDescending(s => s.SaleDate)
                .ToList();

            return View(model);
        }

        #region Sales Summary

        public IActionResult SalesSummary()
        {
            var model = new SalesSummaryReportVM();
            return View(model);
        }

        [HttpPost]
        public IActionResult SalesSummary(SalesSummaryReportVM model)
        {
            try
            {
                var salesData = _context.Sales
                    .GroupBy(s => 1)
                    .Select(g => new
                    {
                        TotalSales = g.Sum(s => s.Total),
                        TotalTransactions = g.Count()
                    })
                    .FirstOrDefault();

                model.TotalSales = salesData?.TotalSales ?? 0;
                model.TotalTransactions = salesData?.TotalTransactions ?? 0;
                model.AverageSale = model.TotalTransactions > 0 ? model.TotalSales / model.TotalTransactions : 0;

                model.DailySummaries = _context.Sales
                    .GroupBy(s => s.SaleDate.Date)
                    .Select(g => new DailySalesSummary
                    {
                        Date = g.Key,
                        TotalSales = g.Sum(s => s.Total),
                        TransactionCount = g.Count()
                    })
                    .OrderBy(d => d.Date)
                    .ToList();

                return View(model);
            }
            catch
            {
                ModelState.AddModelError("", "An error occurred while generating the report.");
                return View(model);
            }
        }

        public IActionResult SalesByItem()
        {
            var model = new SalesByItemReportVM();
            return View(model);
        }

        [HttpPost]
        public IActionResult SalesByItem(SalesByItemReportVM model)
        {
            try
            {
                var query = _context.SaleItems
                    .Include(si => si.Sale)
                    .Include(si => si.Item)
                    .AsQueryable();

                model.Items = query
                    .GroupBy(si => new { si.Item.Id, si.Item.ItemName, si.Item.ItemCode, si.Item.GenericName })
                    .Select(g => new ItemSalesSummary
                    {
                        ItemId = g.Key.Id,
                        ItemName = g.Key.ItemName,
                        ItemCode = g.Key.ItemCode,
                        Category = g.Key.GenericName,
                        QuantitySold = g.Sum(si => si.Quantity),
                        TotalAmount = g.Sum(si => si.UnitPrice * si.Quantity),
                        AveragePrice = g.Average(si => si.UnitPrice)
                    })
                    .OrderByDescending(i => i.TotalAmount)
                    .ToList();

                model.ItemSales = query
                    .GroupBy(si => new { si.Item.Id })
                    .Select(g => new ItemSalesData
                    {
                        ItemId = g.Key.Id,
                        ItemName = g.First().Item.ItemName,
                        ItemCode = g.First().Item.ItemCode,
                        QuantitySold = g.Sum(si => si.Quantity),
                        TotalSales = g.Sum(si => si.UnitPrice * si.Quantity)
                    })
                    .ToList();

                return View(model);
            }
            catch
            {
                ModelState.AddModelError("", "An error occurred while generating the report.");
                return View(model);
            }
        }

        #endregion

        #region Purchase Reports

        public async Task<IActionResult> Purchases()
        {
            var model = new PurchaseReport
            {
                StartDate = DateTime.Today.AddDays(-7),
                EndDate = DateTime.Today
            };

            ViewBag.SupplierList = new SelectList(
                _context.Suppliers.OrderBy(s => s.SupplierName).ToList(),
                "Id",   // value field
                "SupplierName"          // text field
            );


            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Purchases(PurchaseReport model)
        {
            if(model.SupplierId == 0)
            {
                model.Purchases = await _context.Purchases
                .Where(p => p.Date.Date >= model.StartDate.Date && p.Date.Date <= model.EndDate.Date)
                .Include(s => s.Items)
                .Include(s => s.Supplier)
                .OrderByDescending(s => s.Id)
                .ToListAsync();
            }
            else
            {
                model.Purchases = await _context.Purchases
                .Where(p => p.Date.Date >= model.StartDate.Date && p.Date.Date <= model.EndDate.Date && p.SupplierId == model.SupplierId)
                .Include(s => s.Items)
                .Include(s => s.Supplier)
                .OrderByDescending(s => s.Id)
                .ToListAsync();
            }

            ViewBag.SupplierList = new SelectList(
                _context.Suppliers.OrderBy(s => s.SupplierName).ToList(),
                "Id",   // value field
                "SupplierName"          // text field
            );

            return View(model);
        }

        #endregion

        #region Inventory Reports

        public IActionResult Inventory()
        {
            var model = new InventoryReportVM();
            return View(model);
        }

        [HttpPost]
        public IActionResult Inventory(InventoryReportVM model)
        {
            try
            {
                var query = _context.Items.AsQueryable();

                if (!string.IsNullOrEmpty(model.CategoryFilter))
                {
                    query = query.Where(i => i.GenericName == model.CategoryFilter);
                }

                if (!string.IsNullOrEmpty(model.SearchTerm))
                {
                    query = query.Where(i => i.ItemName.Contains(model.SearchTerm) ||
                                           i.ItemCode.Contains(model.SearchTerm));
                }

                model.Items = query.Select(i => new InventoryItemVM
                {
                    Id = i.Id,
                    ItemCode = i.ItemCode,
                    ItemName = i.ItemName,
                    Category = i.GenericName,
                    Quantity = i.StockQuantity,
                    LowStockThreshold = i.LowStockThreshold,
                    SalePrice = i.SalePrice
                    // CostPrice removed
                }).ToList();

                return View(model);
            }
            catch
            {
                ModelState.AddModelError("", "An error occurred while generating the report.");
                return View(model);
            }
        }

        public IActionResult LowStock()
        {
            var model = new InventoryReportVM
            {
                Items = _context.Items
                    .Where(i => i.StockQuantity <= i.LowStockThreshold)
                    .Select(i => new InventoryItemVM
                    {
                        Id = i.Id,
                        ItemCode = i.ItemCode,
                        ItemName = i.ItemName,
                        Category = i.GenericName,
                        Quantity = i.StockQuantity,
                        LowStockThreshold = i.LowStockThreshold,
                        SalePrice = i.SalePrice
                        // CostPrice removed
                    })
                    .ToList()
            };
            return View(model);
        }

        public IActionResult StockValuation()
        {
            var model = new InventoryReportVM
            {
                Items = _context.Items
                    .Select(i => new InventoryItemVM
                    {
                        Id = i.Id,
                        ItemCode = i.ItemCode,
                        ItemName = i.ItemName,
                        Category = i.GenericName,
                        Quantity = i.StockQuantity,
                        SalePrice = i.SalePrice
                        // CostPrice removed
                    })
                    .OrderByDescending(i => i.Quantity)
                    .ToList()
            };
            return View(model);
        }

        #endregion


        [HttpGet]
        public IActionResult ItemLedger(int? itemId, DateTime? fromDate, DateTime? toDate)
        {
            var model = new ItemLedgerReportVM
            {
                ItemId = itemId,
                FromDate = fromDate ?? DateTime.Today,
                ToDate = toDate ?? DateTime.Today,
                Items = _context.Items


                    .OrderBy(i => i.ItemName)
                    .Select(i => new SelectListItem
                    {


                        Value = i.Id.ToString(),
                        Text = (i.ItemName ?? "") + " (" + (i.ItemCode ?? "") + ")"
                    })
                    .ToList()
            };

            ViewBag.ItemsJson = System.Text.Json.JsonSerializer.Serialize(
    _context.Items
        .OrderBy(i => i.ItemName)
        .Select(i => new
        {
            i.Id,
            i.ItemCode,
            i.ItemName,
            i.SalePrice,
            i.Quantity
        })
        .ToList()
);

            if (!itemId.HasValue || itemId.Value <= 0)
                return View(model);

            var item = _context.Items.FirstOrDefault(i => i.Id == itemId.Value);
            if (item == null)
                return View(model);

            model.ItemName = item.ItemName ?? "";
            model.ItemCode = item.ItemCode ?? "";

            var from = (model.FromDate ?? DateTime.Today).Date;
            var to = (model.ToDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

            // Purchase
            var purchaseRows = _context.Purchases
                .Include(p => p.Items)
                .Where(p => p.PurchaseType == "New" && p.Date >= from && p.Date <= to)
                .SelectMany(p => p.Items
                    .Where(pi => pi.ItemId == itemId.Value)
                    .Select(pi => new ItemLedgerRowVM
                    {
                        Date = p.Date,
                        Type = "Purchase",
                        InvoiceNo = !string.IsNullOrWhiteSpace(p.ReferenceNo) ? p.ReferenceNo : ("PUR-" + p.Id),
                        QtyIn = pi.Quantity,
                        QtyOut = 0,
                        PurchaseRate = pi.UnitCost,
                        SaleRate = 0,
                        Amount = pi.Quantity * pi.UnitCost,
                        Profit = 0
                    }))
                .ToList();

            // Purchase Return
            var purchaseReturnRows = _context.Purchases
                .Include(p => p.Items)
                .Where(p => p.PurchaseType == "Return" && p.Date >= from && p.Date <= to)
                .SelectMany(p => p.Items
                    .Where(pi => pi.ItemId == itemId.Value)
                    .Select(pi => new ItemLedgerRowVM
                    {
                        Date = p.Date,
                        Type = "Purchase Return",
                        InvoiceNo = !string.IsNullOrWhiteSpace(p.ReferenceNo) ? p.ReferenceNo : ("PR-" + p.Id),
                        QtyIn = 0,
                        QtyOut = pi.Quantity,
                        PurchaseRate = pi.UnitCost,
                        SaleRate = 0,
                        Amount = pi.Quantity * pi.UnitCost,
                        Profit = 0
                    }))
                .ToList();

            // Sale
            var saleRows = _context.SaleItems
                .Include(si => si.Sale)
                .Include(si => si.Item)
                .Where(si =>
                    si.ItemId == itemId.Value &&
                    si.Sale.SaleType == "Sale" &&
                    si.Sale.SaleDate >= from &&
                    si.Sale.SaleDate <= to)
                .Select(si => new ItemLedgerRowVM
                {
                    Date = si.Sale.SaleDate,
                    Type = "Sale",
                    InvoiceNo = !string.IsNullOrWhiteSpace(si.Sale.InvoiceNumber) ? si.Sale.InvoiceNumber : ("SAL-" + si.SaleId),
                    QtyIn = 0,
                    QtyOut = si.Quantity,
                    PurchaseRate = si.Item.PurchasePrice,
                    SaleRate = si.UnitPrice,
                    Amount = si.Quantity * si.UnitPrice,
                    Profit = (si.UnitPrice - si.Item.PurchasePrice) * si.Quantity
                })
                .ToList();

            // Sale Return
            var saleReturnRows = _context.SaleItems
                .Include(si => si.Sale)
                .Include(si => si.Item)
                .Where(si =>
                    si.ItemId == itemId.Value &&
                    si.Sale.SaleType == "Return" &&
                    si.Sale.SaleDate >= from &&
                    si.Sale.SaleDate <= to)
                .Select(si => new ItemLedgerRowVM
                {
                    Date = si.Sale.SaleDate,
                    Type = "Sale Return",
                    InvoiceNo = !string.IsNullOrWhiteSpace(si.Sale.InvoiceNumber) ? si.Sale.InvoiceNumber : ("SR-" + si.SaleId),
                    QtyIn = si.Quantity,
                    QtyOut = 0,
                    PurchaseRate = si.Item.PurchasePrice,
                    SaleRate = si.UnitPrice,
                    Amount = si.Quantity * si.UnitPrice,
                    Profit = (si.UnitPrice - si.Item.PurchasePrice) * si.Quantity
                })
                .ToList();

            var allRows = purchaseRows
                .Concat(purchaseReturnRows)
                .Concat(saleRows)
                .Concat(saleReturnRows)
                .OrderBy(r => r.Date)
                .ThenBy(r => r.Type)
                .ToList();

            model.Rows = allRows;

            model.TotalPurchaseQty = purchaseRows.Sum(x => x.QtyIn);
            model.TotalPurchaseReturnQty = purchaseReturnRows.Sum(x => x.QtyOut);
            model.TotalSaleQty = saleRows.Sum(x => x.QtyOut);
            model.TotalSaleReturnQty = saleReturnRows.Sum(x => x.QtyIn);

            model.NetPurchaseQty = model.TotalPurchaseQty - model.TotalPurchaseReturnQty;
            model.NetSaleQty = model.TotalSaleQty - model.TotalSaleReturnQty;

            model.TotalPurchaseAmount = purchaseRows.Sum(x => x.Amount);
            model.TotalPurchaseReturnAmount = purchaseReturnRows.Sum(x => x.Amount);
            model.TotalSaleAmount = saleRows.Sum(x => x.Amount);
            model.TotalSaleReturnAmount = saleReturnRows.Sum(x => x.Amount);

            model.GrossProfit = saleRows.Sum(x => x.Profit);
            model.ReturnLoss = saleReturnRows.Sum(x => x.Profit);
            model.NetProfit = model.GrossProfit - model.ReturnLoss;

            return View(model);
        }
        public async Task<IActionResult> IncomeStatement()
        {
            ViewBag.ExpenseSummary = await _context.Expenses
                .Include(e => e.ExpenseCategory)
                .GroupBy(e => e.ExpenseCategory.CategoryName)
                .Select(g => new
                {
                    CategoryName = g.Key,
                    TotalAmount = g.Sum(x => x.Amount),
                    Count = g.Count()
                })
                .ToListAsync();

            ViewBag.TotalSales = _context.SaleItems
                                .Where(s => s.Sale.SaleType == "Sale")
                                .Sum(s => (decimal?)s.Quantity * s.UnitPrice) ?? 0;
            ViewBag.TotalPurchase = _context.Purchases
                                .Where(p => p.PurchaseType == "New")
                                .Sum(s => s.TotalAmount);
            
            ViewBag.TotalDiscount = _context.Sales
                                .Where(s => s.SaleType == "Sale")
                                .Sum(s => (decimal?)s.Discount) ?? 0;

            ViewBag.Setting = await _context.ShopSettings.OrderByDescending(s => s.Id).FirstOrDefaultAsync() ?? new ShopSetting();


            return View();
        }

    }
}
