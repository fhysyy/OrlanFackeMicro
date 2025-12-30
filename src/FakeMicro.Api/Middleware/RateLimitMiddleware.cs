using FakeMicro.Api.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace FakeMicro.Api.Middleware
{
    /// <summary>
    /// 流量控制中间件
    /// 实现基于令牌桶算法的API限流
    /// </summary>
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RateLimitMiddleware> _logger;
        private readonly RateLimitConfig _rateLimitConfig;

        public RateLimitMiddleware(RequestDelegate next, IMemoryCache cache, ILogger<RateLimitMiddleware> logger, RateLimitConfig rateLimitConfig)
        {
            _next = next;
            _cache = cache;
            _logger = logger;
            _rateLimitConfig = rateLimitConfig;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestId = context.Items["RequestId"]?.ToString() ?? "unknown";
            
            // 获取客户端标识（IP地址或用户ID）
            var clientIdentifier = GetClientIdentifier(context);
            
            _logger.LogInformation("[{RequestId}] 限流中间件开始处理 - 客户端标识: {ClientIdentifier}", requestId, clientIdentifier);
            
            // 检查是否在白名单中（管理员等特殊用户）
            if (IsExemptFromRateLimit(context))
            {
                _logger.LogInformation("[{RequestId}] 客户端在白名单中，跳过限流检查 - 路径: {Path}", requestId, context.Request.Path);
                await _next(context);
                return;
            }

            // 获取限流配置和端点模式
            var (rateLimitConfig, endpointPattern) = GetRateLimitConfig(context);
            _logger.LogInformation("[{RequestId}] 使用限流配置 - 端点: {Endpoint}, 容量: {Capacity}, 速率: {Rate}/s, 窗口: {Window}s", 
                requestId, endpointPattern, rateLimitConfig.BucketCapacity, rateLimitConfig.TokensPerSecond, rateLimitConfig.WindowSeconds);

            // 检查是否超过限制
            if (!await CheckRateLimit(clientIdentifier, endpointPattern, rateLimitConfig))
            {
                _logger.LogWarning("[{RequestId}] 客户端触发限流 - 标识: {ClientIdentifier}, 路径: {Path}, 端点类型: {Endpoint}", 
                    requestId, clientIdentifier, context.Request.Path, endpointPattern);
                
                // 计算预计重试时间（基于当前令牌数量和补充速率）
                var retryAfterSeconds = Math.Ceiling(1 / rateLimitConfig.TokensPerSecond);
                
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                // 添加Retry-After响应头，帮助客户端了解何时可以重试
                context.Response.Headers.Append("Retry-After", retryAfterSeconds.ToString());
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                {
                    error = "请求过于频繁，请稍后再试",
                    errorCode = "rate_limit_exceeded",
                    message = "您已超过接口调用频率限制",
                    requestId = requestId,
                    retryAfter = retryAfterSeconds,
                    clientIdentifier = clientIdentifier,
                    timestamp = DateTime.UtcNow,
                    details = new {
                        path = context.Request.Path,
                        method = context.Request.Method,
                        endpointType = endpointPattern,
                        rateLimit = new {
                            capacity = rateLimitConfig.BucketCapacity,
                            tokensPerSecond = rateLimitConfig.TokensPerSecond,
                            windowSeconds = rateLimitConfig.WindowSeconds
                        }
                    }
                }));
                return;
            }

            _logger.LogInformation("[{RequestId}] 限流检查通过，继续处理请求", requestId);
            await _next(context);
        }

        private string GetClientIdentifier(HttpContext context)
        {
            // 优先使用用户ID，如果没有认证则使用IP地址
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                return $"user:{userId}";
            }

            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return $"ip:{ipAddress}";
        }

        private bool IsExemptFromRateLimit(HttpContext context)
        {
            // 管理员用户不受限流限制
            if (context.User?.IsInRole("Admin") == true || context.User?.IsInRole("SystemAdmin") == true)
            {
                return true;
            }

            // 特定路径不受限流限制（如健康检查）
            var path = context.Request.Path.ToString().ToLower();
            if (path.Contains("/health") || path.Contains("/swagger"))
            {
                return true;
            }

            return false;
        }

        private (RateLimitOptions Config, string EndpointPattern) GetRateLimitConfig(HttpContext context)
        {
            var path = context.Request.Path.ToString().ToLower();
            
            // 根据路径设置不同的限流策略
            if (path.Contains("/auth/login") || path.Contains("/auth/register"))
            {
                // 认证接口限制更严格
                return (_rateLimitConfig.AuthEndpoints, "auth");
            }
            else if (path.Contains("/files/upload") || path.Contains("/files/download"))
            {
                // 文件上传下载接口
                return (_rateLimitConfig.FileEndpoints, "file");
            }
            else if (path.Contains("/admin/"))
            {
                // 管理接口
                return (_rateLimitConfig.AdminEndpoints, "admin");
            }
            else if (path.Contains("/api/"))
            {
                // 普通API接口
                return (_rateLimitConfig.ApiEndpoints, "api");
            }
            else
            {
                // 默认配置
                return (_rateLimitConfig.Default, "default");
            }
        }

        private async Task<bool> CheckRateLimit(string clientIdentifier, string endpointPattern, RateLimitOptions config)
        {
            // 为每个客户端+端点组合创建唯一的缓存键，实现更精细的限流
            var cacheKey = $"ratelimit:{clientIdentifier}:{endpointPattern}";
            var now = DateTime.UtcNow;

            // 线程安全的方式获取或创建令牌桶
            var bucket = await _cache.GetOrCreateAsync(cacheKey, entry =>
            {
                // 设置缓存过期时间为窗口时间的2倍，确保令牌桶能正确重置
                entry.AbsoluteExpiration = now.AddSeconds(config.WindowSeconds * 2);
                entry.SlidingExpiration = TimeSpan.FromSeconds(config.WindowSeconds);
                
                return Task.FromResult(new TokenBucket
                {
                    Tokens = config.BucketCapacity,
                    LastRefillTime = now
                });
            });

            // 检查bucket是否为null（不应该发生，但为了安全考虑）
            if (bucket == null)
            {
                _logger.LogWarning("令牌桶创建失败，客户端: {ClientIdentifier}, 端点: {Endpoint}", clientIdentifier, endpointPattern);
                return false;
            }

            // 计算应该补充的令牌数量
            var timePassed = now - bucket.LastRefillTime;
            var tokensToAdd = timePassed.TotalSeconds * config.TokensPerSecond;
            
            // 更新令牌桶状态（线程安全）
            lock (bucket)
            {
                bucket.Tokens = Math.Min(bucket.Tokens + tokensToAdd, config.BucketCapacity);
                bucket.LastRefillTime = now;

                // 检查是否有足够的令牌
                if (bucket.Tokens >= 1)
                {
                    bucket.Tokens -= 1;
                    // 更新缓存中的令牌桶
                    _cache.Set(cacheKey, bucket, TimeSpan.FromSeconds(config.WindowSeconds * 2));
                    return true;
                }
            }

            _logger.LogWarning("客户端 {ClientIdentifier} 在端点 {Endpoint} 触发限流，当前令牌数: {Tokens}", 
                clientIdentifier, endpointPattern, bucket.Tokens);
            return false;
        }
    }



    /// <summary>
    /// 令牌桶算法实现
    /// </summary>
    public class TokenBucket
    {
        public double Tokens { get; set; }
        public DateTime LastRefillTime { get; set; }
    }

    /// <summary>
    /// 限流中间件扩展方法
    /// </summary>
    public static class RateLimitMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimit(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RateLimitMiddleware>();
        }
    }
}