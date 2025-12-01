using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Interfaces;

/// <summary>
/// 通用仓储工厂接口
/// 用于创建不同类型的仓储实例
/// </summary>
public interface IRepositoryFactory
{
    /// <summary>
    /// 创建通用仓储实例
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>通用仓储实例</returns>
    IRepository<TEntity, TKey> CreateRepository<TEntity, TKey>() where TEntity : class;

    /// <summary>
    /// 创建通用仓储实例（异步）
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>通用仓储实例</returns>
    Task<IRepository<TEntity, TKey>> CreateRepositoryAsync<TEntity, TKey>() where TEntity : class;
}