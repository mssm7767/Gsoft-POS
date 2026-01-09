// Controllers/UnitController.cs
using GSoftPosNew.Data;
using GSoftPosNew.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace GSoftPosNew.Controllers
{

    public class UnitController : Controller
    {
        private readonly AppDbContext _context;
        public UnitController(AppDbContext context) { _context = context; }

        private void LoadList()
        {
            ViewBag.UnitList = _context.Units
                                       .OrderBy(u => u.Name)
                                       .ToList();
        }

        // CREATE (GET)
        [HttpGet]
        public IActionResult Create()
        {
            LoadList();
            return View(new Unit());
        }

        // CREATE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Unit model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fix the highlighted errors and try again.";
                LoadList();
                return View(model);
            }

            // prevent duplicate (optional)
            if (_context.Units.Any(u => u.Name.ToLower() == model.Name.Trim().ToLower()))
            {
                TempData["Error"] = $"Unit \"{model.Name}\" already exists.";
                LoadList();
                return View(model);
            }

            _context.Units.Add(new Unit
            {
                Name = model.Name.Trim(),
                Detail = string.IsNullOrWhiteSpace(model.Detail) ? null : model.Detail.Trim()
            });
            _context.SaveChanges();

            TempData["Success"] = $"Unit \"{model.Name}\" saved successfully!";
            TempData["LastUnitName"] = model.Name;
            return RedirectToAction(nameof(Create));
        }

        // EDIT (GET)
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var entity = _context.Units.FirstOrDefault(x => x.Id == id);
            if (entity == null)
            {
                TempData["Error"] = "Unit not found.";
                return RedirectToAction(nameof(Create));
            }
            LoadList();
            return View("Create", entity); // same view reuse
        }

        // EDIT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Unit model)
        {
            if (id != model.Id)
            {
                TempData["Error"] = "Invalid request.";
                return RedirectToAction(nameof(Create));
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fix the highlighted errors and try again.";
                LoadList();
                return View("Create", model);
            }

            var entity = _context.Units.FirstOrDefault(x => x.Id == id);
            if (entity == null)
            {
                TempData["Error"] = "Unit not found.";
                return RedirectToAction(nameof(Create));
            }

            // check duplicate name (except self)
            var newName = model.Name.Trim();
            if (_context.Units.Any(u => u.Id != id && u.Name.ToLower() == newName.ToLower()))
            {
                TempData["Error"] = $"Another unit with name \"{newName}\" already exists.";
                LoadList();
                return View("Create", model);
            }

            entity.Name = newName;
            entity.Detail = string.IsNullOrWhiteSpace(model.Detail) ? null : model.Detail.Trim();
            _context.SaveChanges();

            TempData["Success"] = $"Unit \"{entity.Name}\" updated successfully!";
            TempData["LastUnitName"] = entity.Name;
            return RedirectToAction(nameof(Create));
        }

        // DELETE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var entity = _context.Units.FirstOrDefault(x => x.Id == id);
            if (entity == null)
            {
                TempData["Error"] = "Unit not found.";
                return RedirectToAction(nameof(Create));
            }

            _context.Units.Remove(entity);
            _context.SaveChanges();
            TempData["Success"] = $"Unit \"{entity.Name}\" deleted.";
            return RedirectToAction(nameof(Create));
        }
    }
}
