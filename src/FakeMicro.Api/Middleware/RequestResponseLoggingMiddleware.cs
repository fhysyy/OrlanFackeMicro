using FakeMicro.DatabaseAccess;
using FakeMicro.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace FakeMicro.Api.Middleware
{
    /// <summary>
    /// 请求响应日志记录中间件
    /// 用于记录每个请求的入参和出参，并保存到审计日志中
    /// </summary>
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
        private readonly IAuditLogRepository _auditLogRepository;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="next">下一个中间件</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="auditLogRepository">审计日志数据访问接口</param>
        public RequestResponseLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestResponseLoggingMiddleware> logger,
            IAuditLogRepository auditLogRepository)
        {
            _next = next;
            _logger = logger;
            _auditLogRepository = auditLogRepository;
        }

        /// <summary>
        /// 中间件执行方法
        /// </summary>
        /// <param name="context">HTTP上下文</param>
        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var auditLog = new AuditLog
            {
                action = $"{context.Request.Method} {context.Request.Path}",
                resource = context.Request.Path.ToString(),
                ip_address = GetClientIpAddress(context),
                user_agent = context.Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.UtcNow,
                Result = "Success"
            };

            try
            {
                // 读取请求体并保存到审计日志
                var requestBody = await ReadRequestBodyAsync(context);
                auditLog.Details = $"请求参数: {requestBody}";

                // 创建响应体内存流
                var originalResponseBody = context.Response.Body;
                using var responseBodyMemoryStream = new MemoryStream();
                context.Response.Body = responseBodyMemoryStream;

                // 执行下一个中间件
                await _next(context);

                // 记录响应体
                responseBodyMemoryStream.Position = 0;
                var responseBody = await new StreamReader(responseBodyMemoryStream).ReadToEndAsync();
                responseBodyMemoryStream.Position = 0;
                await responseBodyMemoryStream.CopyToAsync(originalResponseBody);
                context.Response.Body = originalResponseBody;

                // 更新审计日志
                auditLog.Result = context.Response.StatusCode >= 200 && context.Response.StatusCode < 300 ? "Success" : "Failed";
                auditLog.Details = $"{auditLog.Details}\n响应结果: {responseBody}";
            }
            catch (Exception ex)
            {
                auditLog.Result = "Failed";
                auditLog.ErrorMessage = ex.Message;
                auditLog.Details = $"{auditLog.Details}\n错误信息: {ex.Message}";
                _logger.LogError(ex, "请求处理过程中发生错误");
                throw;
            }
            finally
            {
                stopwatch.Stop();
                auditLog.execution_time = stopwatch.ElapsedMilliseconds;
                auditLog.Details = $"{auditLog.Details}\n执行时间: {auditLog.execution_time}ms";

                try
                {
                    // 保存审计日志
                    await _auditLogRepository.AddAsync(auditLog);
                    await _auditLogRepository.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(JsonConvert.SerializeObject(auditLog));
                    // 如果保存审计日志失败，只记录错误，不影响主流程
                    _logger.LogError(ex, "保存审计日志失败");
                }
            }
        }

        /// <summary>
        /// 读取请求体内容
        /// </summary>
        /// <param name="context">HTTP上下文</param>
        /// <returns>请求体内容</returns>
        private async Task<string> ReadRequestBodyAsync(HttpContext context)
        {
            if (!context.Request.Body.CanSeek)
            {
                // 如果请求体不能seek，创建一个内存流以便多次读取
                context.Request.EnableBuffering();
            }

            context.Request.Body.Position = 0;
            var requestBody = await new StreamReader(context.Request.Body, Encoding.UTF8).ReadToEndAsync();
            context.Request.Body.Position = 0;

            return requestBody;
        }

        /// <summary>
        /// 获取客户端IP地址
        /// </summary>
        /// <param name="context">HTTP上下文</param>
        /// <returns>客户端IP地址</returns>
        private string GetClientIpAddress(HttpContext context)
        {
            // 尝试从X-Forwarded-For头获取IP（如果是代理请求）
            var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(ipAddress))
            {
                // 如果有多个IP，取第一个
                ipAddress = ipAddress.Split(',')[0].Trim();
            }

            // 如果没有X-Forwarded-For头，使用RemoteIpAddress
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = context.Connection.RemoteIpAddress?.ToString();
            }

            return ipAddress ?? "未知IP";
        }
    }
}