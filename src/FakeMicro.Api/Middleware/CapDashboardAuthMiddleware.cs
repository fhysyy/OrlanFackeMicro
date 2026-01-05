using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace FakeMicro.Api.Middleware
{
    public class CapDashboardAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _dashboardPath;

        public CapDashboardAuthMiddleware(RequestDelegate next, string dashboardPath)
        {
            _next = next;
            _dashboardPath = dashboardPath;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;

            if (!string.IsNullOrEmpty(path) && path.StartsWith(_dashboardPath))
            {
                if (!Security.DatePasswordAuthHelper.AuthenticateByCookie(context) &&
                    !Security.DatePasswordAuthHelper.AuthenticateRequest(context) &&
                    !Security.DatePasswordAuthHelper.AuthenticateByQueryString(context))
                {
                    context.Response.StatusCode = 401;
                    context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"CAP Dashboard\"";
                    await context.Response.WriteAsync("Unauthorized. Please provide today's date (yyyyMMdd) as password.");
                    return;
                }
            }

            await _next(context);
        }
    }
}
