using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FakeMicro.Api.Middleware
{
    /// <summary>
    /// 请求追踪中间件 - 用于调试API请求处理过程
    /// </summary>
    public class RequestTracingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestTracingMiddleware> _logger;

        public RequestTracingMiddleware(RequestDelegate next, ILogger<RequestTracingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString();
            
            // 记录请求开始
            LogRequestStart(context, requestId);
            
            try
            {
                // 设置请求追踪信息到上下文
                context.Items["RequestId"] = requestId;
                context.Items["RequestStartTime"] = DateTime.UtcNow;
                
                // 记录中间件处理开始
                _logger.LogInformation("[{RequestId}] 开始处理请求 - 路径: {Path}, 方法: {Method}", 
                    requestId, context.Request.Path, context.Request.Method);
                
                // 继续处理管道
                await _next(context);
                
                stopwatch.Stop();
                
                // 记录请求完成
                LogRequestComplete(context, requestId, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                // 记录请求异常
                LogRequestError(context, requestId, stopwatch.ElapsedMilliseconds, ex);
                
                // 重新抛出异常，让全局异常处理器处理
                throw;
            }
        }

        private void LogRequestStart(HttpContext context, string requestId)
        {
            var clientIp = GetClientIpAddress(context);
            var userAgent = context.Request.Headers.UserAgent.ToString();
            
            _logger.LogInformation("""
[{RequestId}] ======== 请求开始 ========
路径: {Path}
方法: {Method}
客户端IP: {ClientIp}
用户代理: {UserAgent}
时间: {Timestamp}
====================================
""", 
                requestId, context.Request.Path, context.Request.Method, clientIp, userAgent, DateTime.UtcNow);
        }

        private void LogRequestComplete(HttpContext context, string requestId, long durationMs)
        {
            var statusCode = context.Response.StatusCode;
            var statusDescription = GetHttpStatusDescription(statusCode);
            
            _logger.LogInformation("""
[{RequestId}] ======== 请求完成 ========
状态码: {StatusCode} ({StatusDescription})
处理时间: {DurationMs}ms
时间: {Timestamp}
====================================
""", 
                requestId, statusCode, statusDescription, durationMs, DateTime.UtcNow);
        }

        private void LogRequestError(HttpContext context, string requestId, long durationMs, Exception ex)
        {
            var statusCode = context.Response.StatusCode;
            var statusDescription = GetHttpStatusDescription(statusCode);
            
            _logger.LogError(ex, """
[{RequestId}] ======== 请求错误 ========
状态码: {StatusCode} ({StatusDescription})
处理时间: {DurationMs}ms
错误类型: {ExceptionType}
错误消息: {ErrorMessage}
堆栈跟踪: {StackTrace}
时间: {Timestamp}
====================================
""", 
                requestId, statusCode, statusDescription, durationMs, 
                ex.GetType().Name, ex.Message, ex.StackTrace, DateTime.UtcNow);
        }

        private string GetClientIpAddress(HttpContext context)
        {
            // 首先检查X-Forwarded-For头（如果有代理）
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var xForwardedForValues) && xForwardedForValues.Count > 0)
            {
                var xForwardedFor = xForwardedForValues[0];
                if (!string.IsNullOrEmpty(xForwardedFor))
                {
                    var firstIp = xForwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
                    if (!string.IsNullOrEmpty(firstIp))
                    {
                        return firstIp;
                    }
                }
            }
            
            // 使用连接的远程IP地址
            if (context.Connection?.RemoteIpAddress != null)
            {
                return context.Connection.RemoteIpAddress.ToString();
            }
            
            return "unknown";
        }

        private string GetHttpStatusDescription(int statusCode)
        {
            return statusCode switch
            {
                200 => "OK",
                201 => "Created",
                204 => "No Content",
                400 => "Bad Request",
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "Not Found",
                500 => "Internal Server Error",
                _ => "Unknown"
            };
        }
    }
}