using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace FakeMicro.Api.Security
{
    /// <summary>
    /// Hangfire Dashboard授权过滤器
    /// </summary>
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            
            // 检查用户是否已认证
            if (!httpContext.User?.Identity?.IsAuthenticated ?? true)
            {
                return false;
            }
            
            // 检查用户是否为管理员或系统管理员
            return httpContext.User.IsInRole("Admin") || 
                   httpContext.User.IsInRole("SystemAdmin") ||
                   httpContext.User.HasClaim(ClaimTypes.Role, "Admin") ||
                   httpContext.User.HasClaim(ClaimTypes.Role, "SystemAdmin");
        }
    }
}