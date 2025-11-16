using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace FakeMicro.Api.Middleware
{
    /// <summary>
    /// 增强的全局异常处理器中间件 - 提供详细的错误信息用于调试
    /// </summary>
    public class EnhancedExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<EnhancedExceptionHandlerMiddleware> _logger;

        public EnhancedExceptionHandlerMiddleware(RequestDelegate next, ILogger<EnhancedExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var requestId = context.Items["RequestId"]?.ToString() ?? "unknown";
            var statusCode = GetStatusCode(exception);
            var statusDescription = GetHttpStatusDescription(statusCode);
            var errorCode = GetErrorCode(exception);

            // 记录详细的错误信息
            _logger.LogError(exception, 
                """
[{RequestId}] ======== 全局异常处理器捕获错误 ========
状态码: {StatusCode} ({StatusDescription})
错误类型: {ExceptionType}
错误代码: {ErrorCode}
错误消息: {ErrorMessage}
路径: {Path}
方法: {Method}
客户端IP: {ClientIp}
堆栈跟踪: {StackTrace}
时间: {Timestamp}
==================================================
""", 
                requestId, statusCode, statusDescription, exception.GetType().Name, 
                errorCode, exception.Message, context.Request.Path, context.Request.Method,
                GetClientIpAddress(context), exception.StackTrace, DateTime.UtcNow);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            // 创建详细错误响应
            var response = CreateErrorResponse(context, exception, statusCode, errorCode, requestId);
            
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                //WriteIndented = context.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true
            }));
        }

        private object CreateErrorResponse(HttpContext context, Exception exception, int statusCode, string errorCode, string requestId)
        {
           // var isDevelopment = context.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true;

            var response = new
            {
                error = GetHttpStatusDescription(statusCode),
                errorCode = errorCode,
                message = exception.Message,
                requestId = requestId,
                timestamp = DateTime.UtcNow,
                path = context.Request.Path,
                method = context.Request.Method
            };

            // 在开发环境中提供详细的错误信息
            //if (isDevelopment)
            //{
            //    return new
            //    {
            //        error = GetHttpStatusDescription(statusCode),
            //        errorCode = errorCode,
            //        message = exception.Message,
            //        requestId = requestId,
            //        timestamp = DateTime.UtcNow,
            //        path = context.Request.Path,
            //        method = context.Request.Method,
            //        exceptionType = exception.GetType().Name,
            //        stackTrace = exception.StackTrace,
            //        innerException = exception.InnerException != null ? new
            //        {
            //            message = exception.InnerException.Message,
            //            type = exception.InnerException.GetType().Name,
            //            stackTrace = exception.InnerException.StackTrace
            //        } : null
            //    };
            //}

            return response;
        }

        private int GetStatusCode(Exception exception)
        {
            return exception switch
            {
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                ArgumentException => (int)HttpStatusCode.BadRequest,
                InvalidOperationException => (int)HttpStatusCode.BadRequest,
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                NotImplementedException => (int)HttpStatusCode.NotImplemented,
                TimeoutException => (int)HttpStatusCode.RequestTimeout,
                _ => (int)HttpStatusCode.InternalServerError
            };
        }

        private string GetErrorCode(Exception exception)
        {
            return exception switch
            {
                UnauthorizedAccessException => "unauthorized_access",
                ArgumentException => "invalid_argument",
                InvalidOperationException => "invalid_operation",
                KeyNotFoundException => "resource_not_found",
                NotImplementedException => "not_implemented",
                TimeoutException => "request_timeout",
                _ => "internal_server_error"
            };
        }

        private string GetClientIpAddress(HttpContext context)
        {
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
                503 => "Service Unavailable",
                _ => "Unknown"
            };
        }
    }

    /// <summary>
    /// 增强的全局异常处理器中间件扩展方法
    /// </summary>
    public static class EnhancedExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseEnhancedExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<EnhancedExceptionHandlerMiddleware>();
        }
    }
}