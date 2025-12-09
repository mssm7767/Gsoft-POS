using GSoftPosNew.Data;
using GSoftPosNew.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GSoftPosNew.Controllers
{
    [Authorize]
    public class LocationController : Controller
    {
        private readonly AppDbContext _context;

        public LocationController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ Create - GET
        public IActionResult Create()
        {
            ViewBag.Locations = _context.Locations.ToList();
            return View(new Location());
        }

        // ✅ Create - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Location location)
        {

                _context.Locations.Add(location);
                _context.SaveChanges();
                TempData["Message"] = "Location saved successfully.";

                ViewBag.Locations = _context.Locations.ToList();
                return View(new Location());


            //// اگر Validation Error ہو
            //ViewBag.Locations = _context.Locations.ToList();
            //return View(location);
        }

        // ✅ Edit - GET
        public IActionResult Edit(int id)
        {
            var loc = _context.Locations.FirstOrDefault(l => l.Id == id);
            if (loc == null)
                return NotFound();

            return View(loc);
        }

        // ✅ Edit - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Location location)
        {
            if (ModelState.IsValid)
            {
                _context.Locations.Update(location);
                _context.SaveChanges();
                TempData["Message"] = "Location updated successfully.";
                return RedirectToAction("Create");
            }

            return View(location);
        }

        // ✅ Delete
        public IActionResult Delete(int id)
        {
            var loc = _context.Locations.FirstOrDefault(l => l.Id == id);
            if (loc == null)
                return NotFound();

            _context.Locations.Remove(loc);
            _context.SaveChanges();
            TempData["Message"] = "Location deleted successfully.";
            return RedirectToAction("Create");
        }
    }
}
