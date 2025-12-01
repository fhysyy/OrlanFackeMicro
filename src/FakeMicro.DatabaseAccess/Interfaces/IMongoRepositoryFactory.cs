using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Interfaces;

/// <summary>
/// MongoDB仓储工厂接口
/// 用于创建MongoDB仓储实例
/// </summary>
public interface IMongoRepositoryFactory : IRepositoryFactory
{
    /// <summary>
    /// 创建MongoDB仓储实例
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>MongoDB仓储实例</returns>
    IMongoRepository<TEntity, TKey> CreateRepository<TEntity, TKey>() where TEntity : class;

    /// <summary>
    /// 创建MongoDB仓储实例（异步）
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>MongoDB仓储实例</returns>
    Task<IMongoRepository<TEntity, TKey>> CreateRepositoryAsync<TEntity, TKey>() where TEntity : class;
}
