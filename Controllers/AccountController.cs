using GSoftPosNew.Data;
using GSoftPosNew.Helpers;
using GSoftPosNew.Models;
using GSoftPosNew.Repositories;
using GSoftPosNew.Services;
using GSoftPosNew.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace GSoftPosNew.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        public AccountController(IUserService userService, AppDbContext context, IPasswordHasher passwordHasher, IConfiguration config)
        {
            _userService = userService;
            _context = context;
            _passwordHasher = passwordHasher;
            _config = config;
        }

        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            DateTime expiryDate = Convert.ToDateTime(_config["AppSettings:LicenseDate"]);

            if (LicenseHelper.IsExpired(expiryDate))
            {
                ViewBag.Error = "Your POS license has expired. Please contact support.";
                return View(); // stop login
            }

            // Find user by username
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username);

            if (user != null)
            {
                // Hash the entered password
                var result = _passwordHasher.VerifyPassword(model.Password, user.PasswordHash);

                if (result)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.FullName ?? string.Empty),
                        new Claim(ClaimTypes.Role, user.Role),
                    };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true, // remember across sessions
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2) // optional expiry
                    };

                    // ✅ Sign in and set cookie
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // ✅ Login success → redirect to Home
                    return RedirectToAction("Index", "Home");
                }
            }

            // If we reach here → login failed
            ModelState.AddModelError(string.Empty, "Invalid username or password");
            return View(model);
        }

        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2")); // lowercase hex
                }
                return builder.ToString();
            }
        }
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
                // Check if username is available
                if (await _userService.UsernameExistsAsync(model.Username))
                {
                    ModelState.AddModelError("Username", "Username is already taken");
                    return View(model);
                }

                // Check if email is available
                if (await _userService.EmailExistsAsync(model.Email))
                {
                    ModelState.AddModelError("Email", "Email is already registered");
                    return View(model);
                }

                var user = new User
                {
                    Username = model.Username,
                    FullName = model.FullName,
                    Email = model.Email,
                    Role = model.Role,
                    IsActive = true,
                    PasswordHash = _passwordHasher.HashPassword(model.Password)
                };

                try
                {
                    await _userService.CreateUserAsync(user, model.Password);

                    // For new users, automatically log them in
                    var result = await _userService.AuthenticateAsync(model.Username, model.Password);
                    if (result != null)
                    {
                        var claims = new[]
                        {
                    new Claim(ClaimTypes.NameIdentifier, result.Id.ToString()),
                    new Claim(ClaimTypes.Name, result.Username),
                    new Claim(ClaimTypes.GivenName, result.FullName),
                    //new Claim(ClaimTypes.Email, result.Email),
                    new Claim(ClaimTypes.Role, result.Role)
                };

                        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var principal = new ClaimsPrincipal(identity);

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            principal);

                        return RedirectToAction("Index", "Home");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while creating your account: " + ex.Message);
                }
            return View(model);
        }
        
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                return BadRequest("Invalid password reset link.");
            }

            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };

            return View(model);
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Optional: Validate token — only if you stored it
            // var tokenRecord = await _context.PasswordResetTokens
            //     .FirstOrDefaultAsync(t => t.Email == model.Email && t.Token == model.Token && t.ExpiryDate > DateTime.UtcNow);

            // if (tokenRecord == null)
            // {
            //     ModelState.AddModelError(string.Empty, "Invalid or expired password reset token.");
            //     return View(model);
            // }

            // Find the user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                // Don't reveal that the user doesn't exist
                TempData["ResetSuccess"] = "Your password has been reset.";
                return RedirectToAction("Login");
            }

            // Update password (make sure you hash it!)
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword); // ✅ Example with BCrypt

            _context.Update(user);
            await _context.SaveChangesAsync();

            // Optional: Remove used token
            // _context.PasswordResetTokens.Remove(tokenRecord);
            // await _context.SaveChangesAsync();

            TempData["ResetSuccess"] = "Your password has been reset. You can now log in.";
            return RedirectToAction("Login");
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user != null)
                {
                    // Generate a fake token (real scenario: store in DB and expire it)
                    var token = Guid.NewGuid().ToString();

                    // Build reset URL
                    var resetUrl = Url.Action("ResetPassword", "Account", new
                    {
                        email = user.Email,
                        token = token
                    }, protocol: Request.Scheme);

                    // Simulate sending email (you can replace with real IEmailSender)
                    ViewBag.Message = "If your email exists in our system, you'll receive a password reset link.";
                    ViewBag.ResetLink = resetUrl; // 🔒 For demo/dev only, remove in production

                    // TODO: Save token and expiration to a DB table like PasswordResetTokens (optional)

                    return View();
                }

                // Always show same message to prevent email enumeration
                ViewBag.Message = "If your email exists in our system, you'll receive a password reset link.";
                return View();
            }

            return View(model);
        }
    }
}
