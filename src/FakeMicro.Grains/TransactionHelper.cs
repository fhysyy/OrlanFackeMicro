using FakeMicro.DatabaseAccess.Transaction;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using Orleans;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace FakeMicro.Grains;

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
        CancellationToken cancellationToken = default)
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
        CancellationToken cancellationToken = default)
    {
        var grainKey = grain.GetPrimaryKeyString();
        var grainType = grain.GetType().Name;
        var circuitName = $"Transaction_{grainType}";
        
        Func<Task<TResult>> transactionOperation = async () =>
        {
            try
            {
                // 获取日志记录器
                var logger = GetLoggerFromGrain(grain);
                
                logger?.LogInformation("开始Grain事务: GrainType={GrainType}, GrainKey={GrainKey}, OperationType={OperationType}", 
                    grainType, grainKey, operationType);
                
                if (transactionService is SqlSugarTransactionManager manager)
                {
                    return await manager.ExecuteInGrainTransactionAsync(
                        operation, grainKey, operationType, IsolationLevel.ReadCommitted, cancellationToken);
                }
                else
                {
                    // 回退到基础事务服务
                     await transactionService.ExecuteInTransactionAsync(operation);
                    return await operation();
                }
            }
            catch (Exception ex) when (ex is not TransactionException)
            {
                // 转换为统一的事务异常
                var transactionEx = new TransactionException($"Grain事务执行失败: {operationType}", ex);
                var logger = GetLoggerFromGrain(grain);
                logger?.LogError(transactionEx, "Grain事务执行失败: GrainType={GrainType}, GrainKey={GrainKey}, OperationType={OperationType}", 
                    grainType, grainKey, operationType);
                throw transactionEx;
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
        CancellationToken cancellationToken = default)
    {
        await ExecuteInGrainTransactionAsync<object?>(grain, transactionService, async () =>
        {
            await operation();
            return null;
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
        CancellationToken cancellationToken = default)
    {
        var grainKey = grain.GetPrimaryKeyString();
        var grainType = grain.GetType().Name;
        // 获取日志记录器
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
               ex is System.Net.Sockets.SocketException ||
               ex is System.IO.IOException ||
               ex is Orleans.Runtime.OrleansException ||
               ex is Orleans.Runtime.SiloUnavailableException ||
               IsRetryableDatabaseException(ex) ||
               ex is CircuitBreakerOpenException == false; // 断路器打开异常不可重试
    }

    /// <summary>
    /// 判断数据库异常是否可重试
    /// </summary>
    private static bool IsRetryableDatabaseException(Exception ex)
    {
        // 处理SQL Server异常
        if (ex is Microsoft.Data.SqlClient.SqlException sqlEx)
        {
            return IsRetryableSqlServerError(sqlEx);
        }
        
        // 处理PostgreSQL异常
        if (ex is Npgsql.NpgsqlException npgsqlEx)
        {
            return IsRetryablePostgresError(npgsqlEx);
        }
        
        return false;
    }

    /// <summary>
    /// 判断SQL Server错误是否可重试
    /// </summary>
    private static bool IsRetryableSqlServerError(Microsoft.Data.SqlClient.SqlException sqlEx)
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
    /// 判断PostgreSQL错误是否可重试
    /// </summary>
    private static bool IsRetryablePostgresError(Npgsql.NpgsqlException npgsqlEx)
    {
        // PostgreSQL可重试错误代码：
        // 40001: 序列化失败
        // 40P01: 死锁检测
        // 53000: 内存不足
        // 53200: 超出连接限制
        // 53300: 过多连接
        // 57P01: 管理员关闭连接
        // 57P02: 数据库关闭
        // 57P03: 无法连接到服务器
        // 58000: 系统错误
        // 58030: I/O错误
        
        var retryableErrors = new[] { "40001", "40P01", "53000", "53200", "53300", "57P01", "57P02", "57P03", "58000", "58030" };
        return retryableErrors.Contains(npgsqlEx.SqlState);
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
        CancellationToken cancellationToken = default)
    {
        var grainKey = grain.GetPrimaryKeyString();
        var grainType = grain.GetType().Name;
        // 获取日志记录器
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
    /// 从Grain获取Logger（优化版）
    /// </summary>
    private static ILogger? GetLoggerFromGrain(Grain grain)
    {
        // 优先检查是否继承自OrleansGrainBase（类型安全）
        if (grain is OrleansGrainBase baseGrain)
        {
            // 使用公共方法获取日志记录器
            return baseGrain.GetLogger();
        }
        
        // 尝试通过反射获取Logger字段（仅作为后备方案）
        try
        {
            var loggerField = grain.GetType().GetField("_logger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (loggerField != null)
            {
                return loggerField.GetValue(grain) as ILogger;
            }
        }
        catch (Exception ex)
        {
            // 反射失败时不抛出异常，仅返回null
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
        CancellationToken cancellationToken = default)
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
    public static System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<T>> Batch<T>(this System.Collections.Generic.IEnumerable<T> source, int batchSize)
    {
        var batch = new System.Collections.Generic.List<T>(batchSize);
        foreach (var item in source)
        {
            batch.Add(item);
            if (batch.Count >= batchSize)
            {
                yield return batch;
                batch = new System.Collections.Generic.List<T>(batchSize);
            }
        }
        
        if (batch.Count > 0)
            yield return batch;
    }
}