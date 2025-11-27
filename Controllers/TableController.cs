using GSoftPosNew.Data;
using GSoftPosNew.Models;
using GSoftPosNew.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GSoftPosNew.Controllers
{
    public class TableController : Controller
    {
        private readonly AppDbContext _context;

        public TableController(AppDbContext context)
        {
            _context = context;
        }

        // 🔹 Dropdown ke liye locations
        private async Task<List<SelectListItem>> GetLocationListAsync()
        {
            return await _context.Locations
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.Name
                })
                .ToListAsync();
        }

        // 🔹 Neeche list ke liye tables
        private async Task<List<PosTableListItem>> GetExistingTablesAsync()
        {
            return await _context.PosTables
                .Include(t => t.Location)
                .OrderBy(t => t.Location.Name)
                .ThenBy(t => t.TableName)
                .Select(t => new PosTableListItem
                {
                    Id = t.Id,
                    LocationName = t.Location.Name,
                    TableName = t.TableName,
                    TableCode = t.TableCode,
                    Capacity = t.Capacity,
                    IsActive = t.IsActive
                })
                .ToListAsync();
        }

        // 🔹 GET: /Table/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new PosTableViewModel
            {
                LocationList = await GetLocationListAsync(),
                ExistingTables = await GetExistingTablesAsync()
            };

            return View(vm);
        }

        // 🔹 POST: /Table/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PosTableViewModel vm)
        {
            // ✅ Debug: agar validation fail ho to pata chale
            if (!ModelState.IsValid)
            {
                var errors = string.Join(" | ",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));

                vm.LocationList = await GetLocationListAsync();
                vm.ExistingTables = await GetExistingTablesAsync();

                ViewBag.ValidationErrors = errors;
                return View(vm);
            }

            var entity = new PosTable
            {
                LocationId = vm.LocationId!.Value,
                TableName = vm.TableName,
                TableCode = vm.TableCode,
                Capacity = vm.Capacity,
                IsActive = vm.IsActive,
                Notes = vm.Notes
            };

            _context.PosTables.Add(entity);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Table saved successfully (Id: " + entity.Id + ")";

            // redirect se:
            // 1) form reset
            // 2) GET Create dobara chalega => nayi list + toast
            return RedirectToAction(nameof(Create));
        }

        // 🔴 TEST ACTION: direct DB test, form se alag
        // URL: /Table/TestInsert
        public async Task<IActionResult> TestInsert()
        {
            // koi ek location lazmi chahiye
            var anyLocation = await _context.Locations.FirstOrDefaultAsync();
            if (anyLocation == null)
            {
                return Content("No Location found. Pehle Add Location screen se kam az kam 1 location banao.");
            }

            var t = new PosTable
            {
                LocationId = anyLocation.Id,
                TableName = "TEST-" + DateTime.Now.ToString("HHmmss"),
                TableCode = "TST",
                Capacity = 4,
                IsActive = true,
                Notes = "Inserted from TestInsert action"
            };

            _context.PosTables.Add(t);
            await _context.SaveChangesAsync();

            return Content($"Test table saved in DB gsoft.dbo.PosTables with Id = {t.Id}, Location = {anyLocation.Name}");
        }
    }
}
