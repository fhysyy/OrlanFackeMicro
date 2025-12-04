using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeMicro.DatabaseAccess.Interfaces;
using Microsoft.Extensions.Logging;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// 动态仓储工厂实现类
/// 支持根据指定数据库类型创建相应的仓储实例
/// 兼容TEntity为Object类型的场景
/// </summary>
public class DynamicRepositoryFactory : IRepositoryFactory
{
    private readonly Dictionary<DatabaseType, Func<Type, Type, object>> _repositoryCreators;
    private readonly Dictionary<DatabaseType, Func<Type, Type, Task<object>>> _repositoryCreatorsAsync;
    private readonly Dictionary<(Type entityType, Type keyType, DatabaseType dbType), Func<object>> _typedStrategies;
    private readonly Dictionary<(Type entityType, Type keyType, DatabaseType dbType), Func<Task<object>>> _typedStrategiesAsync;
    private readonly ILogger<DynamicRepositoryFactory> _logger;
    private readonly DatabaseType _defaultDatabaseType;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="defaultDatabaseType">默认数据库类型</param>
    public DynamicRepositoryFactory(ILogger<DynamicRepositoryFactory> logger, DatabaseType defaultDatabaseType = DatabaseType.PostgreSQL)
    {
        _logger = logger;
        _defaultDatabaseType = defaultDatabaseType;
        _repositoryCreators = new Dictionary<DatabaseType, Func<Type, Type, object>>();
        _repositoryCreatorsAsync = new Dictionary<DatabaseType, Func<Type, Type, Task<object>>>();
        _typedStrategies = new Dictionary<(Type, Type, DatabaseType), Func<object>>();
        _typedStrategiesAsync = new Dictionary<(Type, Type, DatabaseType), Func<Task<object>>>();
    }

    /// <summary>
    /// 注册特定数据库类型的仓储创建器
    /// </summary>
    /// <param name="databaseType">数据库类型</param>
    /// <param name="creator">仓储创建器委托</param>
    /// <param name="asyncCreator">异步仓储创建器委托</param>
    public void RegisterDatabaseCreator(
        DatabaseType databaseType,
        Func<Type, Type, object> creator,
        Func<Type, Type, Task<object>> asyncCreator)
    {
        _repositoryCreators[databaseType] = creator;
        _repositoryCreatorsAsync[databaseType] = asyncCreator;
        _logger.LogInformation("已注册数据库类型 {DatabaseType} 的仓储创建器", databaseType);
    }

    /// <summary>
    /// 创建通用仓储实例（使用默认数据库）
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>通用仓储实例</returns>
    public IRepository<TEntity, TKey> CreateRepository<TEntity, TKey>() where TEntity : class, new()
    {
        return CreateRepository<TEntity, TKey>(_defaultDatabaseType);
    }

    /// <summary>
    /// 创建通用仓储实例（异步，使用默认数据库）
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>通用仓储实例</returns>
    public async Task<IRepository<TEntity, TKey>> CreateRepositoryAsync<TEntity, TKey>() where TEntity : class, new()
    {
        return await CreateRepositoryAsync<TEntity, TKey>(_defaultDatabaseType);
    }

    /// <summary>
    /// 动态指定数据库类型创建仓储实例
    /// 支持TEntity为Object类型的场景
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="databaseType">要使用的数据库类型</param>
    /// <returns>通用仓储实例</returns>
    public IRepository<TEntity, TKey> CreateRepository<TEntity, TKey>(DatabaseType databaseType) where TEntity : class, new()
    {
        try
        {
            _logger.LogDebug("正在为类型 {EntityType} 创建 {DatabaseType} 仓储实例", typeof(TEntity).Name, databaseType);

            // 检查是否有注册的特定类型策略
            var strategyKey = (typeof(TEntity), typeof(TKey), databaseType);
            if (_typedStrategies.TryGetValue(strategyKey, out var strategy))
            {
                return (IRepository<TEntity, TKey>)strategy();
            }

            // 检查是否有注册的数据库类型创建器
            if (_repositoryCreators.TryGetValue(databaseType, out var creator))
            {
                return (IRepository<TEntity, TKey>)creator(typeof(TEntity), typeof(TKey));
            }

            throw new NotSupportedException($"不支持的数据库类型: {databaseType}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建 {DatabaseType} 仓储实例失败，实体类型: {EntityType}", databaseType, typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// 动态指定数据库类型创建仓储实例（异步）
    /// 支持TEntity为Object类型的场景
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="databaseType">要使用的数据库类型</param>
    /// <returns>通用仓储实例</returns>
    public async Task<IRepository<TEntity, TKey>> CreateRepositoryAsync<TEntity, TKey>(DatabaseType databaseType) where TEntity : class, new()
    {
        try
        {
            _logger.LogDebug("正在异步为类型 {EntityType} 创建 {DatabaseType} 仓储实例", typeof(TEntity).Name, databaseType);

            // 检查是否有注册的特定类型策略
            var strategyKey = (typeof(TEntity), typeof(TKey), databaseType);
            if (_typedStrategiesAsync.TryGetValue(strategyKey, out var strategy))
            {
                return (IRepository<TEntity, TKey>)await strategy();
            }

            // 检查是否有注册的数据库类型创建器
            if (_repositoryCreatorsAsync.TryGetValue(databaseType, out var creator))
            {
                return (IRepository<TEntity, TKey>)await creator(typeof(TEntity), typeof(TKey));
            }

            // 如果没有异步创建器，尝试使用同步创建器
            return await Task.FromResult(CreateRepository<TEntity, TKey>(databaseType));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "异步创建 {DatabaseType} 仓储实例失败，实体类型: {EntityType}", databaseType, typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// 注册仓储创建策略
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="databaseType">数据库类型</param>
    /// <param name="strategy">创建策略</param>
    public void RegisterStrategy<TEntity, TKey>(DatabaseType databaseType, IRepositoryCreationStrategy<TEntity, TKey> strategy) where TEntity : class, new()
        {
            var key = (typeof(TEntity), typeof(TKey), databaseType);
            _typedStrategies[key] = () => (object)strategy.CreateRepository();
            _typedStrategiesAsync[key] = async () => (object)await strategy.CreateRepositoryAsync();
            _logger.LogInformation("已注册 {EntityType} 类型的 {DatabaseType} 仓储创建策略", typeof(TEntity).Name, databaseType);
        }

    /// <summary>
    /// 检查是否支持指定的数据库类型
    /// </summary>
    /// <param name="databaseType">数据库类型</param>
    /// <returns>是否支持</returns>
    public bool IsDatabaseTypeSupported(DatabaseType databaseType)
    {
        return _repositoryCreators.ContainsKey(databaseType) || 
               _repositoryCreatorsAsync.ContainsKey(databaseType);
    }
}