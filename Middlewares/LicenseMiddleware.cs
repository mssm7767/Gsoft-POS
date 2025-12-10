using GSoftPosNew.Services;

namespace GSoftPosNew.Middlewares
{
    public class LicenseMiddleware
    {
        private readonly RequestDelegate _next;

        public LicenseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, LicenseService license)
        {
            if (license.IsExpired() && !context.Request.Path.StartsWithSegments("/Home/LicenseExpired"))
            {
                context.Response.Redirect("/Home/LicenseExpired");
                return;
            }

            await _next(context);
        }
    }
}
