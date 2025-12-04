using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Interfaces;

/// <summary>
/// 仓储工厂接口，用于创建不同数据库类型的仓储实例
/// </summary>
public interface IRepositoryFactory
{
    /// <summary>
    /// 创建仓储实例，使用默认数据库类型
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>仓储实例</returns>
    IRepository<TEntity, TKey> CreateRepository<TEntity, TKey>() where TEntity : class, new();
    
    /// <summary>
    /// 创建仓储实例，使用默认数据库类型
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>仓储实例</returns>
    Task<IRepository<TEntity, TKey>> CreateRepositoryAsync<TEntity, TKey>() where TEntity : class, new();
    
    /// <summary>
    /// 创建仓储实例，动态指定数据库类型
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="databaseType">数据库类型</param>
    /// <returns>仓储实例</returns>
    IRepository<TEntity, TKey> CreateRepository<TEntity, TKey>(DatabaseType databaseType) where TEntity : class, new();
    
    /// <summary>
    /// 创建仓储实例，动态指定数据库类型
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="databaseType">数据库类型</param>
    /// <returns>仓储实例</returns>
    Task<IRepository<TEntity, TKey>> CreateRepositoryAsync<TEntity, TKey>(DatabaseType databaseType) where TEntity : class, new();
    
    /// <summary>
    /// 注册仓储创建策略
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="databaseType">数据库类型</param>
    /// <param name="strategy">创建策略</param>
    void RegisterStrategy<TEntity, TKey>(DatabaseType databaseType, IRepositoryCreationStrategy<TEntity, TKey> strategy) where TEntity : class, new();
    
    /// <summary>
    /// 检查是否支持指定的数据库类型
    /// </summary>
    /// <param name="databaseType">数据库类型</param>
    /// <returns>是否支持</returns>
    bool IsDatabaseTypeSupported(DatabaseType databaseType);
}

/// <summary>
/// 仓储创建策略接口，用于实现不同数据库类型的仓储创建逻辑
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public interface IRepositoryCreationStrategy<TEntity, TKey> where TEntity : class, new()
{
    /// <summary>
    /// 获取该策略支持的数据库类型
    /// </summary>
    /// <returns>数据库类型</returns>
    DatabaseType GetDatabaseType();
    
    /// <summary>
    /// 创建仓储实例
    /// </summary>
    /// <returns>仓储实例</returns>
    IRepository<TEntity, TKey> CreateRepository();
    
    /// <summary>
    /// 异步创建仓储实例
    /// </summary>
    /// <returns>仓储实例</returns>
    Task<IRepository<TEntity, TKey>> CreateRepositoryAsync();
}

