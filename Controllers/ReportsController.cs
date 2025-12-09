using GSoftPosNew.Data;
using GSoftPosNew.Models;
using GSoftPosNew.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace GSoftPosNew.Controllers
{
    [Authorize]
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
            
            ViewBag.TotalDiscount = _context.Sales
                                .Where(s => s.SaleType == "Sale")
                                .Sum(s => (decimal?)s.Discount) ?? 0;

            ViewBag.Setting = await _context.ShopSettings.OrderByDescending(s => s.Id).FirstOrDefaultAsync() ?? new ShopSetting();


            return View();
        }

    }
}
