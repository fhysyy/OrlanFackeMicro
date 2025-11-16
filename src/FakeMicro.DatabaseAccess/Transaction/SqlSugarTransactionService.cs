using FakeMicro.DatabaseAccess.Interfaces;
using SqlSugar;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Transaction;

// 自定义事务异常类
public class TransactionException : Exception
{
    public TransactionException(string message) : base(message) { }
    public TransactionException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// SqlSugar事务服务实现
/// 提供基于SqlSugar的事务管理功能，符合Orleans最佳实践
/// </summary>
public class SqlSugarTransactionService : ITransactionService
{
    private readonly ISqlSugarClient _sqlSugarClient;
    private readonly ILogger<SqlSugarTransactionService> _logger;
    
    // 使用AsyncLocal存储当前作用域的事务信息
    private static readonly AsyncLocal<bool> _isInTransaction = new AsyncLocal<bool>();
    private static readonly AsyncLocal<int> _transactionNestingLevel = new AsyncLocal<int>();

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="sqlSugarClient">SqlSugar客户端</param>
    /// <param name="logger">日志记录器</param>
    public SqlSugarTransactionService(
        ISqlSugarClient sqlSugarClient,
        ILogger<SqlSugarTransactionService> logger)
    {
        _sqlSugarClient = sqlSugarClient ?? throw new ArgumentNullException(nameof(sqlSugarClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 在事务中执行操作
    /// </summary>
    /// <typeparam name="TResult">操作结果类型</typeparam>
    /// <param name="action">在事务中执行的操作</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync(action, IsolationLevel.ReadCommitted, cancellationToken);
    }
    
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> action,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        // 检查是否已经在事务中
        var isNestedTransaction = _isInTransaction.Value;
        var initialNestingLevel = _transactionNestingLevel.Value;
        
        try
        {
            // 增加事务嵌套级别
            _transactionNestingLevel.Value = initialNestingLevel + 1;
            
            if (!isNestedTransaction)
            {
                _isInTransaction.Value = true;
                _logger.LogDebug("开始新事务，隔离级别: {IsolationLevel}", isolationLevel);
                
                // 使用SqlSugar内置的事务管理
                var result = await _sqlSugarClient.Ado.UseTranAsync(async () =>
                {
                    try
                    {
                        return await action();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "事务内部操作失败");
                        throw new TransactionException("事务内部操作失败", ex);
                    }
                });
                
                // 如果事务成功执行，返回action的结果
                if (result.IsSuccess)
                {
                    return result.Data;
                }
                else
                {
                    throw new TransactionException("事务执行失败");
                }
            }
            else
            {
                _logger.LogDebug("嵌套事务执行，嵌套级别: {NestingLevel}", _transactionNestingLevel.Value);
                // 嵌套事务中直接执行操作
                return await action();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("事务操作被取消");
            throw;
        }
        catch (TransactionException)
        {
            // 已经是TransactionException，直接抛出
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "事务执行失败");
            throw new TransactionException("事务执行失败", ex);
        }
        finally
        {
            // 减少嵌套级别
            _transactionNestingLevel.Value = initialNestingLevel;
            
            // 如果是最外层事务，清理事务标记
            if (!isNestedTransaction)
            {
                _isInTransaction.Value = false;
                _logger.LogDebug("事务完成");
            }
        }
    }
    
    /// <summary>
    /// 在事务中执行操作（无返回值）
    /// </summary>
    /// <param name="action">在事务中执行的操作</param>
    /// <returns>任务对象</returns>
    public async Task ExecuteInTransactionAsync(Func<Task> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));
            
        await ExecuteInTransactionAsync<object?>(async () =>
        {
            await action();
            return null;
        });
    }
    
    /// <summary>
    /// 在事务中执行操作（带取消令牌）
    /// </summary>
    /// <param name="action">在事务中执行的操作</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务对象</returns>
    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));
            
        await ExecuteInTransactionAsync<object?>(async () =>
        {
            await action();
            return null;
        }, cancellationToken);
    }
    
    // 重载版本，传入ISqlSugarClient参数
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<ISqlSugarClient, Task<TResult>> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteInTransactionAsync(() => action(_sqlSugarClient), isolationLevel, cancellationToken);
    }
    
    public async Task ExecuteInTransactionAsync(
        Func<Task> action,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));
            
        await ExecuteInTransactionAsync<object?>(async () =>
        {
            await action();
            return null;
        }, isolationLevel, cancellationToken);
    }
    
    // 重载版本，传入ISqlSugarClient参数
    public async Task ExecuteInTransactionAsync(
        Func<ISqlSugarClient, Task> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync<object?>(async () => { await action(_sqlSugarClient); return null; }, isolationLevel, cancellationToken);
    }
    
    // 检查当前是否在事务中
    public bool IsInTransaction => _isInTransaction.Value;
    
    // 获取当前事务嵌套级别
    public int TransactionNestingLevel => _transactionNestingLevel.Value;
}
