using GSoftPosNew.Data;
using GSoftPosNew.Models;
using Microsoft.AspNetCore.Mvc;

namespace GSoftPosNew.Controllers
{
    public class UserLicenseController : Controller
    {
        private readonly AppDbContext _context;

        public UserLicenseController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var users = _context.Users.ToList();
            return View(users);
        }

        public IActionResult Edit(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
                return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, User user)
        {
            if (id != user.Id)
                return NotFound();

            //if (ModelState.IsValid)
            //{
                var existingUser = _context.Users.Find(id);
                if (existingUser == null) return NotFound();

                existingUser.LicenseExpiryDate = user.LicenseExpiryDate;
                _context.SaveChanges();

                return RedirectToAction(nameof(Index));
            //}

            //return View(user);
        }
    }

}
