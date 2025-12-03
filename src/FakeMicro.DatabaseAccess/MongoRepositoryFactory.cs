using System;
using System.Threading;
using System.Threading.Tasks;
using FakeMicro.DatabaseAccess.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// MongoDB仓储工厂
/// 用于创建MongoDB仓储实例
/// </summary>
public class MongoRepositoryFactory : IMongoRepositoryFactory
{
    private readonly ISqlSugarClient _db;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider">服务提供程序</param>
    /// <param name="loggerFactory">日志工厂</param>
    public MongoRepositoryFactory(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        
        // 获取命名注册的MongoDB SqlSugar客户端
        _db = serviceProvider.GetRequiredKeyedService<ISqlSugarClient>("MongoDB");
    }

    /// <summary>
    /// 创建MongoDB仓储实例（实现IMongoRepositoryFactory接口）
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
    /// 内部创建MongoDB仓储实例
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="connectionStringName">连接字符串名称</param>
    /// <returns>MongoDB仓储实例</returns>
    private IMongoRepository<TEntity, TKey> CreateRepositoryInternal<TEntity, TKey>(string? connectionStringName = null) where TEntity : class
    {
        var logger = _loggerFactory.CreateLogger<MongoRepository<TEntity, TKey>>();
        return new MongoRepository<TEntity, TKey>(_db, logger);
    }

    /// <summary>
    /// 创建MongoDB仓储实例（异步，实现IMongoRepositoryFactory接口）
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
        // 异步创建仓储实例
        var logger = _loggerFactory.CreateLogger<MongoRepository<TEntity, TKey>>();
        IMongoRepository<TEntity, TKey> repository = new MongoRepository<TEntity, TKey>(_db, logger, databaseName);
        return Task.FromResult(repository);
    }

    /// <summary>
    /// 创建通用仓储实例（显式实现IRepositoryFactory接口）
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>通用仓储实例</returns>
    IRepository<TEntity, TKey> IRepositoryFactory.CreateRepository<TEntity, TKey>()
    {
        return CreateRepositoryInternal<TEntity, TKey>(null);
    }

    /// <summary>
    /// 创建通用仓储实例（异步，显式实现IRepositoryFactory接口）
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>通用仓储实例</returns>
    Task<IRepository<TEntity, TKey>> IRepositoryFactory.CreateRepositoryAsync<TEntity, TKey>()
    {
        return Task.FromResult<IRepository<TEntity, TKey>>(CreateRepositoryInternal<TEntity, TKey>(null));
    }
}