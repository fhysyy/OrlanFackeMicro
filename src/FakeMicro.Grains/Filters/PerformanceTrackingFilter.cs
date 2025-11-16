using FakeMicro.Interfaces.Services;
using Orleans;
using Orleans.Runtime;
using System.Diagnostics;

namespace FakeMicro.Grains.Filters
{
    /// <summary>
    /// 性能跟踪过滤器 - 记录Grain调用的性能指标
    /// </summary>
    public class PerformanceTrackingFilter : IIncomingGrainCallFilter
    {
        private readonly ILoggerService _logger;

        public PerformanceTrackingFilter(ILoggerService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Invoke(IIncomingGrainCallContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var grainType = context.Grain.GetType().Name;
            var methodName = context.ImplementationMethod?.Name ?? "Unknown";
            
            try
            {
                await context.Invoke();
                stopwatch.Stop();
                
                // 记录性能指标（仅记录耗时较长的方法）
                if (stopwatch.ElapsedMilliseconds > 100) // 超过100ms的方法
                {
                    _logger.LogWarning("Grain调用耗时较长: {GrainType}.{MethodName}, 耗时: {ElapsedMs}ms", 
                        grainType, methodName, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation("Grain调用完成: {GrainType}.{MethodName}, 耗时: {ElapsedMs}ms", 
                        grainType, methodName, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Grain调用失败: {GrainType}.{MethodName}, 耗时: {ElapsedMs}ms", 
                    grainType, methodName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }

    /// <summary>
    /// 重试机制过滤器 - 为特定异常提供重试机制
    /// </summary>
    public class RetryFilter : IIncomingGrainCallFilter
    {
        private readonly ILoggerService _logger;
        private readonly int _maxRetryCount;
        private readonly TimeSpan _retryDelay;

        public RetryFilter(ILoggerService logger, int maxRetryCount = 3, TimeSpan? retryDelay = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxRetryCount = maxRetryCount;
            _retryDelay = retryDelay ?? TimeSpan.FromMilliseconds(100);
        }

        public async Task Invoke(IIncomingGrainCallContext context)
        {
            var retryCount = 0;
            var grainType = context.Grain.GetType().Name;
            var methodName = context.ImplementationMethod?.Name ?? "Unknown";

            while (true)
            {
                try
                {
                    await context.Invoke();
                    return;
                }
                catch (Exception ex) when (IsRetryableException(ex) && retryCount < _maxRetryCount)
                {
                    retryCount++;
                    _logger.LogWarning("Grain调用重试: {GrainType}.{MethodName}, 重试次数: {RetryCount}, 异常: {ExceptionMessage}", 
                        grainType, methodName, retryCount, ex.Message);
                    
                    await Task.Delay(_retryDelay * retryCount); // 指数退避
                }
            }
        }

        private bool IsRetryableException(Exception ex)
        {
            // 可重试的异常类型
            return ex is TimeoutException || 
                   ex is OrleansException || 
                   ex.InnerException is TimeoutException;
        }
    }

    /// <summary>
    /// 授权和验证过滤器 - 提供统一的权限验证
    /// </summary>
    public class AuthorizationFilter : IIncomingGrainCallFilter
    {
        private readonly ILoggerService _logger;

        public AuthorizationFilter(ILoggerService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Invoke(IIncomingGrainCallContext context)
        {
            var grainType = context.Grain.GetType().Name;
            var methodName = context.ImplementationMethod?.Name ?? "Unknown";

            // 检查方法是否需要授权（可通过特性标记）
            if (RequiresAuthorization(context))
            {
                // 这里可以实现具体的授权逻辑
                // 例如：检查JWT令牌、验证用户权限等
                if (!await IsAuthorizedAsync(context))
                {
                    _logger.LogWarning("授权失败: {GrainType}.{MethodName}", grainType, methodName);
                    throw new UnauthorizedAccessException("访问被拒绝");
                }
            }

            await context.Invoke();
        }

        private bool RequiresAuthorization(IIncomingGrainCallContext context)
        {
            // 这里可以根据方法特性或配置来判断是否需要授权
            // 例如：通过自定义特性标记需要授权的方法
            return context.ImplementationMethod?.GetCustomAttributes(true)
                .Any(attr => attr.GetType().Name.Contains("Authorize")) == true;
        }

        private async Task<bool> IsAuthorizedAsync(IIncomingGrainCallContext context)
        {
            // 实现具体的授权逻辑
            // 可以检查请求头、验证令牌等
            return await Task.FromResult(true); // 简化实现
        }
    }
}