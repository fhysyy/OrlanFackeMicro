using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FakeMicro.DatabaseAccess.Interfaces;

/// <summary>
/// 事务上下文接口
/// </summary>
public interface ITransactionContext
{
    Task BeginAsync();
    Task CommitAsync();
    Task RollbackAsync();
}

/// <summary>
/// 事务性仓储接口
/// </summary>
public interface ITransactionalRepository<TEntity, TKey> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TKey id);
    Task AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(TEntity entity);
    Task SaveChangesAsync();
}

/// <summary>
/// 事务性仓储实现
/// </summary>
public class TransactionalRepository<TEntity, TKey> : ITransactionalRepository<TEntity, TKey> 
    where TEntity : class
{
    private readonly ITransactionContext _transactionContext;
    private readonly IRepository<TEntity, TKey> _repository;
    private readonly ILogger<TransactionalRepository<TEntity, TKey>> _logger;

    public TransactionalRepository(
        ITransactionContext transactionContext,
        IRepository<TEntity, TKey> repository,
        ILogger<TransactionalRepository<TEntity, TKey>> logger)
    {
        _transactionContext = transactionContext ?? throw new ArgumentNullException(nameof(transactionContext));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task AddAsync(TEntity entity)
    {
        await _repository.AddAsync(entity);
    }

    public async Task DeleteAsync(TEntity entity)
    {
        await _repository.DeleteAsync(entity);
    }

    public async Task<TEntity?> GetByIdAsync(TKey id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task SaveChangesAsync()
    {
        await _repository.SaveChangesAsync();
    }

    public async Task UpdateAsync(TEntity entity)
    {
        await _repository.UpdateAsync(entity);
    }
}
