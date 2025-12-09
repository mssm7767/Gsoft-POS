using GSoftPosNew.Data;
using GSoftPosNew.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace GSoftPosNew.Controllers
{
    [Authorize]
    public class ExpenseCategoryController : Controller
    {
        private readonly AppDbContext _context;

        public ExpenseCategoryController(AppDbContext context)
        {
            _context = context;
        }
        // GET: /ExpenseCategory
        public IActionResult Index()
        {
            var categories = _context.ExpenseCategories
                .OrderBy(c => c.CategoryName)
                .ToList();

            return View(categories);
        }

        // GET: /ExpenseCategory/Create
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Categories = _context.ExpenseCategories
                .OrderBy(c => c.CategoryName)
                .ToList();
            return View(new ExpenseCategory());
        }

        // POST: /ExpenseCategory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ExpenseCategory model)
        {

            _context.ExpenseCategories.Add(model);
            _context.SaveChanges();
            TempData["Message"] = "Expense category saved successfully!";
            return RedirectToAction(nameof(Create));
        }

        // GET: /ExpenseCategory/Edit/{id}
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var cat = _context.ExpenseCategories.FirstOrDefault(c => c.Id == id);
            if (cat == null)
                return NotFound();

            return View(cat);
        }

        // POST: /ExpenseCategory/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ExpenseCategory model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existing = _context.ExpenseCategories.FirstOrDefault(c => c.Id == model.Id);
            if (existing == null)
                return NotFound();

            existing.CategoryName = model.CategoryName;
            existing.Description = model.Description;

            _context.SaveChanges();
            TempData["Message"] = "Expense category updated successfully!";
            return RedirectToAction(nameof(Create));
        }

        // GET: /ExpenseCategory/Delete/{id}
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var cat = _context.ExpenseCategories.FirstOrDefault(c => c.Id == id);
            if (cat == null)
                return NotFound();

            _context.ExpenseCategories.Remove(cat);
            _context.SaveChanges();
            TempData["Message"] = "Expense category deleted successfully!";
            return RedirectToAction(nameof(Create));
        }
    }
}
