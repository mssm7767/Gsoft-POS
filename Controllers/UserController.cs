using GSoftPosNew.Data;
using GSoftPosNew.Models;
using GSoftPosNew.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;

namespace GSoftPosNew.Controllers
{

    public class UserController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public UserController(AppDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public IActionResult Index()
        {
            var UserList = _context.Users.OrderBy(u => u.FullName).ToList();
            return View(UserList);
        }

        // GET: User/Create
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.UserList = _context.Users.OrderBy(u => u.FullName).ToList();
            ViewBag.Roles = _context.Roles.ToList();
            return View(new User());
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(User model, IFormFile? ProfilePic)
        {
            // Repopulate dropdowns or lists
            ViewBag.UserList = _context.Users.OrderBy(u => u.FullName).ToList();

         

            // Handle Profile Picture upload
            if (ProfilePic != null && ProfilePic.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/users");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(ProfilePic.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    ProfilePic.CopyTo(stream);
                }

                model.ProfilePicUrl = $"/uploads/users/{uniqueFileName}";
            }

            // Default values for DB-required fields
         
            model.EmailConfirmed = false;
            model.LastLogin ??= DateTime.MinValue; // matches '0001-01-01...' default
            model.Role ??= string.Empty;
            model.PasswordHash ??= string.Empty;

            // Hash password
            model.PasswordHash = _passwordHasher.HashPassword(model.Password);

            // Never save plain password
            model.Password = null;

            _context.Users.Add(model);
            _context.SaveChanges();

            TempData["Success"] = "User saved successfully!";
            return RedirectToAction(nameof(Create));
        }


        // GET: User/Edit/5
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return NotFound();

            ViewBag.UserList = _context.Users.OrderBy(u => u.FullName).ToList();

            return View("Create", user);
        }

        // GET: User/Delete/5
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
                TempData["Success"] = "User deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
