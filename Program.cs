using GSoftPosNew.data;
using GSoftPosNew.Data;
using GSoftPosNew.Middlewares;
using GSoftPosNew.Repositories;
using GSoftPosNew.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Logging.AddConsole();

// Register your DbContext with the connection string from appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<DatabaseResetService>();
builder.Services.AddScoped<LicenseService>();


// Identity Password Hasher Registration
builder.Services.AddScoped<IPasswordHasher<GSoftPosNew.Models.User>, PasswordHasher<GSoftPosNew.Models.User>>();



// Configure session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;
});


// Add services to the container.
builder.Services.AddControllersWithViews();

// ✅ Add cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";         // Redirect if not logged in
        options.AccessDeniedPath = "/Account/Denied"; // Redirect if unauthorized
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
    });




var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        SeedData.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

// *** Middleware order, NO mistake: ***
app.UseStaticFiles();
app.UseRouting();

app.UseMiddleware<LicenseMiddleware>();

app.UseSession();               // Session (if you are using it)
app.UseAuthentication();        // <-- Always BEFORE UseAuthorization
app.UseMiddleware<UserRolePermissionMiddleware>();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();