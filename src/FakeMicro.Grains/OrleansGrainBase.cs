using FakeMicro.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading;
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

        // 日志记录阈值配置
        protected const int PERFORMANCE_LOG_THRESHOLD = 500; // 毫秒
        protected const bool ENABLE_DEBUG_LOGS = false; // 默认不启用DEBUG级别的日志

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="grainContext">Grain上下文（可选）</param>
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
        /// 获取日志记录器实例
        /// </summary>
        /// <returns>ILogger实例</returns>
        public ILogger GetLogger()
        {
            return _logger;
        }

        /// <summary>
        /// Orleans最佳实践：重写激活方法
        /// </summary>
        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            LogInformation("已激活");
            
            try
            {
                // 尝试恢复Grain状态
                return RecoverStateAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                LogError(ex, "状态恢复失败");
                // 即使状态恢复失败，也要继续激活Grain
                return base.OnActivateAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Orleans最佳实践：重写停用方法
        /// </summary>
        public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
        {
            LogInformation("正在停用，原因: {Reason}", reason.Description);
            
            try
            {
                // 执行清理操作
                return CleanupResourcesAsync(reason, cancellationToken);
            }
            catch (Exception ex)
            {
                LogError(ex, "资源清理失败");
                // 即使清理失败，也要继续停用Grain
                return base.OnDeactivateAsync(reason, cancellationToken);
            }
        }

        /// <summary>
        /// 性能监控：记录方法执行时间（Orleans最佳实践：避免过度日志）
        /// </summary>
        protected async Task<T> TrackPerformanceAsync<T>(string operationName, Func<Task<T>> operation, params string[]? sensitiveParameters)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await operation();
                stopwatch.Stop();
                
                // 仅记录耗时超过阈值的操作，避免日志噪声
                if (stopwatch.ElapsedMilliseconds > PERFORMANCE_LOG_THRESHOLD)
                {
                    LogInformation("操作完成: {OperationName}, 耗时: {ElapsedMs}ms",
                        operationName, stopwatch.ElapsedMilliseconds);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                LogError(ex, "操作失败: {OperationName}, 耗时: {ElapsedMs}ms",
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
        /// 安全执行异步操作并返回结果，包含日志记录和异常处理
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="operationName">操作名称</param>
        /// <param name="operation">操作委托</param>
        /// <param name="sensitiveParameters">敏感参数（仅用于日志记录）</param>
        /// <returns>操作结果</returns>
        protected async Task<T> SafeExecuteAsync<T>(string operationName, Func<Task<T>> operation, params string[]? sensitiveParameters)
        {
            var parametersLog = sensitiveParameters != null && sensitiveParameters.Length > 0                ? $"参数: [{string.Join(", ", sensitiveParameters)}]"                : "无参数";
            LogTrace("开始执行: {OperationName} {Parameters}", operationName, parametersLog);
            try
            {
                // 使用性能跟踪包装操作
                var result = await TrackPerformanceAsync(operationName, operation, sensitiveParameters);
                
                LogTrace("执行成功: {OperationName}", operationName);
                return result;
            }
            catch (CustomException ex)
            {
                // 自定义异常已包含足够信息，直接记录
                LogError(ex, "自定义异常: {OperationName} - {Message}", operationName, ex.Message);
                throw; // 重新抛出，由上层处理
            }
            catch (ArgumentNullException ex)
            {
                LogError(ex, "参数为空异常: {OperationName} - 参数名称: {ParamName}", operationName, ex.ParamName);
                throw new ValidationException(ex.ParamName, "参数不能为空");
            }
            catch (ArgumentException ex)
            {
                LogError(ex, "参数异常: {OperationName} - {Message}", operationName, ex.Message);
                throw new ValidationException(ex.ParamName ?? "未知参数", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                LogError(ex, "无效操作异常: {OperationName} - {Message}", operationName, ex.Message);
                throw new BusinessException(ex.Message, "invalid_operation");
            }
            catch (KeyNotFoundException ex)
            {
                LogError(ex, "键不存在异常: {OperationName} - {Message}", operationName, ex.Message);
                throw new NotFoundException("资源", ex.Message);
            }
            catch (TimeoutException ex)
            {
                LogError(ex, "超时异常: {OperationName} - {Message}", operationName, ex.Message);
                throw new BusinessException("操作超时，请稍后重试", "operation_timeout", ex);
            }
            catch (Exception ex)
            {
                LogError(ex, "未处理异常: {OperationName}", operationName);
                throw new BusinessException("系统内部错误，请联系管理员", "internal_server_error", ex);
            }
        }
        /// <summary>
        /// 安全执行异步操作（无返回值），包含日志记录和异常处理
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <param name="operation">操作委托</param>
        /// <param name="sensitiveParameters">敏感参数（仅用于日志记录）</param>
        /// <returns>任务对象</returns>
        protected async Task SafeExecuteAsync(string operationName, Func<Task> operation, params string[]? sensitiveParameters)
        {
            await SafeExecuteAsync(operationName, async () =>
            {
                await operation();
                return true;
            }, sensitiveParameters);
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
        /// 简化的日志方法 - 统一日志格式
        /// </summary>
        protected void LogInformation(string message, params object[] args)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(FormatLogMessage(message), args);
            }
        }

        protected void LogWarning(string message, params object[] args)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(FormatLogMessage(message), args);
            }
        }

        protected void LogError(Exception ex, string message, params object[] args)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, FormatLogMessage(message), args);
            }
        }

        protected void LogTrace(string message, params object[] args)
        {
            if (ENABLE_DEBUG_LOGS && _logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace(FormatLogMessage(message), args);
            }
        }

        protected void LogDebug(string message, params object[] args)
        {
            if (ENABLE_DEBUG_LOGS && _logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(FormatLogMessage(message), args);
            }
        }
        
        protected void LogCritical(string message, params object[] args)
        {
            if (_logger.IsEnabled(LogLevel.Critical))
            {
                _logger.LogCritical(FormatLogMessage(message), args);
            }
        }
        
        protected void LogCritical(Exception ex, string message, params object[] args)
        {
            if (_logger.IsEnabled(LogLevel.Critical))
            {
                _logger.LogCritical(ex, FormatLogMessage(message), args);
            }
        }
        
        /// <summary>
        /// 格式化日志消息，统一添加Grain标识
        /// </summary>
        private string FormatLogMessage(string message)
        {
            var grainTypeName = GetGrainTypeName();
            return $"{grainTypeName}: {message}";
        }
        
        /// <summary>
        /// 获取当前Grain的类型名称
        /// </summary>
        protected string GetGrainTypeName()
        {
            return GetType().Name;
        }
    }

    #region 断路器实现

    /// <summary>
    /// 断路器状态
    /// </summary>
    public enum CircuitState
    {
        Closed,    // 关闭状态，正常操作
        Open,      // 打开状态，拒绝请求
        HalfOpen   // 半开状态，允许部分请求
    }

    /// <summary>
    /// 断路器模式实现
    /// </summary>
    [Serializable]
    public class CircuitBreaker
    {
        private readonly string _name;
        private readonly int _failureThreshold;
        private readonly TimeSpan _resetTimeout;
        private readonly ILogger _logger;
        private readonly object _stateLock = new object();
        private readonly Queue<DateTime> _failureTimes = new Queue<DateTime>();
        private Timer _resetTimer;
        private CircuitState _state = CircuitState.Closed;
        private int _halfOpenFailures = 0;
        private const int HalfOpenMaxAttempts = 3;

        /// <summary>
        /// 断路器状态
        /// </summary>
        public CircuitState State => _state;

        /// <summary>
        /// 构造函数
        /// </summary>
        public CircuitBreaker(string name, int failureThreshold, TimeSpan resetTimeout, ILogger logger)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _failureThreshold = failureThreshold;
            _resetTimeout = resetTimeout;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 执行受断路器保护的操作
        /// </summary>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            // 检查断路器状态
            if (_state == CircuitState.Open)
            {
                _logger.LogWarning("断路器 {CircuitName} 处于打开状态，拒绝请求", _name);
                throw new CircuitBreakerOpenException($"断路器 {_name} 处于打开状态，暂时无法处理请求");
            }

            try
            {
                T result = await operation();

                // 操作成功，重置断路器状态
                OnSuccess();

                return result;
            }
            catch (Exception ex)
            {
                // 操作失败，记录失败并可能打开断路器
                OnFailure();

                // 重新抛出异常
                throw;
            }
        }

        /// <summary>
        /// 操作成功处理
        /// </summary>
        private void OnSuccess()
        {
            lock (_stateLock)
            {
                if (_state == CircuitState.HalfOpen)
                {
                    _logger.LogInformation("断路器 {CircuitName} 从半开状态切换到关闭状态", _name);
                    _state = CircuitState.Closed;
                    _failureTimes.Clear();
                    _halfOpenFailures = 0;
                    StopResetTimer();
                }
            }
        }

        /// <summary>
        /// 操作失败处理
        /// </summary>
        private void OnFailure()
        {
            lock (_stateLock)
            {
                if (_state == CircuitState.Closed)
                {
                    // 记录失败时间
                    _failureTimes.Enqueue(DateTime.UtcNow);

                    // 移除超过时间窗口的失败记录
                    var cutoffTime = DateTime.UtcNow - _resetTimeout;
                    while (_failureTimes.Count > 0 && _failureTimes.Peek() < cutoffTime)
                    {
                        _failureTimes.Dequeue();
                    }

                    // 检查是否超过失败阈值
                    if (_failureTimes.Count >= _failureThreshold)
                    {
                        _logger.LogWarning("断路器 {CircuitName} 从关闭状态切换到打开状态，失败次数: {FailureCount}",
                            _name, _failureTimes.Count);
                        _state = CircuitState.Open;
                        StartResetTimer();
                    }
                }
                else if (_state == CircuitState.HalfOpen)
                {
                    _halfOpenFailures++;

                    if (_halfOpenFailures >= HalfOpenMaxAttempts)
                    {
                        _logger.LogWarning("断路器 {CircuitName} 从半开状态切换到打开状态，半开失败次数: {HalfOpenFailures}",
                            _name, _halfOpenFailures);
                        _state = CircuitState.Open;
                        _halfOpenFailures = 0;
                        StartResetTimer();
                    }
                }
            }
        }

        /// <summary>
        /// 启动重置计时器
        /// </summary>
        private void StartResetTimer()
        {
            StopResetTimer();
            _resetTimer = new Timer(ResetTimerCallback, null, _resetTimeout, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// 停止重置计时器
        /// </summary>
        private void StopResetTimer()
        {
            if (_resetTimer != null)
            {
                _resetTimer.Dispose();
                _resetTimer = null;
            }
        }

        /// <summary>
        /// 重置计时器回调
        /// </summary>
        private void ResetTimerCallback(object state)
        {
            lock (_stateLock)
            {
                if (_state == CircuitState.Open)
                {
                    _logger.LogInformation("断路器 {CircuitName} 从打开状态切换到半开状态", _name);
                    _state = CircuitState.HalfOpen;
                    _halfOpenFailures = 0;
                }
            }
        }
    }

    #endregion
}