using FakeMicro.DatabaseAccess.Interfaces;
using SqlSugar;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace FakeMicro.DatabaseAccess.Transaction;

/// <summary>
/// SqlSugar高级事务管理器
/// 提供Orleans环境下的事务管理最佳实践
/// </summary>
public class SqlSugarTransactionManager : ITransactionService
{
    private readonly ISqlSugarClient _sqlSugarClient;
    private readonly ILogger<SqlSugarTransactionManager> _logger;
    
    // 事务上下文信息
    private readonly AsyncLocal<TransactionContext> _transactionContext = new AsyncLocal<TransactionContext>();
    
    // 事务统计信息
    private long _totalTransactions = 0;
    private long _failedTransactions = 0;
    private readonly ConcurrentDictionary<string, long> _transactionMetrics = new ConcurrentDictionary<string, long>();

    /// <summary>
    /// 事务上下文类
    /// </summary>
    private class TransactionContext
    {
        public bool IsInTransaction { get; set; }
        public int NestingLevel { get; set; }
        public IsolationLevel IsolationLevel { get; set; }
        public DateTime StartTime { get; set; }
        public string TransactionId { get; set; } = Guid.NewGuid().ToString();
        public string? GrainKey { get; set; }
        public string? OperationType { get; set; }
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public SqlSugarTransactionManager(
        ISqlSugarClient sqlSugarClient,
        ILogger<SqlSugarTransactionManager> logger)
    {
        _sqlSugarClient = sqlSugarClient ?? throw new ArgumentNullException(nameof(sqlSugarClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 在事务中执行操作（基本版本）
    /// </summary>
    public async Task ExecuteInTransactionAsync(Func<Task> action)
    {
        await ExecuteInTransactionAsync(action, IsolationLevel.ReadCommitted, null, null);
    }

    /// <summary>
    /// 在事务中执行操作（完整版本）
    /// </summary>
    public async Task ExecuteInTransactionAsync(
        Func<Task> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        string? grainKey = null,
        string? operationType = null)
    {
        await ExecuteInTransactionAsync<object?>(async () =>
        {
            await action();
            return null;
        }, isolationLevel, grainKey, operationType);
    }

    /// <summary>
    /// 在事务中执行操作并返回结果
    /// </summary>
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        string? grainKey = null,
        string? operationType = null)
    {
        return await ExecuteInTransactionAsyncInternal(action, isolationLevel, grainKey, operationType, CancellationToken.None);
    }

    /// <summary>
    /// 在事务中执行操作（带取消令牌）
    /// </summary>
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> action,
        CancellationToken cancellationToken,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        string? grainKey = null,
        string? operationType = null)
    {
        return await ExecuteInTransactionAsyncInternal(action, isolationLevel, grainKey, operationType, cancellationToken);
    }

    /// <summary>
    /// 内部事务执行逻辑
    /// </summary>
    private async Task<TResult> ExecuteInTransactionAsyncInternal<TResult>(
        Func<Task<TResult>> action,
        IsolationLevel isolationLevel,
        string? grainKey,
        string? operationType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var currentContext = _transactionContext.Value;
        var isNestedTransaction = currentContext?.IsInTransaction == true;
        var transactionId = Guid.NewGuid().ToString();
        
        // 更新事务统计
        Interlocked.Increment(ref _totalTransactions);
        
        // 创建新的事务上下文（如果需要）
        var context = isNestedTransaction ? currentContext : new TransactionContext
        {
            IsolationLevel = isolationLevel,
            StartTime = DateTime.UtcNow,
            GrainKey = grainKey,
            OperationType = operationType
        };
        
        if (!isNestedTransaction)
        {
            _transactionContext.Value = context;
        }
        
        try
        {
            TResult result;
            
            if (!isNestedTransaction)
            {
                // 新事务：使用SqlSugar内置事务管理
                context.IsInTransaction = true;
                
                _logger.LogInformation("开始新事务: TransactionId={TransactionId}, GrainKey={GrainKey}, OperationType={OperationType}, IsolationLevel={IsolationLevel}", 
                    transactionId, grainKey, operationType, isolationLevel);
                
                // 使用SqlSugar的事务方法，修复CS1662错误
                var transactionResult = await _sqlSugarClient.Ado.UseTranAsync(async () =>
                {
                    try
                    {
                        return await action();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "事务内部操作失败: TransactionId={TransactionId}", transactionId);
                        throw;
                    }
                });
                result = transactionResult.Data;
                
                _logger.LogInformation("事务成功提交: TransactionId={TransactionId}, Duration={Duration}ms", 
                    transactionId, (DateTime.UtcNow - context.StartTime).TotalMilliseconds);
            }
            else
            {
                // 嵌套事务：直接执行操作，由外层事务管理
                _logger.LogDebug("嵌套事务执行: NestingLevel={NestingLevel}, GrainKey={GrainKey}", 
                    context.NestingLevel, grainKey);
                
                result = await action();
            }
            
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("事务操作被取消: TransactionId={TransactionId}", transactionId);
            Interlocked.Increment(ref _failedTransactions);
            throw;
        }
        catch (TransactionException)
        {
            // 已经是TransactionException，直接抛出
            Interlocked.Increment(ref _failedTransactions);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "事务执行失败: TransactionId={TransactionId}", transactionId);
            Interlocked.Increment(ref _failedTransactions);
            
            // 记录操作类型统计
            if (!string.IsNullOrEmpty(operationType))
            {
                _transactionMetrics.AddOrUpdate(operationType, 1, (key, oldValue) => oldValue + 1);
            }
            
            throw new TransactionException($"事务执行失败: {ex.Message}", ex);
        }
        finally
        {
            if (!isNestedTransaction)
            {
                _transactionContext.Value = default;
            }
        }
    }

