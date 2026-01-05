using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace FakeMicro.Api.Security
{
    /// <summary>
    /// 日期密码认证处理器
    /// 用于满足 ASP.NET Core 认证系统的要求
    /// 实际的认证逻辑在授权处理器中完成
    /// </summary>
    public class DatePasswordAuthenticationHandler : AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>
    {
        public DatePasswordAuthenticationHandler(
            IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var httpContext = Context;
            
            if (DatePasswordAuthHelper.AuthenticateRequest(httpContext) || 
                DatePasswordAuthHelper.AuthenticateByQueryString(httpContext))
            {
                var claims = new[] { new Claim(ClaimTypes.Name, "DashboardUser") };
                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
            
            return Task.FromResult(AuthenticateResult.NoResult());
        }
    }
}
