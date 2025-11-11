using GSoftPosNew.Data;
using GSoftPosNew.Models;
using GSoftPosNew.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GSoftPosNew.Controllers
{
    public class RoleController : Controller
    {
        private readonly AppDbContext _context;

        public RoleController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ List All Roles
        public IActionResult Index()
        {
            var roles = _context.Roles.ToList();
            return View(roles);
        }

        // ✅ Create - GET
        public IActionResult Create()
        {
            return View();
        }

        // ✅ Create - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Role role)
        {
            if (ModelState.IsValid)
            {
                _context.Roles.Add(role);
                _context.SaveChanges();
                TempData["Message"] = "Role created successfully.";
                return RedirectToAction("Index");
            }
            return View(role);
        }

        // ✅ Assign Permissions - GET
        [HttpGet]
        public IActionResult AssignPermissions()
        {
            var model = new AssignPermissionsViewModel
            {
                RoleList = _context.Roles
                    .Select(r => new SelectListItem
                    {
                        Value = r.Id.ToString(),
                        Text = r.RoleName
                    }).ToList(),
                Modules = new List<string>
        {
            "User",
            "Sales",
            "Purchase",
            "Reports",
            "Customers",
            "Suppliers",
            "Items",
            "Categories",
            "Settings",
            "Accounts",
            "SMS"
        },
                SelectedModules = new List<string>()
            };

            return View(model);
        }

        // ✅ Assign Permissions - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SavePermissions(int RoleId, List<string> SelectedModules)
        {
            // Remove old permissions
            var oldPermissions = _context.RolePermissions.Where(p => p.RoleId == RoleId);
            _context.RolePermissions.RemoveRange(oldPermissions);
            _context.SaveChanges();

            // Add new permissions
            foreach (var module in SelectedModules)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = RoleId,
                    ModuleName = module,
                    IsAllowed = true
                });
            }
            _context.SaveChanges();

            TempData["Message"] = "Permissions assigned successfully.";
            return RedirectToAction("AssignPermissions");
        }


        // ✅ Edit - GET
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var role = _context.Roles.FirstOrDefault(r => r.Id == id);
            if (role == null)
                return NotFound();

            return View(role);
        }

        // ✅ Edit - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Role role)
        {
            if (ModelState.IsValid)
            {
                _context.Roles.Update(role);
                _context.SaveChanges();
                TempData["Message"] = "Role updated successfully.";
                return RedirectToAction("Index");
            }
            return View(role);
        }

        // ✅ Delete
        public IActionResult Delete(int id)
        {
            var role = _context.Roles.FirstOrDefault(r => r.Id == id);
            if (role == null)
                return NotFound();

            _context.Roles.Remove(role);
            _context.SaveChanges();
            TempData["Message"] = "Role deleted successfully.";
            return RedirectToAction("Index");
        }
    }
}
