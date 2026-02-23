using GSoftPosNew.Data;
using GSoftPosNew.Models;
using GSoftPosNew.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSoftPosNew.Controllers
{

    public class SettingsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly DatabaseResetService _dbResetService;


        public SettingsController(AppDbContext context, IWebHostEnvironment environment, DatabaseResetService dbResetService)
        {
            _context = context;
            _environment = environment;
            _dbResetService = dbResetService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var settings = await _context.ShopSettings.OrderByDescending(s => s.Id).FirstOrDefaultAsync() ?? new ShopSetting();

            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ShopSetting model)
        {
            var existing = await _context.ShopSettings.FindAsync(model.Id);
            

            if (existing == null)
            {
                

                // Handle Logo Upload
                if (model.LogoFile != null && model.LogoFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.LogoFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        model.LogoFile.CopyTo(stream);
                    }

                    model.LogoPath = "/uploads/" + uniqueFileName;
                }

                

                model.ShopName = model.ShopName.ToUpper();
                _context.ShopSettings.Update(model);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Settings updated successfully!";
                return RedirectToAction("Index");
            }
            else
            {
                model.LogoPath = existing.LogoPath;
                // Handle Logo Upload
                if (model.LogoFile != null && model.LogoFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.LogoFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        model.LogoFile.CopyTo(stream);
                    }

                    model.LogoPath = "/uploads/" + uniqueFileName;
                }

                existing.LogoPath = model.LogoPath;
                existing.ShopName = model.ShopName.ToUpper();
                existing.Email = model.Email;
                existing.Contact1 = model.Contact1;
                existing.Contact2 = model.Contact2;
                existing.Message = model.Message;
                existing.Address = model.Address;
                existing.NTN = model.NTN;
                existing.STRN = model.STRN;
                existing.POSId = model.POSId;
                existing.ServiceCharges = model.ServiceCharges;
                existing.SaleLowStock = model.SaleLowStock;

                _context.ShopSettings.Update(existing);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Settings updated successfully!";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        public async Task<IActionResult> DatabaseBackup()
        {
            // Folder where the backup will be saved
            string backupFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "backup");
            if (!Directory.Exists(backupFolder))
            {
                Directory.CreateDirectory(backupFolder);
            }

            // File name with today's date and time
            string fileName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
            string filePath = Path.Combine(backupFolder, fileName);

            // Get the database name from the DbContext
            string databaseName = _context.Database.GetDbConnection().Database;

            // Build SQL command
            string sql = $@"
                BACKUP DATABASE [{databaseName}]
                TO DISK = N'{filePath}'
                WITH FORMAT, INIT,  
                NAME = N'{databaseName}-Full Database Backup';";

            // Execute the backup command
            await _context.Database.ExecuteSqlRawAsync(sql);

            TempData["Message"] = "✅ Database Back-up successfully!";

            // Optionally: return file for download, or just redirect
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> ResetDb()
        {
            await _dbResetService.ResetDatabaseExceptUsersAsync();
            TempData["Message"] = "✅ Database has been reset (Users table preserved).";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> ResetItemQuantity()
        {
            // Example: Reset all items' quantities to 0
            var items = await _context.Items.ToListAsync();

            foreach (var item in items)
            {
                item.Quantity = 0;
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = "All item quantities have been reset to Zero.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> ResetTransactions()
        {
            // 1. Delete all Sales
            _context.Sales.RemoveRange(_context.Sales);

            // 2. Delete all Purchases
            _context.Purchases.RemoveRange(_context.Purchases);

            // 3. Delete all SupplierPayments
            _context.SupplierPayments.RemoveRange(_context.SupplierPayments);

            // 4. Delete all CustomerPayments
            _context.CustomerPayments.RemoveRange(_context.CustomerPayments);

            // 5. Reset OpeningBalance of Customers
            var customers = await _context.Customers.ToListAsync();
            foreach (var customer in customers)
            {
                customer.OpeningBalance = 0;
            }

            // 6. Reset OpeningBalance of Suppliers
            var suppliers = await _context.Suppliers.ToListAsync();
            foreach (var supplier in suppliers)
            {
                supplier.OpeningBalance = 0;
            }

            // Save all changes
            await _context.SaveChangesAsync();

            TempData["Message"] = "All transactions and balances have been reset successfully.";
            return RedirectToAction("Index");
        }
    }
}
