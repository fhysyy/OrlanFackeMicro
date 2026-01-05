using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using System;

namespace FakeMicro.Api.Security
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            var requestPath = httpContext.Request.Path.Value;
            var queryString = httpContext.Request.QueryString.Value;
            var currentDatePassword = DateTime.Now.ToString("yyyyMMdd");

           

            if (!string.IsNullOrEmpty(requestPath))
            {
                var lowerPath = requestPath.ToLowerInvariant();

                if (lowerPath.Contains("/css") || lowerPath.Contains("/js") || lowerPath.Contains("/fonts") || 
                    lowerPath.Contains("/images") || lowerPath.Contains(".css") || lowerPath.Contains(".js"))
                {
                    return true;
                }
            }

            var authResult = DatePasswordAuthHelper.AuthenticateByCookie(httpContext) ||
                            DatePasswordAuthHelper.AuthenticateRequest(httpContext) || 
                            DatePasswordAuthHelper.AuthenticateByQueryString(httpContext);

        
            return authResult;
        }
    }
}