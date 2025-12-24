using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using FakeMicro.Api.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Net;

namespace FakeMicro.Api.Middleware;

/// <summary>
/// 幂等性中间件
/// 用于实现API请求的幂等性保证
/// </summary>
public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IdempotencyService _idempotencyService;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="next">下一个中间件</param>
    /// <param name="idempotencyService">幂等性服务</param>
    /// <param name="logger">日志记录器</param>
    public IdempotencyMiddleware(RequestDelegate next, IdempotencyService idempotencyService, ILogger<IdempotencyMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _idempotencyService = idempotencyService ?? throw new ArgumentNullException(nameof(idempotencyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 中间件处理方法
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <returns>任务</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // 只处理需要幂等性保证的HTTP方法
        if (!IsIdempotentMethod(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // 检查请求头中是否包含幂等性键
        if (!context.Request.Headers.TryGetValue("X-Idempotency-Key", out var idempotencyKeyValues) || 
            string.IsNullOrWhiteSpace(idempotencyKeyValues.ToString()))
        {
            // 如果没有幂等性键，继续处理请求
            await _next(context);
            return;
        }

        var idempotencyKey = idempotencyKeyValues.ToString().Trim();
        _logger.LogInformation("处理幂等性请求，键: {IdempotencyKey}, 路径: {Path}, 方法: {Method}",
            idempotencyKey, context.Request.Path, context.Request.Method);

        try
        {
            // 获取当前用户ID（如果有）
            long? userId = null;
            if (context.User.Identity.IsAuthenticated)
            {
                // 从Claims中获取用户ID，需要根据实际项目调整
                var userIdClaim = context.User.FindFirst("sub")?.Value;
                if (long.TryParse(userIdClaim, out var parsedUserId))
                {
                    userId = parsedUserId;
                }
            }

            // 检查是否已存在相同的幂等性请求
            var existingRequest = await _idempotencyService.GetIdempotentRequestAsync(
                idempotencyKey, 
                userId, 
                context.Request.Method, 
                context.Request.Path);

            if (existingRequest != null)
            {
                // 如果存在，直接返回缓存的响应
                await ReturnCachedResponseAsync(context, existingRequest);
                return;
            }

            // 如果不存在，创建响应流包装器来捕获响应
            var responseStream = context.Response.Body;
            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            try
            {
                // 继续处理请求
                await _next(context);
            }
            finally
            {
                // 重置响应流位置
                memoryStream.Seek(0, SeekOrigin.Begin);
                // 读取响应内容
                var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
                
                // 保存响应到幂等性服务
                await _idempotencyService.SaveIdempotentResponseAsync(
                    idempotencyKey, 
                    userId, 
                    context.Request.Method, 
                    context.Request.Path, 
                    context.Response.StatusCode, 
                    context.Response.Headers, 
                    responseBody);

                // 重置响应流位置
                memoryStream.Seek(0, SeekOrigin.Begin);
                // 将响应复制回原始响应流
                await memoryStream.CopyToAsync(responseStream);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理幂等性请求时发生错误: {IdempotencyKey}", idempotencyKey);
            // 发生错误时，让请求继续处理，保证服务可用性
            await _next(context);
        }
    }

    /// <summary>
    /// 检查HTTP方法是否需要幂等性保证
    /// </summary>
    /// <param name="method">HTTP方法</param>
    /// <returns>是否需要幂等性保证</returns>
    private bool IsIdempotentMethod(string method)
    {
        // 需要幂等性保证的HTTP方法：POST, PUT, DELETE
        // 注意：GET和HEAD方法本身就是幂等的，不需要额外处理
        return method.Equals(HttpMethods.Post, StringComparison.OrdinalIgnoreCase) ||
               method.Equals(HttpMethods.Put, StringComparison.OrdinalIgnoreCase) ||
               method.Equals(HttpMethods.Delete, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 返回缓存的响应
    /// </summary>
    /// <param name="context">HTTP上下文</param>
    /// <param name="request">幂等性请求记录</param>
    /// <returns>任务</returns>
    private async Task ReturnCachedResponseAsync(HttpContext context, DatabaseAccess.Entities.IdempotentRequest request)
    {
        _logger.LogInformation("返回缓存的幂等性响应: {IdempotencyKey}", request.IdempotencyKey);

        // 设置响应状态码
        context.Response.StatusCode = request.StatusCode;

        // 设置响应头
        var headers = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, string>>(request.ResponseHeaders);
        if (headers != null)
        {
            foreach (var header in headers)
            {
                // 避免重复设置某些头
                if (header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                context.Response.Headers[header.Key] = header.Value;
            }
        }

        // 设置内容类型
        context.Response.ContentType = "application/json";

        // 写入响应内容
        await context.Response.WriteAsync(request.ResponseBody);
    }
}
