using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FakeMicro.Grains
{
    /// <summary>
    /// Orleans通用Grain基类（无状态版本）
    /// 遵循Orleans 9.x最佳实践
    /// </summary>
    public abstract class OrleansGrainBase:Grain
    {
        protected readonly ILogger _logger;
        protected readonly IGrainContext? _grainContext;

        protected OrleansGrainBase(
            ILogger logger,
            IGrainContext? grainContext = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _grainContext = grainContext;
        }

        /// <summary>
        /// 获取当前Grain的唯一标识符
        /// </summary>
        protected virtual string GetGrainIdentity()
        {
            return $"{GetType().Name}[{this.GetPrimaryKeyString()}]";
        }

        /// <summary>
        /// Orleans最佳实践：重写激活方法
        /// </summary>
        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Grain {GrainIdentity} 已激活", GetGrainIdentity());
            
            try
            {
                // 尝试恢复Grain状态
                return RecoverStateAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grain {GrainIdentity} 状态恢复失败", GetGrainIdentity());
                // 即使状态恢复失败，也要继续激活Grain
                return base.OnActivateAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Orleans最佳实践：重写停用方法
        /// </summary>
        public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Grain {GrainIdentity} 正在停用，原因: {Reason}",
                GetGrainIdentity(), reason.Description);
            
            try
            {
                // 执行清理操作
                return CleanupResourcesAsync(reason, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Grain {GrainIdentity} 资源清理失败", GetGrainIdentity());
                // 即使清理失败，也要继续停用Grain
                return base.OnDeactivateAsync(reason, cancellationToken);
            }
        }

        /// <summary>
        /// 性能监控：记录方法执行时间（Orleans最佳实践：避免过度日志）
        /// </summary>
        protected async Task<T> TrackPerformanceAsync<T>(string operationName, Func<Task<T>> operation)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await operation();
                stopwatch.Stop();
                
                // 仅记录耗时超过阈值的操作，避免日志噪声
                if (stopwatch.ElapsedMilliseconds > 500)
                {
                    _logger.LogInformation("操作完成: {OperationName}, 耗时: {ElapsedMs}ms",
                        operationName, stopwatch.ElapsedMilliseconds);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "操作失败: {OperationName}, 耗时: {ElapsedMs}ms",
                    operationName, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
        
        /// <summary>
        /// 执行带重试机制的操作
        /// </summary>
        protected async Task<T> ExecuteWithRetryAsync<T>(string operationName, Func<Task<T>> operation, int maxRetries = 3, TimeSpan? delay = null)
        {
            return await TransactionHelper.ExecuteWithRetryAsync(this, operation, operationName, maxRetries, delay);
        }
        
        /// <summary>
        /// 执行带断路器和重试机制的操作
        /// </summary>
        protected async Task<T> ExecuteWithCircuitBreakerAsync<T>(string operationName, string circuitName, Func<Task<T>> operation, int maxRetries = 3, TimeSpan? delay = null)
        {
            return await TransactionHelper.ExecuteWithCircuitBreakerAsync(this, operation, operationName, circuitName, maxRetries, delay);
        }
        
        /// <summary>
        /// 尝试恢复Grain状态（可由子类重写）
        /// </summary>
        protected virtual Task RecoverStateAsync(CancellationToken cancellationToken)
        {
            // 默认实现：不执行任何恢复操作
            return base.OnActivateAsync(cancellationToken);
        }
        
        /// <summary>
        /// 清理Grain资源（可由子类重写）
        /// </summary>
        protected virtual Task CleanupResourcesAsync(DeactivationReason reason, CancellationToken cancellationToken)
        {
            // 默认实现：不执行任何清理操作
            return base.OnDeactivateAsync(reason, cancellationToken);
        }

        /// <summary>
        /// 简化的日志方法
        /// </summary>
        protected void LogInformation(string message, params object[] args)
        {
            _logger.LogInformation(message, args);
        }

        protected void LogWarning(string message, params object[] args)
        {
            _logger.LogWarning(message, args);
        }

        protected void LogError(Exception ex, string message, params object[] args)
        {
            _logger.LogError(ex, message, args);
        }

        protected void LogTrace(string message, params object[] args)
        {
            _logger.LogTrace(message, args);
        }

        protected void LogDebug(string message, params object[] args)
        {
            _logger.LogDebug(message, args);
        }
        
        protected void LogCritical(string message, params object[] args)
        {
            _logger.LogCritical(message, args);
        }
        
        protected void LogCritical(Exception ex, string message, params object[] args)
        {
            _logger.LogCritical(ex, message, args);
        }
        
        /// <summary>
        /// 获取当前Grain的类型名称
        /// </summary>
        protected string GetGrainTypeName()
        {
            return GetType().Name;
        }
    }
}