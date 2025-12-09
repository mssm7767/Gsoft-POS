using GSoftPosNew.Data;
using GSoftPosNew.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace GSoftPosNew.Controllers
{
    [Authorize]
    public class ExpenseController : Controller
    {
        private readonly AppDbContext _context;

        public ExpenseController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Expense
        public IActionResult Index(int? categoryId, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            const int PageSize = 15;

            var expenses = _context.Expenses.Include(e => e.ExpenseCategory).OrderByDescending(e => e.ExpenseDate).AsQueryable();

            if (categoryId.HasValue)
            {
                expenses = expenses.Where(e => e.ExpenseCategoryId == categoryId.Value);
                ViewBag.SelectedCategoryId = categoryId;
            }

            // Filter by date range
            if (fromDate.HasValue)
            {
                expenses = expenses.Where(e => e.ExpenseDate >= fromDate.Value);
                ViewBag.FromDate = fromDate;
            }

            if (toDate.HasValue)
            {
                expenses = expenses.Where(e => e.ExpenseDate <= toDate.Value);
                ViewBag.ToDate = toDate;
            }

            // Pagination logic
            var totalRecords = expenses.Count();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)PageSize);
            var pagedExpenses = expenses
                .OrderByDescending(e => e.ExpenseDate)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // ViewBags for pagination
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            ViewBag.Categories = _context.ExpenseCategories.ToList();

            ViewBag.TotalAmount = expenses.Sum(e => e.Amount);
            return View(pagedExpenses);
        }

        // GET: /Expense/Create
        public IActionResult Create()
        {
            ViewBag.Categories = _context.ExpenseCategories
                .OrderBy(c => c.CategoryName)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.CategoryName
                })
                .ToList();

            return View(new Expense { ExpenseDate = DateTime.Today });
        }

        // POST: /Expense/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Expense model)
        {

            _context.Expenses.Add(model);
            _context.SaveChanges();

            TempData["Message"] = "Expense saved successfully!";
            return RedirectToAction(nameof(Index));
        }
        
        // GET: /Expense/Edit
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Categories = await _context.ExpenseCategories
                .OrderBy(c => c.CategoryName)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.CategoryName
                })
                .ToListAsync();

            var model = await _context.Expenses.FindAsync(id);

            return View(model);
        }

        // POST: /Expense/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Expense model)
        {
            var existing = await _context.Expenses.FindAsync(model.Id);

            if(existing != null)
            {
                existing.Amount = model.Amount;
                existing.ReferenceNumber = model.ReferenceNumber;
                existing.ExpenseFor = model.ExpenseFor;
                existing.ExpenseDate = model.ExpenseDate;
                existing.ExpenseCategoryId = model.ExpenseCategoryId;
                existing.Note = model.Note;

                _context.Expenses.Update(existing);
                _context.SaveChanges();
                TempData["Message"] = "Expense updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            

            
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(int id)
        {
            var cat = _context.Expenses.FirstOrDefault(c => c.Id == id);
            if (cat == null)
                return NotFound();

            _context.Expenses.Remove(cat);
            _context.SaveChanges();
            TempData["Message"] = "Expense deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
