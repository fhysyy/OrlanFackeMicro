using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Net;
using System.Threading.Tasks;

namespace FakeMicro.Api.Middleware
{
    /// <summary>
    /// API限流中间件 - 使用Redis滑动窗口算法
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly IConnectionMultiplexer _redis;
        private readonly RateLimitOptions _options;

        public RateLimitingMiddleware(
            RequestDelegate next,
            ILogger<RateLimitingMiddleware> logger,
            IConnectionMultiplexer redis,
            RateLimitOptions options)
        {
            _next = next;
            _logger = logger;
            _redis = redis;
            _options = options;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 跳过健康检查端点
            if (context.Request.Path.StartsWithSegments("/health") ||
                context.Request.Path.StartsWithSegments("/api/health"))
            {
                await _next(context);
                return;
            }

            // 获取客户端标识
            var clientId = GetClientIdentifier(context);
            var endpoint = $"{context.Request.Method}:{context.Request.Path}";

            try
            {
                var allowed = await CheckRateLimitAsync(clientId, endpoint);

                if (!allowed)
                {
                    _logger.LogWarning("Rate limit exceeded for client: {ClientId}, endpoint: {Endpoint}", 
                        clientId, endpoint);

                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(
                        System.Text.Json.JsonSerializer.Serialize(new
                        {
                            error = "Rate limit exceeded",
                            message = $"Too many requests. Please try again later.",
                            retryAfter = _options.WindowSeconds
                        }));
                    return;
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in rate limiting middleware");
                // 限流失败时放行请求（故障开放策略）
                await _next(context);
            }
        }

        /// <summary>
        /// 使用Redis滑动窗口算法检查限流
        /// </summary>
        private async Task<bool> CheckRateLimitAsync(string clientId, string endpoint)
        {
            var db = _redis.GetDatabase();
            var key = $"ratelimit:{clientId}:{endpoint}";
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var windowStart = now - (_options.WindowSeconds * 1000);

            // Lua脚本：原子性地实现滑动窗口限流
            var script = @"
                local key = KEYS[1]
                local now = tonumber(ARGV[1])
                local window_start = tonumber(ARGV[2])
                local limit = tonumber(ARGV[3])
                local window_seconds = tonumber(ARGV[4])

                -- 删除窗口外的记录
                redis.call('ZREMRANGEBYSCORE', key, 0, window_start)

                -- 获取当前窗口内的请求数
                local current = redis.call('ZCARD', key)

                if current < limit then
                    -- 添加当前请求
                    redis.call('ZADD', key, now, now)
                    -- 设置过期时间
                    redis.call('EXPIRE', key, window_seconds)
                    return 1
                else
                    return 0
                end
            ";

            var result = await db.ScriptEvaluateAsync(
                script,
                new RedisKey[] { key },
                new RedisValue[] { now, windowStart, _options.RequestLimit, _options.WindowSeconds }
            );

            return (int)result == 1;
        }

        /// <summary>
        /// 获取客户端标识（IP或用户ID）
        /// </summary>
        private string GetClientIdentifier(HttpContext context)
        {
            // 优先使用用户ID（已认证用户）
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst("sub")?.Value 
                    ?? context.User.FindFirst("user_id")?.Value 
                    ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                if (!string.IsNullOrEmpty(userId))
                {
                    return $"user:{userId}";
                }
            }

            // 使用IP地址
            var ip = context.Connection.RemoteIpAddress?.ToString();
            
            // 检查是否通过代理
            if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                ip = context.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
            }
            else if (context.Request.Headers.ContainsKey("X-Real-IP"))
            {
                ip = context.Request.Headers["X-Real-IP"].ToString();
            }

            return $"ip:{ip ?? "unknown"}";
        }
    }

    /// <summary>
    /// 限流配置选项
    /// </summary>
    public class RateLimitOptions
    {
        /// <summary>
        /// 时间窗口（秒）
        /// </summary>
        public int WindowSeconds { get; set; } = 60;

        /// <summary>
        /// 窗口内最大请求数
        /// </summary>
        public int RequestLimit { get; set; } = 100;

        /// <summary>
        /// 是否启用限流
        /// </summary>
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// 扩展方法
    /// </summary>
    public static class RateLimitingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimiting(
            this IApplicationBuilder builder,
            RateLimitOptions options = null)
        {
            options ??= new RateLimitOptions();
            
            if (options.Enabled)
            {
                return builder.UseMiddleware<RateLimitingMiddleware>(options);
            }

            return builder;
        }
    }
}
