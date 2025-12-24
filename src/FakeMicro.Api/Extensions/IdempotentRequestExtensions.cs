using Microsoft.AspNetCore.Builder;

namespace FakeMicro.Api.Extensions
{
    public static class IdempotentRequestExtensions
    {
        /// <summary>
        /// 注册幂等性请求检查中间件
        /// </summary>
        /// <param name="app">应用构建器</param>
        /// <returns>应用构建器</returns>
        public static IApplicationBuilder UseIdempotentRequests(this IApplicationBuilder app)
        {
            return app.UseMiddleware<Middleware.IdempotencyMiddleware>();
        }
    }
}