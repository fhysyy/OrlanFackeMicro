using FakeMicro.Api.Middleware;
using Microsoft.AspNetCore.Builder;

namespace FakeMicro.Api.Extensions
{
    /// <summary>
    /// ApplicationBuilder扩展方法
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// 使用全局异常处理
        /// </summary>
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}