    /// <summary>
    /// 检查当前是否在事务中
    /// </summary>
    public bool IsInTransaction => _transactionContext.Value?.IsInTransaction == true;

    /// <summary>
    /// 获取当前事务嵌套级别
    /// </summary>
    public int TransactionNestingLevel => _transactionContext.Value?.NestingLevel ?? 0;

    /// <summary>
    /// 获取事务统计信息
    /// </summary>
    public TransactionStatistics GetTransactionStatistics()
    {
        return new TransactionStatistics
        {
            TotalTransactions = Interlocked.Read(ref _totalTransactions),
            FailedTransactions = Interlocked.Read(ref _failedTransactions),
            SuccessRate = _totalTransactions == 0 ? 0 : (1.0 - (double)_failedTransactions / _totalTransactions) * 100,
            OperationMetrics = new ConcurrentDictionary<string, long>(_transactionMetrics)
        };
    }

    /// <summary>
    /// 重置事务统计
    /// </summary>
    public void ResetStatistics()
    {
        Interlocked.Exchange(ref _totalTransactions, 0);
        Interlocked.Exchange(ref _failedTransactions, 0);
        _transactionMetrics.Clear();
    }

    /// <summary>
    /// 创建Grain级别的事务范围
    /// </summary>
    public async Task<TResult> ExecuteInGrainTransactionAsync<TResult>(
        Func<Task<TResult>> action,
        string grainKey,
        string operationType,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsyncInternal(
            action, isolationLevel, grainKey, operationType, cancellationToken);
    }

    /// <summary>
    /// 创建批量操作的事务范围
    /// </summary>
    public async Task ExecuteBatchInTransactionAsync<T>(
        IEnumerable<T> items,
        Func<T, Task> operation,
        int batchSize = 100,
        string? grainKey = null,
        CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync(async () =>
        {
            var batchCount = 0;
            var totalCount = 0;
            
            foreach (var batch in items.Batch(batchSize))
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var tasks = batch.Select(item => operation(item));
                await Task.WhenAll(tasks);
                
                batchCount++;
                totalCount += batch.Count();
                
                _logger.LogDebug("批量操作进度: Batch={BatchCount}, Total={TotalCount}", batchCount, totalCount);
            }
        }, IsolationLevel.ReadCommitted, grainKey, "BatchOperation");
    }
}

/// <summary>
/// 事务统计信息
/// </summary>
public class TransactionStatistics
{
    public long TotalTransactions { get; set; }
    public long FailedTransactions { get; set; }
    public double SuccessRate { get; set; }
    public ConcurrentDictionary<string, long> OperationMetrics { get; set; } = new ConcurrentDictionary<string, long>();
}

/// <summary>
/// 批量操作扩展方法
/// </summary>
public static class EnumerableExtensions
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