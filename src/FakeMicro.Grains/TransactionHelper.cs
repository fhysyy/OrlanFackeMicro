using FakeMicro.DatabaseAccess.Transaction;
using FakeMicro.DatabaseAccess.Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace FakeMicro.Grains;

/// <summary>
/// 断路器状态
/// </summary>
public enum CircuitState
{
    /// <summary>
    /// 正常状态，允许请求
    /// </summary>
    Closed,
    /// <summary>
    /// 熔断状态，拒绝请求
    /// </summary>
    Open,
    /// <summary>
    /// 半开状态，允许部分请求以测试服务是否恢复
    /// </summary>
    HalfOpen
}

/// <summary>
/// 断路器模式实现
/// </summary>
public class CircuitBreaker
{
    private readonly string _name;
    private readonly int _failureThreshold;
    private readonly TimeSpan _resetTimeout;
    private readonly ILogger _logger;
    private readonly object _stateLock = new object();
    private readonly ConcurrentQueue<DateTime> _failureTimes = new ConcurrentQueue<DateTime>();
    private System.Threading.Timer _resetTimer;
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
    /// <param name="name">断路器名称</param>
    /// <param name="failureThreshold">失败阈值</param>
    /// <param name="resetTimeout">重置超时时间</param>
    /// <param name="logger">日志记录器</param>
    public CircuitBreaker(string name, int failureThreshold, TimeSpan resetTimeout, ILogger logger)
    {
        _name = name;
        _failureThreshold = failureThreshold;
        _resetTimeout = resetTimeout;
        _logger = logger;
    }

