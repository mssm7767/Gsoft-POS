using GSoftPosNew.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GSoftPosNew.Middlewares
{
    public class UserRolePermissionMiddleware
    {
        private readonly RequestDelegate _next;

        public UserRolePermissionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext db)
        {
            // Only process for authenticated users
            if (context.User.Identity?.IsAuthenticated ?? false)
            {
                var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;

                if (!string.IsNullOrEmpty(userRole))
                {
                    var role = await db.Roles.FirstOrDefaultAsync(r => r.RoleName == userRole);

                    if (role != null)
                    {
                        // Get all permissions for the role
                        var permissions = await db.RolePermissions
                            .Where(p => p.RoleId == role.Id)
                            .Select(p => new { p.ModuleName, p.IsAllowed })
                            .ToListAsync();

                        // Convert to dictionary: { "ModuleName" => true/false }
                        var permissionDict = permissions
                            .ToDictionary(p => p.ModuleName, p => p.IsAllowed);

                        // Store for global use
                        context.Items["UserRole"] = role.RoleName;
                        context.Items["UserPermissions"] = permissionDict;
                    }
                }
            }

            await _next(context);
        }
    }
}
