using GSoftPosNew.Data;
using GSoftPosNew.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace GSoftPosNew.Controllers
{
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;
        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Category/Create
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.CategoryList = _context.Categories
                                           .OrderBy(c => c.Name)
                                           .ToList();
            return View();
        }

        // POST: /Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category model)
        {
            _context.Categories.Add(model);
            _context.SaveChanges();
            TempData["Success"] = "Category added!";
            return RedirectToAction(nameof(Create));
        }

        // GET: Category/Edit/5
        [HttpGet]
        public IActionResult Edit(int id)
        {
            ViewBag.CategoryList = _context.Categories
                                           .OrderBy(c => c.Name)
                                           .ToList();

            if (id == null) // new category
                return View(new Category());

            // load existing category for editing
            var category = _context.Categories.FirstOrDefault(c => c.Id == id);
            if (category == null)
                return NotFound();

            return View(category);
        }

        // POST: /Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CategoryList = _context.Categories
                                               .OrderBy(c => c.Name)
                                               .ToList();
                return View(model);
            }

            if (model.Id > 0) // update
            {
                var existingCategory = _context.Categories.FirstOrDefault(c => c.Id == model.Id);
                if (existingCategory == null)
                    return NotFound();

                existingCategory.Name = model.Name;
                existingCategory.Description = model.Description;
                existingCategory.Discount = model.Discount;

                _context.Categories.Update(existingCategory);
                TempData["Success"] = "Category updated!";
            }
            else // insert
            {
                _context.Categories.Add(model);
                TempData["Success"] = "Category added!";
            }

            _context.SaveChanges();
            return RedirectToAction(nameof(Create));
        }


        public IActionResult Delete (int id)
        {
            var modelTodelete = _context.Categories.Find(id);
            if(modelTodelete != null)
            {
                _context.Categories.Remove(modelTodelete);
                _context.SaveChanges();

                TempData["Danger"] = "Category deleted!";
                return RedirectToAction(nameof(Create));
            }

            return RedirectToAction(nameof(Create));
        }
    }
}