    /// <summary>
    /// 尝试执行操作
    /// </summary>
    /// <typeparam name="TResult">操作结果类型</typeparam>
    /// <param name="operation">要执行的操作</param>
    /// <returns>操作结果</returns>
    public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> operation)
    {
        if (!ShouldAllowRequest())
        {
            throw new CircuitBreakerOpenException($"断路器 {_name} 处于打开状态，拒绝请求");
        }

        try
        {
            var result = await operation();
            OnSuccess();
            return result;
        }
        catch (Exception ex)
        {
            OnFailure();
            throw;
        }
    }

    /// <summary>
    /// 判断是否允许请求
    /// </summary>
    private bool ShouldAllowRequest()
    {
        lock (_stateLock)
        {
            switch (_state)
            {
                case CircuitState.Closed:
                    return true;
                case CircuitState.Open:
                    return false;
                case CircuitState.HalfOpen:
                    return true;
                default:
                    return true;
            }
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
                while (_failureTimes.TryPeek(out var time) && time < cutoffTime)
                {
                    _failureTimes.TryDequeue(out _);
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
    /// 启动重置定时器
    /// </summary>
    private void StartResetTimer()
    {
        StopResetTimer();
        
        _resetTimer = new System.Threading.Timer(_ =>
        {
            lock (_stateLock)
            {
                _logger.LogInformation("断路器 {CircuitName} 从打开状态切换到半开状态", _name);
                _state = CircuitState.HalfOpen;
                _halfOpenFailures = 0;
            }
        }, null, _resetTimeout, Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// 停止重置定时器
    /// </summary>
    private void StopResetTimer()
    {
        if (_resetTimer != null)
        {
            _resetTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _resetTimer.Dispose();
            _resetTimer = null;
        }
    }
}

/// <summary>
/// 断路器打开异常
/// </summary>
public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException(string message) : base(message) { }
}

/// <summary>
/// 断路器管理器
/// </summary>
public static class CircuitBreakerManager
{
    private static readonly ConcurrentDictionary<string, CircuitBreaker> _circuitBreakers = new ConcurrentDictionary<string, CircuitBreaker>();
    private static ILoggerFactory _loggerFactory;

    /// <summary>
    /// 初始化断路器管理器
    /// </summary>
    /// <param name="loggerFactory">日志工厂</param>
    public static void Initialize(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// 获取或创建断路器
    /// </summary>
    /// <param name="name">断路器名称</param>
    /// <param name="failureThreshold">失败阈值</param>
    /// <param name="resetTimeout">重置超时时间</param>
    /// <returns>断路器实例</returns>
    public static CircuitBreaker GetOrCreateCircuitBreaker(string name, int failureThreshold = 5, TimeSpan? resetTimeout = null)
    {
        if (_loggerFactory == null)
        {
            throw new InvalidOperationException("CircuitBreakerManager 尚未初始化");
        }

        return _circuitBreakers.GetOrAdd(name, key =>
        {
            var logger = _loggerFactory.CreateLogger<CircuitBreaker>();
            return new CircuitBreaker(key, failureThreshold, resetTimeout ?? TimeSpan.FromSeconds(30), logger);
        });
    }
}

/// <summary>
/// Grain事务辅助类
/// 提供Orleans Grain环境下的事务管理最佳实践
/// </summary>
public static class TransactionHelper
{
    /// <summary>
    /// 在Grain事务中执行操作
    /// </summary>
    /// <typeparam name="TResult">操作结果类型</typeparam>
    /// <param name="grain">当前Grain实例</param>
    /// <param name="transactionService">事务服务</param>
    /// <param name="operation">要执行的操作</param>
    /// <param name="operationType">操作类型描述</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    public static async Task<TResult> ExecuteInGrainTransactionAsync<TResult>(
        this Grain grain,
        ITransactionService transactionService,
        Func<Task<TResult>> operation,
        string operationType,
        System.Threading.CancellationToken cancellationToken = default)
    {
        return await ExecuteInGrainTransactionAsync(grain, transactionService, operation, operationType, false, cancellationToken);
    }

    /// <summary>
    /// 在Grain事务中执行操作（带断路器）
    /// </summary>
    /// <typeparam name="TResult">操作结果类型</typeparam>
    /// <param name="grain">当前Grain实例</param>
    /// <param name="transactionService">事务服务</param>
    /// <param name="operation">要执行的操作</param>
    /// <param name="operationType">操作类型描述</param>
    /// <param name="useCircuitBreaker">是否使用断路器</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    public static async Task<TResult> ExecuteInGrainTransactionAsync<TResult>(
        this Grain grain,
        ITransactionService transactionService,
        Func<Task<TResult>> operation,
        string operationType,
        bool useCircuitBreaker,
        System.Threading.CancellationToken cancellationToken = default)
    {
        var grainKey = grain.GetPrimaryKeyString();
        var grainType = grain.GetType().Name;
        var circuitName = $"Transaction_{grainType}";
        
        Func<Task<TResult>> transactionOperation = async () =>
        {
            try
            {
                // Orleans 9.0 适配：通过构造函数注入获取Logger
                var logger = GetLoggerFromGrain(grain);
                
                logger?.LogInformation("开始Grain事务: GrainType={GrainType}, GrainKey={GrainKey}, OperationType={OperationType}", 
                    grainType, grainKey, operationType);
                
                if (transactionService is SqlSugarTransactionManager manager)
                {
                    return await manager.ExecuteInGrainTransactionAsync(
                        operation, grainKey, operationType, System.Data.IsolationLevel.ReadCommitted, cancellationToken);
                }
                else
                {
                    // 回退到基础事务服务
                     await transactionService.ExecuteInTransactionAsync(operation);
                    return await operation();
                }
            }
            catch (TransactionException ex)
            {
                var logger = GetLoggerFromGrain(grain);
                logger?.LogError(ex, "Grain事务执行失败: GrainType={GrainType}, GrainKey={GrainKey}, OperationType={OperationType}", 
                    grainType, grainKey, operationType);
                throw;
            }
        };
        
        if (useCircuitBreaker)
        {
            return await grain.ExecuteWithCircuitBreakerAsync(transactionOperation, operationType, circuitName, cancellationToken: cancellationToken);
        }
        else
        {
            return await grain.ExecuteWithRetryAsync(transactionOperation, operationType, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// 在Grain事务中执行操作（无返回值）
    /// </summary>
    public static async Task ExecuteInGrainTransactionAsync(
        this Grain grain,
        ITransactionService transactionService,
        Func<Task> operation,
        string operationType,
        System.Threading.CancellationToken cancellationToken = default)
    {
        await ExecuteInGrainTransactionAsync<object?>(grain, transactionService, async () =>
        {
            await operation();
            return (object?)null;
        }, operationType, cancellationToken);
    }

    /// <summary>
    /// 安全执行Grain操作（带重试机制）
    /// </summary>
    public static async Task<TResult> ExecuteWithRetryAsync<TResult>(
        this Grain grain,
        Func<Task<TResult>> operation,
        string operationType,
        int maxRetries = 3,
        TimeSpan? delay = null,
        System.Threading.CancellationToken cancellationToken = default)
    {
        var grainKey = grain.GetPrimaryKeyString();
        var grainType = grain.GetType().Name;
        // Orleans 9.0 适配：通过构造函数注入获取Logger
        var logger = GetLoggerFromGrain(grain);
        
        delay ??= TimeSpan.FromMilliseconds(100);
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                logger?.LogInformation("执行Grain操作: GrainType={GrainType}, GrainKey={GrainKey}, OperationType={OperationType}, Attempt={Attempt}", 
                    grainType, grainKey, operationType, attempt);
                
                return await operation();
            }
            catch (Exception ex) when (attempt < maxRetries && IsRetryableException(ex))
            {
                logger?.LogWarning(ex, "Grain操作失败，准备重试: GrainType={GrainType}, GrainKey={GrainKey}, OperationType={OperationType}, Attempt={Attempt}", 
                    grainType, grainKey, operationType, attempt);
                
                await Task.Delay(delay.Value, cancellationToken);
                delay = TimeSpan.FromMilliseconds(delay.Value.TotalMilliseconds * 2); // 指数退避
            }
        }
        
        // 最后一次尝试，如果失败则直接抛出异常
        logger?.LogInformation("最后一次尝试Grain操作: GrainType={GrainType}, GrainKey={GrainKey}, OperationType={OperationType}, Attempt={MaxRetries}", 
            grainType, grainKey, operationType, maxRetries);
        
        return await operation();
    }

    /// <summary>
    /// 判断异常是否可重试
    /// </summary>
    private static bool IsRetryableException(Exception ex)
    {
        // 以下情况可重试：
        // 1. 数据库连接超时
        // 2. 死锁
        // 3. 网络问题
        // 4. 暂时性错误
        // 5. 断路器半开状态下的失败
        
        return ex is TimeoutException ||
               ex is Microsoft.Data.SqlClient.SqlException sqlEx && IsRetryableSqlError(sqlEx) ||
               ex is System.Net.Sockets.SocketException ||
               ex is System.IO.IOException ||
               ex is Orleans.Runtime.OrleansException ||
               ex is Orleans.Runtime.SiloUnavailableException ||
               ex is CircuitBreakerOpenException == false; // 断路器打开异常不可重试
    }

    /// <summary>
    /// 判断SQL错误是否可重试
    /// </summary>
    private static bool IsRetryableSqlError(Microsoft.Data.SqlClient.SqlException sqlEx)
    {
        // SQL Server可重试错误代码：
        // 1205: 死锁
        // -2: 超时
        // 4060: 数据库不可用
        // 18456: 登录失败（可能是暂时的）
        // 40197: 可用性组失败
        // 40613: 数据库镜像/可用性组断开连接
        // 10054: TCP连接重置
        // 10060: TCP连接超时
        // 10061: 连接被拒绝
        // 40053: 连接限制超过
        // 40143: 服务器不可用
        // 40198: 服务已暂停
        
        var retryableErrors = new[] { 1205, -2, 4060, 18456, 40197, 40613, 10054, 10060, 10061, 40053, 40143, 40198 };
        return retryableErrors.Contains(sqlEx.Number);
    }

    /// <summary>
    /// 批量执行Grain操作
    /// </summary>
    public static async Task ExecuteBatchAsync<T>(
        this Grain grain,
        ITransactionService transactionService,
        System.Collections.Generic.IEnumerable<T> items,
        Func<T, Task> operation,
        string operationType,
        int batchSize = 100,
        System.Threading.CancellationToken cancellationToken = default)
    {
        var grainKey = grain.GetPrimaryKeyString();
        var grainType = grain.GetType().Name;
        // Orleans 9.0 适配：通过构造函数注入获取Logger
        var logger = GetLoggerFromGrain(grain);
        
        logger?.LogInformation("开始批量Grain操作: GrainType={GrainType}, GrainKey={GrainKey}, OperationType={OperationType}, TotalItems={TotalItems}, BatchSize={BatchSize}", 
            grainType, grainKey, operationType, items.Count(), batchSize);
        
        if (transactionService is SqlSugarTransactionManager manager)
        {
            await manager.ExecuteBatchInTransactionAsync(items, operation, batchSize, grainKey, cancellationToken);
        }
        else
        {
            // 基础批量操作实现
            var batchCount = 0;
            var totalCount = 0;
            
            foreach (var batch in items.Batch(batchSize))
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var tasks = batch.Select(item => operation(item));
                await Task.WhenAll(tasks);
                
                batchCount++;
                totalCount += batch.Count();
                
                logger?.LogDebug("批量操作进度: Batch={BatchCount}, Total={TotalCount}", batchCount, totalCount);
            }
        }
        
        logger?.LogInformation("批量Grain操作完成: GrainType={GrainType}, GrainKey={GrainKey}, OperationType={OperationType}, TotalItems={TotalItems}", 
            grainType, grainKey, operationType, items.Count());
    }

    /// <summary>
    /// 获取事务统计信息
    /// </summary>
    public static TransactionStatistics? GetTransactionStatistics(this ITransactionService transactionService)
    {
        if (transactionService is SqlSugarTransactionManager manager)
        {
            return manager.GetTransactionStatistics();
        }
        return null;
    }

    /// <summary>
    /// 从Grain获取Logger（Orleans 9.0适配）
    /// </summary>
    private static ILogger? GetLoggerFromGrain(Grain grain)
    {
        // 检查Grain是否继承自OrleansGrainBase
        if (grain is OrleansGrainBase baseGrain)
        {
            // 使用反射获取Logger字段
            var loggerField = baseGrain.GetType().GetField("_logger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (loggerField != null)
            {
                return loggerField.GetValue(baseGrain) as ILogger;
            }
        }
        
        // 尝试通过反射获取其他可能的Logger字段
        var loggerFieldInfo = grain.GetType().GetField("_logger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (loggerFieldInfo != null)
        {
            return loggerFieldInfo.GetValue(grain) as ILogger;
        }
        
        return null;
    }

    /// <summary>
    /// 安全执行Grain操作（带断路器和重试机制）
    /// </summary>
    public static async Task<TResult> ExecuteWithCircuitBreakerAsync<TResult>(
        this Grain grain,
        Func<Task<TResult>> operation,
        string operationType,
        string circuitName,
        int maxRetries = 3,
        TimeSpan? delay = null,
        System.Threading.CancellationToken cancellationToken = default)
    {
        var logger = GetLoggerFromGrain(grain);
        var circuitBreaker = CircuitBreakerManager.GetOrCreateCircuitBreaker(circuitName);
        
        try
        {
            return await circuitBreaker.ExecuteAsync(async () =>
            {
                return await grain.ExecuteWithRetryAsync(operation, operationType, maxRetries, delay, cancellationToken);
            });
        }
        catch (CircuitBreakerOpenException ex)
        {
            logger?.LogWarning(ex, "断路器打开，拒绝Grain操作: GrainType={GrainType}, GrainKey={GrainKey}, OperationType={OperationType}, CircuitName={CircuitName}",
                grain.GetType().Name, grain.GetPrimaryKeyString(), operationType, circuitName);
            throw;
        }
    }
}

/// <summary>
/// 批量操作扩展方法
/// </summary>
internal static class EnumerableBatchExtensions
{
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        var batch = new List<T>(batchSize);
        foreach (var item in source)
        {
            batch.Add(item);
            if (batch.Count >= batchSize)
            {
                yield return batch;
                batch = new List<T>(batchSize);
            }
        }
        
        if (batch.Count > 0)
            yield return batch;
    }
}