using GSoftPosNew.Data;
using GSoftPosNew.Models;
using GSoftPosNew.Services;
using GSoftPosNew.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Globalization;

namespace GSoftPosNew.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        private readonly LicenseService _license;

        public HomeController(ILogger<HomeController> logger, AppDbContext context, LicenseService license)
        {
            _logger = logger;
            _context = context;
            _license = license;

        }

        public IActionResult LicenseExpired()
        {
            return View();
        }

        public IActionResult Index(DateTime? fromDate, DateTime? toDate)
        {
            var expiry = _license.GetExpiryDate();
            var daysLeft = (expiry - DateTime.Today).Days;
            ViewBag.DaysLeft = daysLeft;


            // Default to today's date range if not provided
            if (!fromDate.HasValue && !toDate.HasValue)
            {
                fromDate = DateTime.Today;
                toDate = DateTime.Today;
            }
            else
            {
                // Ensure proper range (if user selects only one side)
                fromDate ??= DateTime.MinValue;
                toDate ??= DateTime.MaxValue;
            }

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");

            // ---------------- Totals ----------------
            var totalItems = _context.Items.Count();
            var totalStockValue = _context.Items.Sum(i => (decimal?)i.Quantity * i.PurchasePrice) ?? 0;

            var totalPurchases = _context.Purchases
                .Where(p => p.PurchaseType == "New" && p.Date.Date >= fromDate && p.Date.Date <= toDate)
                .Sum(p => (decimal?)p.TotalAmount) ?? 0;

            var totalSales = _context.SaleItems
                .Where(s => s.Sale.SaleType == "Sale" && s.Sale.SaleDate.Date >= fromDate && s.Sale.SaleDate.Date <= toDate)
                .Sum(s => (decimal?)s.Quantity * s.UnitPrice) ?? 0;

            var totalCostOfSales = (from si in _context.SaleItems
                                    where si.Sale.SaleType == "Sale" && si.Sale.SaleDate.Date >= fromDate && si.Sale.SaleDate.Date <= toDate
                                    join i in _context.Items on si.ItemId equals i.Id
                                    select (decimal?)si.Quantity * i.PurchasePrice).Sum() ?? 0;

            var totalProfit = totalSales - totalCostOfSales;

            var totalRevenuesOrPayments = _context.Payments
                .Where(p => p.Sale.SaleType == "Sale" && p.PaymentDate >= fromDate && p.PaymentDate <= toDate)
                .Sum(p => (decimal?)p.Amount) ?? 0;

            var totalLowStockItems = _context.Items.Count(i => i.Quantity < 5);
            var totalExpiryItems = _context.Items.Count(i => i.ExpiryDate <= DateTime.Today);

            var itemIds = _context.SaleItems
                .Where(s => s.Sale.SaleType == "Sale" && s.Sale.SaleDate.Date >= fromDate && s.Sale.SaleDate.Date <= toDate)
                .OrderByDescending(s => s.Id)
                .Select(s => s.ItemId)
                .ToList();

            var itemsList = _context.Items
                .Where(i => itemIds.Contains(i.Id))
                .OrderByDescending(i => i.Id)
                .Select(i => new ItemModel
                {
                    Id = i.Id,
                    ItemCode = i.ItemCode,
                    ItemName = i.ItemName,
                    Quantity = i.Quantity,
                    PurchasePrice = i.PurchasePrice,
                    SalePrice = i.SalePrice
                })
                .ToList();

            // ---------------- Charts ----------------
            var monthlySales = _context.SaleItems
                .Where(s => s.Sale.SaleType == "Sale" && s.Sale.SaleDate.Date >= fromDate && s.Sale.SaleDate.Date <= toDate)
                .GroupBy(s => s.Sale.SaleDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Total = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .ToList();

            var salesMonths = monthlySales
                .Select(x => CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(x.Month))
                .ToList();

            var salesData = monthlySales.Select(x => x.Total).ToList();

            // Top 5 products
            var topProducts = _context.SaleItems
                .Where(s => s.Sale.SaleType == "Sale" && s.Sale.SaleDate.Date >= fromDate && s.Sale.SaleDate.Date <= toDate)
                .GroupBy(s => s.Item.ItemName)
                .Select(g => new { Product = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Quantity)
                .Take(5)
                .ToList();

            var topProductNames = topProducts.Select(x => x.Product).ToList();
            var topProductSales = topProducts.Select(x => x.Quantity).ToList();

            // ---------------- Model ----------------
            var model = new DashboardViewModel
            {
                TotalItems = totalItems,
                TotalPurchases = totalPurchases,
                TotalStockValue = totalStockValue,
                TotalSales = totalSales,
                TotalProfit = totalProfit,
                TotalRevenuesOrPayments = totalRevenuesOrPayments,
                TotalLowStockItems = totalLowStockItems,
                TotalExpiryItems = totalExpiryItems,
                ItemsList = itemsList,
                SalesMonths = salesMonths,
                SalesData = salesData,
                TopProducts = topProductNames,
                TopProductSales = topProductSales   // ✅ ab types match
            };

            return View(model);
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
