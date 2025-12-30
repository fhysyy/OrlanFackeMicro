using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;

namespace FakeMicro.Api.Security
{
    /// <summary>
    /// Hangfire Dashboard授权过滤器
    /// </summary>
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            // 由于当前JWT认证已被注释，暂时允许匿名访问Hangfire Dashboard
            return true;
        }
    }
}