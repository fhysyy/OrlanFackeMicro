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
    /// 创建SQL仓储实例，使用默认SQL数据库
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>SQL仓储实例</returns>
    ISqlRepository<TEntity, TKey> CreateSqlRepository<TEntity, TKey>() where TEntity : class, new();
    
    /// <summary>
    /// 创建SQL仓储实例，使用默认SQL数据库
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>SQL仓储实例</returns>
    Task<ISqlRepository<TEntity, TKey>> CreateSqlRepositoryAsync<TEntity, TKey>() where TEntity : class, new();
    
    /// <summary>
    /// 创建仓储实例，使用默认数据库类型，并通过键值路由到对应分片
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="key">用于路由的键值</param>
    /// <returns>仓储实例</returns>
    IRepository<TEntity, TKey> CreateRepository<TEntity, TKey>(TKey key) where TEntity : class, new();
    
    /// <summary>
    /// 创建仓储实例，使用默认数据库类型，并通过键值路由到对应分片
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="key">用于路由的键值</param>
    /// <returns>仓储实例</returns>
    Task<IRepository<TEntity, TKey>> CreateRepositoryAsync<TEntity, TKey>(TKey key) where TEntity : class, new();
    
    /// <summary>
    /// 创建SQL仓储实例，使用默认SQL数据库，并通过键值路由到对应分片
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="key">用于路由的键值</param>
    /// <returns>SQL仓储实例</returns>
    ISqlRepository<TEntity, TKey> CreateSqlRepository<TEntity, TKey>(TKey key) where TEntity : class, new();
    
    /// <summary>
    /// 创建SQL仓储实例，使用默认SQL数据库，并通过键值路由到对应分片
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="key">用于路由的键值</param>
    /// <returns>SQL仓储实例</returns>
    Task<ISqlRepository<TEntity, TKey>> CreateSqlRepositoryAsync<TEntity, TKey>(TKey key) where TEntity : class, new();
    
    /// <summary>
    /// 创建MongoDB仓储实例，使用默认MongoDB数据库
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>MongoDB仓储实例</returns>
    IMongoRepository<TEntity, TKey> CreateMongoRepository<TEntity, TKey>() where TEntity : class, new();
    
    /// <summary>
    /// 创建MongoDB仓储实例，使用默认MongoDB数据库
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>MongoDB仓储实例</returns>
    Task<IMongoRepository<TEntity, TKey>> CreateMongoRepositoryAsync<TEntity, TKey>() where TEntity : class, new();
    
    /// <summary>
    /// 创建MongoDB仓储实例，指定数据库名称
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="databaseName">数据库名称</param>
    /// <returns>MongoDB仓储实例</returns>
    IMongoRepository<TEntity, TKey> CreateMongoRepository<TEntity, TKey>(string? databaseName) where TEntity : class, new();
    
    /// <summary>
    /// 创建MongoDB仓储实例，指定数据库名称
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="databaseName">数据库名称</param>
    /// <returns>MongoDB仓储实例</returns>
    Task<IMongoRepository<TEntity, TKey>> CreateMongoRepositoryAsync<TEntity, TKey>(string? databaseName) where TEntity : class, new();
    
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

