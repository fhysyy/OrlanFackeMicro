using FakeMicro.DatabaseAccess.Transaction;
using FakeMicro.DatabaseAccess.Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using System;
using System.Threading.Tasks;

namespace FakeMicro.Grains;

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
        var grainKey = grain.GetPrimaryKeyString();
        var grainType = grain.GetType().Name;
        
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
        
        return ex is TimeoutException ||
               ex is Microsoft.Data.SqlClient.SqlException sqlEx && IsRetryableSqlError(sqlEx) ||
               ex is System.Net.Sockets.SocketException ||
               ex is System.IO.IOException;
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
        
        var retryableErrors = new[] { 1205, -2, 4060, 18456 };
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
        // Orleans 9.0 中，Grain类不再直接暴露ServiceProvider
        // 需要通过反射或其他方式获取Logger，或者让Grain继承自具有Logger的基类
        // 这里提供一个空实现，具体实现需要根据项目结构调整
        return null;
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