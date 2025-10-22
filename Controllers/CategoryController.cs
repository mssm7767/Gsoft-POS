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
        public async Task<IActionResult> Create(Category model, IFormFile? ImageFile)
        {
            if (ImageFile != null && ImageFile.Length > 0)
            {
                // Generate unique file name
                var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);

                // Path to save in wwwroot/images
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                // Save relative path to DB (so Razor can render it with Url.Content)
                model.ImagePath = "/images/" + fileName;
            }

            _context.Categories.Add(model);
            await _context.SaveChangesAsync();

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
        public async Task<IActionResult> Edit(Category model, IFormFile? ImageFile)
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

                // Handle image upload
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    // Optional: delete old image file if exists
                    if (!string.IsNullOrEmpty(existingCategory.ImagePath))
                    {
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingCategory.ImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Save new image
                    var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    existingCategory.ImagePath = "/images/" + fileName;
                }

                _context.Categories.Update(existingCategory);
                TempData["Success"] = "Category updated!";
            }
            else // insert
            {
                // Handle image upload for new category
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    model.ImagePath = "/images/" + fileName;
                }

                _context.Categories.Add(model);
                TempData["Success"] = "Category added!";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Create));
        }



        public IActionResult Delete(int id)
        {
            var modelToDelete = _context.Categories.Find(id);
            if (modelToDelete != null)
            {
                // Delete the image file if it exists
                if (!string.IsNullOrEmpty(modelToDelete.ImagePath))
                {
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", modelToDelete.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.Categories.Remove(modelToDelete);
                _context.SaveChanges();

                TempData["Danger"] = "Category deleted!";
            }

            return RedirectToAction(nameof(Create));
        }

    }
}

