using GSoftPosNew.Data;
using GSoftPosNew.Models;
using GSoftPosNew.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;

namespace GSoftPosNew.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

    
            public IActionResult Index()
            {
                // ---------------- Totals ----------------
                var totalItems = _context.Items.Count();
                var totalStockValue = _context.Items.Sum(i => (decimal?)i.Quantity * i.PurchasePrice) ?? 0;

                var totalPurchases = _context.Purchases
                                    .Where(p => p.PurchaseType == "New")
                                    .Sum(p => (decimal?)p.TotalAmount) ?? 0;

                // Total sales revenue
                var totalSales = _context.SaleItems
                                .Where(s => s.Sale.SaleType == "Sale")
                                .Sum(s => (decimal?)s.Quantity * s.UnitPrice) ?? 0;


                // Total cost of sales (join with Items to get PurchasePrice)
                var totalCostOfSales = (from si in _context.SaleItems
                                        .Where(s => s.Sale.SaleType == "Sale")
                                        join i in _context.Items on si.ItemId equals i.Id
                                            select (decimal?)si.Quantity * i.PurchasePrice).Sum() ?? 0;

                // Profit
                var totalProfit = totalSales - totalCostOfSales;

                // Revenues or Payments
                var totalRevenuesOrPayments = _context.Payments
                            .Where(s => s.Sale.SaleType == "Sale")
                            .Sum(pay => (decimal?)pay.Amount) ?? 0;

                var totalLowStockItems = _context.Items.Count(i => i.Quantity < 5);
                var totalExpiryItems = _context.Items.Count(i => i.ExpiryDate <= DateTime.Today);

                var itemIds = _context.SaleItems
                        .Where(s => s.Sale.SaleType == "Sale")
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

            // Monthly Sales (group by month)
            var monthlySales = _context.SaleItems
                    .Where(s => s.Sale.SaleType == "Sale")
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

                var salesData = monthlySales
                    .Select(x => x.Total)
                    .ToList();

                // Top 5 products
                var topProducts = _context.SaleItems
                    .Where(s => s.Sale.SaleType == "Sale")
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
                    TopProductSales = topProductSales
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
