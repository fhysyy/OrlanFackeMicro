using System.Threading;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Interfaces;

/// <summary>
/// 事务上下文接口
/// </summary>
public interface ITransactionContext
{
    Task BeginAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 事务性仓储接口
/// </summary>
public interface ITransactionalRepository
{
    /// <summary>
    /// 执行事务
    /// </summary>
    /// <param name="action">事务内执行的操作</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// 执行事务并返回结果
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="action">事务内执行的操作</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken = default);
}
