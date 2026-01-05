using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;

namespace FakeMicro.Api.Security
{
    /// <summary>
    /// 日期密码授权处理器
    /// 用于 CAP Dashboard 的授权验证
    /// </summary>
    public class DatePasswordAuthorizationHandler : AuthorizationHandler<DatePasswordRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DatePasswordRequirement requirement)
        {
            var httpContext = context.Resource as Microsoft.AspNetCore.Http.HttpContext;
            
            if (httpContext == null)
            {
                return Task.CompletedTask;
            }
            
            if (DatePasswordAuthHelper.AuthenticateRequest(httpContext) || 
                DatePasswordAuthHelper.AuthenticateByQueryString(httpContext))
            {
                context.Succeed(requirement);
            }
            
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 日期密码授权要求
    /// </summary>
    public class DatePasswordRequirement : IAuthorizationRequirement
    {
    }
}
