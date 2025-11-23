using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FakeMicro.Api.Middleware
{
    /// <summary>
    /// CAP事件总线中间件
    /// 确保CAP请求处理上下文正确设置
    /// </summary>
    public class CapMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CapMiddleware> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="next">下一个中间件</param>
        /// <param name="logger">日志记录器</param>
        public CapMiddleware(RequestDelegate next, ILogger<CapMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// 处理请求
        /// </summary>
        /// <param name="context">HTTP上下文</param>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // 继续处理请求
                await _next(context);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "CAP中间件处理请求时发生错误");
                throw;
            }
        }
    }
}