using System;
using System.Threading;
using System.Threading.Tasks;
using FakeMicro.DatabaseAccess.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// MongoDB仓储工厂
/// 用于创建MongoDB仓储实例
/// </summary>
public class MongoRepositoryFactory : IMongoRepositoryFactory
{
    private readonly MongoClient _mongoClient;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="mongoClient">MongoDB客户端</param>
    /// <param name="loggerFactory">日志工厂</param>
    public MongoRepositoryFactory(MongoClient mongoClient, ILoggerFactory loggerFactory)
    {
        _mongoClient = mongoClient ?? throw new ArgumentNullException(nameof(mongoClient));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// 创建MongoDB仓储实例
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>MongoDB仓储实例</returns>
    public IMongoRepository<TEntity, TKey> CreateRepository<TEntity, TKey>() where TEntity : class
    {
        return CreateRepositoryInternal<TEntity, TKey>(null);
    }

    /// <summary>
    /// 创建MongoDB仓储实例（带连接字符串名称）
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="connectionStringName">连接字符串名称</param>
    /// <returns>MongoDB仓储实例</returns>
    public IMongoRepository<TEntity, TKey> CreateRepository<TEntity, TKey>(string? connectionStringName = null) where TEntity : class
    {
        return CreateRepositoryInternal<TEntity, TKey>(connectionStringName);
    }

    /// <summary>
    /// 创建MongoDB仓储实例（异步）
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>MongoDB仓储实例</returns>
    public Task<IMongoRepository<TEntity, TKey>> CreateRepositoryAsync<TEntity, TKey>() where TEntity : class
    {
        return CreateRepositoryAsyncInternal<TEntity, TKey>(null, null, CancellationToken.None);
    }

    /// <summary>
    /// 创建MongoDB仓储实例（异步，带数据库名称）
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="databaseName">数据库名称</param>
    /// <returns>MongoDB仓储实例</returns>
    public Task<IMongoRepository<TEntity, TKey>> CreateRepositoryAsync<TEntity, TKey>(string? databaseName) where TEntity : class
    {
        return CreateRepositoryAsyncInternal<TEntity, TKey>(null, databaseName, CancellationToken.None);
    }

    /// <summary>
    /// 创建MongoDB仓储实例（异步，带连接字符串名称）
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="connectionStringName">连接字符串名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>MongoDB仓储实例</returns>
    public Task<IMongoRepository<TEntity, TKey>> CreateRepositoryAsync<TEntity, TKey>(string? connectionStringName = null, CancellationToken cancellationToken = default) where TEntity : class
    {
        return CreateRepositoryAsyncInternal<TEntity, TKey>(connectionStringName, null, cancellationToken);
    }

    /// <summary>
    /// 创建MongoDB仓储实例（异步，带连接字符串名称和数据库名称）
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="connectionStringName">连接字符串名称</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>MongoDB仓储实例</returns>
    public Task<IMongoRepository<TEntity, TKey>> CreateRepositoryAsync<TEntity, TKey>(string? connectionStringName, string? databaseName, CancellationToken cancellationToken = default) where TEntity : class
    {
        return CreateRepositoryAsyncInternal<TEntity, TKey>(connectionStringName, databaseName, cancellationToken);
    }

    /// <summary>
    /// 内部创建MongoDB仓储实例
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="connectionStringName">连接字符串名称</param>
    /// <returns>MongoDB仓储实例</returns>
    private IMongoRepository<TEntity, TKey> CreateRepositoryInternal<TEntity, TKey>(string? connectionStringName = null) where TEntity : class
    {
        var logger = _loggerFactory.CreateLogger<MongoRepository<TEntity, TKey>>();
        return new MongoRepository<TEntity, TKey>(_mongoClient, logger);
    }

    /// <summary>
    /// 内部创建MongoDB仓储实例（异步）
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="connectionStringName">连接字符串名称</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>MongoDB仓储实例</returns>
    private Task<IMongoRepository<TEntity, TKey>> CreateRepositoryAsyncInternal<TEntity, TKey>(string? connectionStringName = null, string? databaseName = null, CancellationToken cancellationToken = default) where TEntity : class
    {
        var logger = _loggerFactory.CreateLogger<MongoRepository<TEntity, TKey>>();
        IMongoRepository<TEntity, TKey> repository = new MongoRepository<TEntity, TKey>(_mongoClient, logger, databaseName);
        return Task.FromResult(repository);
    }

    #region IRepositoryFactory Implementation
    // 以下方法由DynamicRepositoryFactory统一实现，MongoRepositoryFactory只专注于MongoDB
    // 显式实现IRepositoryFactory接口的方法，委托给DynamicRepositoryFactory处理

    IRepository<TEntity, TKey> IRepositoryFactory.CreateRepository<TEntity, TKey>()
    {
        return CreateRepository<TEntity, TKey>();
    }

    IRepository<TEntity, TKey> IRepositoryFactory.CreateRepository<TEntity, TKey>(DatabaseType databaseType)
    {
        if (databaseType != DatabaseType.MongoDB)
        {
            throw new NotSupportedException($"MongoRepositoryFactory only supports MongoDB, but got {databaseType}");
        }
        return CreateRepository<TEntity, TKey>();
    }

    Task<IRepository<TEntity, TKey>> IRepositoryFactory.CreateRepositoryAsync<TEntity, TKey>()
    {
        return CreateRepositoryAsync<TEntity, TKey>().ContinueWith(task => (IRepository<TEntity, TKey>)task.Result);
    }

    Task<IRepository<TEntity, TKey>> IRepositoryFactory.CreateRepositoryAsync<TEntity, TKey>(DatabaseType databaseType)
    {
        if (databaseType != DatabaseType.MongoDB)
        {
            throw new NotSupportedException($"MongoRepositoryFactory only supports MongoDB, but got {databaseType}");
        }
        return CreateRepositoryAsync<TEntity, TKey>().ContinueWith(task => (IRepository<TEntity, TKey>)task.Result);
    }

    void IRepositoryFactory.RegisterStrategy<TEntity, TKey>(DatabaseType databaseType, IRepositoryCreationStrategy<TEntity, TKey> strategy) where TEntity : class
    {
        throw new NotSupportedException($"MongoRepositoryFactory does not support registering custom strategies.");
    }

    bool IRepositoryFactory.IsDatabaseTypeSupported(DatabaseType databaseType)
    {
        return databaseType == DatabaseType.MongoDB;
    }

    ISqlRepository<TEntity, TKey> IRepositoryFactory.CreateSqlRepository<TEntity, TKey>()
    {
        throw new NotSupportedException("MongoRepositoryFactory does not support creating SQL repositories.");
    }

    Task<ISqlRepository<TEntity, TKey>> IRepositoryFactory.CreateSqlRepositoryAsync<TEntity, TKey>()
    {
        throw new NotSupportedException("MongoRepositoryFactory does not support creating SQL repositories.");
    }

    IMongoRepository<TEntity, TKey> IRepositoryFactory.CreateMongoRepository<TEntity, TKey>()
    {
        return CreateRepository<TEntity, TKey>();
    }

    Task<IMongoRepository<TEntity, TKey>> IRepositoryFactory.CreateMongoRepositoryAsync<TEntity, TKey>()
    {
        return CreateRepositoryAsync<TEntity, TKey>();
    }

    IMongoRepository<TEntity, TKey> IRepositoryFactory.CreateMongoRepository<TEntity, TKey>(string? databaseName)
    {
        return CreateRepositoryAsyncInternal<TEntity, TKey>(null, databaseName, CancellationToken.None).Result;
    }

    Task<IMongoRepository<TEntity, TKey>> IRepositoryFactory.CreateMongoRepositoryAsync<TEntity, TKey>(string? databaseName)
    {
        return CreateRepositoryAsyncInternal<TEntity, TKey>(null, databaseName, CancellationToken.None);
    }
    #endregion
}