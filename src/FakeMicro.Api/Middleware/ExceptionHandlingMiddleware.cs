using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FakeMicro.Shared.Exceptions;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Claims;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace FakeMicro.Api.Middleware
{
    /// <summary>
    /// 全局异常处理中间件
    /// 捕获并处理应用程序中的所有未处理异常
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="next">下一个中间件委托</param>
        /// <param name="logger">日志记录器</param>
        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// 处理HTTP请求并捕获异常
        /// </summary>
        /// <param name="context">HTTP上下文</param>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                // 获取请求信息用于日志记录
                var requestInfo = GetRequestInfo(context);
                
                // 根据异常类型设置不同的状态码
                var statusCode = DetermineStatusCode(exception);
                
                // 记录详细的异常信息
                LogExceptionDetails(context, exception, statusCode, requestInfo);
                
                // 向客户端返回适当的错误响应
                await HandleExceptionAsync(context, exception, statusCode);
            }
        }

        /// <summary>
        /// 收集请求信息
        /// </summary>
        /// <param name="context">HTTP上下文</param>
        /// <returns>请求信息对象</returns>
        private RequestInfo GetRequestInfo(HttpContext context)
        {
            var userInfo = new UserInfo
            {
                IsAuthenticated = context.User.Identity?.IsAuthenticated ?? false,
                UserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier),
                Username = context.User.FindFirstValue(ClaimTypes.Name),
                Roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
            };

            return new RequestInfo
            {
                Path = context.Request.Path,
                Method = context.Request.Method,
                QueryString = context.Request.QueryString.ToString(),
                ClientIp = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers.UserAgent.ToString(),
                User = userInfo
            };
        }

        /// <summary>
        /// 根据异常类型确定HTTP状态码
        /// </summary>
        /// <param name="exception">异常对象</param>
        /// <returns>HTTP状态码</returns>
        private HttpStatusCode DetermineStatusCode(Exception exception)
        {
            // 优先处理自定义异常
            if (exception is CustomException customException)
            {
                return (HttpStatusCode)customException.HttpStatusCode;
            }
            return exception switch
            {
                ArgumentException or ArgumentNullException or ArgumentOutOfRangeException or FormatException => 
                    HttpStatusCode.BadRequest,
                UnauthorizedAccessException or System.Security.Authentication.AuthenticationException => 
                    HttpStatusCode.Unauthorized,
                AccessViolationException => 
                    HttpStatusCode.Forbidden,
                KeyNotFoundException or System.Data.DataException => 
                    HttpStatusCode.NotFound,
                TimeoutException => 
                    HttpStatusCode.RequestTimeout,
                NotImplementedException => 
                    HttpStatusCode.NotImplemented,
                InvalidOperationException when exception.Message.Contains("并发") => 
                    HttpStatusCode.Conflict,
                InvalidOperationException => 
                    HttpStatusCode.BadRequest,
                _ => 
                    HttpStatusCode.InternalServerError
            };
        }

        /// <summary>
        /// 记录异常的详细信息
        /// </summary>
        /// <param name="context">HTTP上下文</param>
        /// <param name="exception">异常对象</param>
        /// <param name="statusCode">HTTP状态码</param>
        /// <param name="requestInfo">请求信息</param>
        private void LogExceptionDetails(HttpContext context, Exception exception, HttpStatusCode statusCode, RequestInfo requestInfo)
        {
            // 根据异常类型和状态码使用不同的日志级别
            if (statusCode >= HttpStatusCode.InternalServerError)
            {
                // 服务器错误使用Error级别
                _logger.LogError(
                    exception,
                    "[全局异常] 严重错误 - {StatusCode} {Message} | 请求: {Method} {Path} | 用户: {UserId}",
                    (int)statusCode,
                    exception.Message,
                    requestInfo.Method,
                    requestInfo.Path,
                    requestInfo.User.UserId ?? "未认证"
                );

                // 记录详细的请求上下文
                _logger.LogDebug(
                    "请求详情: 客户端IP={ClientIp}, UserAgent={UserAgent}, QueryString={QueryString}, 角色={Roles}",
                    requestInfo.ClientIp,
                    requestInfo.UserAgent,
                    requestInfo.QueryString,
                    string.Join(", ", requestInfo.User.Roles)
                );

                // 对于关键异常，记录完整堆栈
                if (exception is System.Data.Common.DbException or System.IO.IOException or System.Net.Http.HttpRequestException)
                {
                    _logger.LogError(exception, "关键异常堆栈跟踪");
                }
            }
            else if (statusCode == HttpStatusCode.BadRequest || statusCode == HttpStatusCode.NotFound)
            {
                // 客户端错误使用Warning级别
                _logger.LogWarning(
                    exception,
                    "[全局异常] 客户端错误 - {StatusCode} {Message} | 请求: {Method} {Path}",
                    (int)statusCode,
                    exception.Message,
                    requestInfo.Method,
                    requestInfo.Path
                );
            }
            else
            {
                // 其他错误使用Information级别
                _logger.LogInformation(
                    exception,
                    "[全局异常] 其他错误 - {StatusCode} {Message}",
                    (int)statusCode,
                    exception.Message
                );
            }
        }

        /// <summary>
        /// 处理异常并返回JSON响应
        /// </summary>
        /// <param name="context">HTTP上下文</param>
        /// <param name="exception">异常对象</param>
        /// <param name="statusCode">HTTP状态码</param>
        private async Task HandleExceptionAsync(HttpContext context, Exception exception, HttpStatusCode statusCode)
        {
            // 设置响应头
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            // 使用索引器赋值，避免重复键异常
            context.Response.Headers["X-Error-Id"] = Guid.NewGuid().ToString();
            context.Response.Headers["X-Error-Type"] = exception.GetType().Name;

            // 构建错误响应对象
            var errorResponse = new ErrorResponse
            {
                StatusCode = context.Response.StatusCode,
                Message = GetUserFriendlyMessage(exception, statusCode),
                ErrorType = exception.GetType().Name,
                Timestamp = DateTime.UtcNow,
                TraceId = context.TraceIdentifier,
                Path = context.Request.Path
            };

            // 添加自定义异常特定的信息
            if (exception is CustomException customException)
            {
                errorResponse.ErrorCode = customException.ErrorCode;
                if (customException.Details != null && customException.Details.Any())
                {
                    errorResponse.Details ??= new ErrorDetails();
                    errorResponse.Details.CustomDetails = JsonSerializer.Serialize(customException.Details);
                }
                // 处理验证异常的特殊情况
                if (exception is ValidationException validationException)
                {
                    errorResponse.ValidationErrors = validationException.ValidationErrors.Select(err => new ValidationError
                    {
                        Field = err.FieldName,
                        Message = err.ErrorMessage
                    }).ToList();
                }
            }
            // 开发环境下包含详细错误信息
            if (context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
            {
                errorResponse.Details ??= new ErrorDetails();
                errorResponse.Details.ExceptionMessage = exception.Message;
                errorResponse.Details.InnerExceptionMessage = exception.InnerException?.Message;
                errorResponse.Details.StackTrace = exception.StackTrace;
            }
            // 序列化响应并返回
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, jsonOptions));
        }

        /// <summary>
        /// 获取对用户友好的错误消息
        /// </summary>
        /// <param name="exception">异常对象</param>
        /// <param name="statusCode">HTTP状态码</param>
        /// <returns>用户友好的错误消息</returns>
        private string GetUserFriendlyMessage(Exception exception, HttpStatusCode statusCode)
        {
            // 对于内部服务器错误，返回通用消息
            if (statusCode == HttpStatusCode.InternalServerError)
            {
                return "服务器内部错误，请稍后重试。我们的团队已收到通知并正在处理此问题。";
            }

            // 根据异常类型返回特定的友好消息
            return exception switch
            {
                ArgumentException ex => 
                    $"参数错误: {ex.ParamName ?? "未知参数"} - {ex.Message}",
                UnauthorizedAccessException => 
                    "未经授权的访问，请登录后重试。",
                KeyNotFoundException => 
                    "请求的资源不存在。",
                TimeoutException => 
                    "请求超时，请稍后重试。",
                _ => 
                    exception.Message
            };
        }
    }

    #region 辅助类

    /// <summary>
    /// 请求信息类
    /// </summary>
    internal class RequestInfo
    {
        public string Path { get; set; }
        public string Method { get; set; }
        public string QueryString { get; set; }
        public string ClientIp { get; set; }
        public string UserAgent { get; set; }
        public UserInfo User { get; set; }
    }

    /// <summary>
    /// 用户信息类
    /// </summary>
    internal class UserInfo
    {
        public bool IsAuthenticated { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public List<string> Roles { get; set; }
    }

    /// <summary>
    /// 错误响应类
    /// </summary>
    internal class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public string ErrorType { get; set; }
        public string ErrorCode { get; set; }
        public DateTime Timestamp { get; set; }
        public string TraceId { get; set; }
        public string Path { get; set; }
        public ErrorDetails Details { get; set; }
        public ICollection<ValidationError> ValidationErrors { get; set; }
    }

    /// <summary>
    /// 错误详情类（仅在开发环境显示）
    /// </summary>
    internal class ErrorDetails
    {
        public string ExceptionMessage { get; set; }
        public string InnerExceptionMessage { get; set; }
        public string StackTrace { get; set; }
        public string CustomDetails { get; set; }
    }

    /// <summary>
    /// 验证错误信息类
    /// </summary>
    internal class ValidationError
    {
        public string Field { get; set; }
        public string Message { get; set; }
    }

    #endregion
}