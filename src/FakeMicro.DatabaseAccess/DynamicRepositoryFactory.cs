using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.DatabaseAccess.Sharding;
using Microsoft.Extensions.Logging;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// 动态仓储工厂实现类
/// 支持根据指定数据库类型创建相应的仓储实例
/// 兼容TEntity为Object类型的场景
/// </summary>
public class DynamicRepositoryFactory : IRepositoryFactory
{
    // 数据库类型到仓储创建器的映射
    private readonly Dictionary<DatabaseType, Func<Type, Type, object>> _repositoryCreators;
    private readonly Dictionary<DatabaseType, Func<Type, Type, Task<object>>> _repositoryCreatorsAsync;
    
    // MongoDB特定的仓储创建器，支持数据库名称参数
    private readonly Func<Type, Type, string?, object>? _mongoRepositoryCreator;
    private readonly Func<Type, Type, string?, Task<object>>? _mongoRepositoryCreatorAsync;
    
    // 特定类型的仓储创建策略
    private readonly Dictionary<(Type entityType, Type keyType, DatabaseType dbType), Func<object>> _typedStrategies;
    private readonly Dictionary<(Type entityType, Type keyType, DatabaseType dbType), Func<Task<object>>> _typedStrategiesAsync;
    
    private readonly ILogger<DynamicRepositoryFactory> _logger;
    private readonly DatabaseType _defaultDatabaseType;
    private readonly IShardingContext? _shardingContext;

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
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="mongoRepositoryCreator">MongoDB仓储创建器</param>
    /// <param name="mongoRepositoryCreatorAsync">异步MongoDB仓储创建器</param>
    /// <param name="defaultDatabaseType">默认数据库类型</param>
    public DynamicRepositoryFactory(
        ILogger<DynamicRepositoryFactory> logger,
        Func<Type, Type, string?, object>? mongoRepositoryCreator = null,
        Func<Type, Type, string?, Task<object>>? mongoRepositoryCreatorAsync = null,
        DatabaseType defaultDatabaseType = DatabaseType.PostgreSQL)
    {
        _logger = logger;
        _defaultDatabaseType = defaultDatabaseType;
        _repositoryCreators = new Dictionary<DatabaseType, Func<Type, Type, object>>();
        _repositoryCreatorsAsync = new Dictionary<DatabaseType, Func<Type, Type, Task<object>>>();
        _typedStrategies = new Dictionary<(Type, Type, DatabaseType), Func<object>>();
        _typedStrategiesAsync = new Dictionary<(Type, Type, DatabaseType), Func<Task<object>>>();
        _mongoRepositoryCreator = mongoRepositoryCreator;
        _mongoRepositoryCreatorAsync = mongoRepositoryCreatorAsync;
    }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="shardingContext">分片上下文</param>
    /// <param name="defaultDatabaseType">默认数据库类型</param>
    public DynamicRepositoryFactory(ILogger<DynamicRepositoryFactory> logger, IShardingContext shardingContext, DatabaseType defaultDatabaseType = DatabaseType.PostgreSQL)
    {
        _logger = logger;
        _defaultDatabaseType = defaultDatabaseType;
        _shardingContext = shardingContext;
        _repositoryCreators = new Dictionary<DatabaseType, Func<Type, Type, object>>();
        _repositoryCreatorsAsync = new Dictionary<DatabaseType, Func<Type, Type, Task<object>>>();
        _typedStrategies = new Dictionary<(Type, Type, DatabaseType), Func<object>>();
        _typedStrategiesAsync = new Dictionary<(Type, Type, DatabaseType), Func<Task<object>>>();
    }
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="mongoRepositoryCreator">MongoDB仓储创建器</param>
    /// <param name="mongoRepositoryCreatorAsync">异步MongoDB仓储创建器</param>
    /// <param name="shardingContext">分片上下文</param>
    /// <param name="defaultDatabaseType">默认数据库类型</param>
    public DynamicRepositoryFactory(
        ILogger<DynamicRepositoryFactory> logger,
        Func<Type, Type, string?, object>? mongoRepositoryCreator = null,
        Func<Type, Type, string?, Task<object>>? mongoRepositoryCreatorAsync = null,
        IShardingContext? shardingContext = null,
        DatabaseType defaultDatabaseType = DatabaseType.PostgreSQL)
    {
        _logger = logger;
        _defaultDatabaseType = defaultDatabaseType;
        _shardingContext = shardingContext;
        _repositoryCreators = new Dictionary<DatabaseType, Func<Type, Type, object>>();
        _repositoryCreatorsAsync = new Dictionary<DatabaseType, Func<Type, Type, Task<object>>>();
        _typedStrategies = new Dictionary<(Type, Type, DatabaseType), Func<object>>();
        _typedStrategiesAsync = new Dictionary<(Type, Type, DatabaseType), Func<Task<object>>>();
        _mongoRepositoryCreator = mongoRepositoryCreator;
        _mongoRepositoryCreatorAsync = mongoRepositoryCreatorAsync;
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
    /// 创建SQL仓储实例，使用默认SQL数据库
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>SQL仓储实例</returns>
    public ISqlRepository<TEntity, TKey> CreateSqlRepository<TEntity, TKey>() where TEntity : class, new()
    {
        return CreateSqlRepository<TEntity, TKey>(_defaultDatabaseType);
    }

    /// <summary>
    /// 创建SQL仓储实例，指定SQL数据库类型
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="databaseType">SQL数据库类型</param>
    /// <returns>SQL仓储实例</returns>
    public ISqlRepository<TEntity, TKey> CreateSqlRepository<TEntity, TKey>(DatabaseType databaseType) where TEntity : class, new()
    {
        if (!IsSqlDatabaseType(databaseType))
        {
            throw new NotSupportedException($"不支持的SQL数据库类型: {databaseType}");
        }
        
        return (ISqlRepository<TEntity, TKey>)CreateRepository<TEntity, TKey>(databaseType);
    }

    /// <summary>
    /// 创建SQL仓储实例，使用默认SQL数据库
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>SQL仓储实例</returns>
    public async Task<ISqlRepository<TEntity, TKey>> CreateSqlRepositoryAsync<TEntity, TKey>() where TEntity : class, new()
    {
        return await CreateSqlRepositoryAsync<TEntity, TKey>(_defaultDatabaseType);
    }

    /// <summary>
    /// 创建SQL仓储实例，指定SQL数据库类型
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="databaseType">SQL数据库类型</param>
    /// <returns>SQL仓储实例</returns>
    public async Task<ISqlRepository<TEntity, TKey>> CreateSqlRepositoryAsync<TEntity, TKey>(DatabaseType databaseType) where TEntity : class, new()
    {
        if (!IsSqlDatabaseType(databaseType))
        {
            throw new NotSupportedException($"不支持的SQL数据库类型: {databaseType}");
        }
        
        return (ISqlRepository<TEntity, TKey>)await CreateRepositoryAsync<TEntity, TKey>(databaseType);
    }

    /// <summary>
    /// 创建MongoDB仓储实例，使用默认MongoDB数据库
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>MongoDB仓储实例</returns>
    public IMongoRepository<TEntity, TKey> CreateMongoRepository<TEntity, TKey>() where TEntity : class, new()
    {
        return CreateMongoRepository<TEntity, TKey>(null);
    }

    /// <summary>
    /// 创建MongoDB仓储实例，指定数据库名称
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="databaseName">数据库名称</param>
    /// <returns>MongoDB仓储实例</returns>
    public IMongoRepository<TEntity, TKey> CreateMongoRepository<TEntity, TKey>(string? databaseName) where TEntity : class, new()
    {
        try
        {
            _logger.LogDebug("正在为类型 {EntityType} 创建MongoDB仓储实例，数据库名称: {DatabaseName}", typeof(TEntity).Name, databaseName);
            
            // 使用MongoDB特定的创建器（如果已注册）
            if (_mongoRepositoryCreator != null)
            {
                return (IMongoRepository<TEntity, TKey>)_mongoRepositoryCreator(typeof(TEntity), typeof(TKey), databaseName);
            }
            
            // 否则使用通用创建器
            return (IMongoRepository<TEntity, TKey>)CreateRepository<TEntity, TKey>(DatabaseType.MongoDB);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建MongoDB仓储实例失败，实体类型: {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// 创建MongoDB仓储实例，使用默认MongoDB数据库
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <returns>MongoDB仓储实例</returns>
    public async Task<IMongoRepository<TEntity, TKey>> CreateMongoRepositoryAsync<TEntity, TKey>() where TEntity : class, new()
    {
        return await CreateMongoRepositoryAsync<TEntity, TKey>(null);
    }

    /// <summary>
    /// 创建MongoDB仓储实例，指定数据库名称
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="databaseName">数据库名称</param>
    /// <returns>MongoDB仓储实例</returns>
    public async Task<IMongoRepository<TEntity, TKey>> CreateMongoRepositoryAsync<TEntity, TKey>(string? databaseName) where TEntity : class, new()
    {
        try
        {
            _logger.LogDebug("正在异步为类型 {EntityType} 创建MongoDB仓储实例，数据库名称: {DatabaseName}", typeof(TEntity).Name, databaseName);
            
            // 使用MongoDB特定的异步创建器（如果已注册）
            if (_mongoRepositoryCreatorAsync != null)
            {
                return (IMongoRepository<TEntity, TKey>)await _mongoRepositoryCreatorAsync(typeof(TEntity), typeof(TKey), databaseName);
            }
            
            // 使用MongoDB特定的同步创建器（如果已注册）
            if (_mongoRepositoryCreator != null)
            {
                return (IMongoRepository<TEntity, TKey>)_mongoRepositoryCreator(typeof(TEntity), typeof(TKey), databaseName);
            }
            
            // 否则使用通用创建器
            return (IMongoRepository<TEntity, TKey>)await CreateRepositoryAsync<TEntity, TKey>(DatabaseType.MongoDB);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "异步创建MongoDB仓储实例失败，实体类型: {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// 检查是否支持指定的数据库类型
    /// </summary>
    /// <param name="databaseType">数据库类型</param>
    /// <returns>是否支持</returns>
    public bool IsDatabaseTypeSupported(DatabaseType databaseType)
    {
        return _repositoryCreators.ContainsKey(databaseType) || 
               _repositoryCreatorsAsync.ContainsKey(databaseType) ||
               (databaseType == DatabaseType.MongoDB && (_mongoRepositoryCreator != null || _mongoRepositoryCreatorAsync != null));
    }
    
    /// <summary>
    /// 创建仓储实例，使用默认数据库类型，并通过键值路由到对应分片
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="key">用于路由的键值</param>
    /// <returns>仓储实例</returns>
    public IRepository<TEntity, TKey> CreateRepository<TEntity, TKey>(TKey key) where TEntity : class, new()
    {
        try
        {
            _logger.LogDebug("正在为类型 {EntityType} 创建分片仓储实例，键值: {Key}", typeof(TEntity).Name, key);
            
            // 使用分片上下文计算路由
            if (_shardingContext != null)
            {
                var shardIndex = _shardingContext.GetShardIndex<TEntity, TKey>(key);
                var shardName = _shardingContext.GetShardName<TEntity, TKey>(key);
                
                _logger.LogDebug("路由到分片: {ShardName}", shardName);
                
                // 这里需要实现根据分片名称获取对应的数据库连接
                // 暂时返回默认数据库的仓储实例
                return CreateRepository<TEntity, TKey>();
            }
            
            _logger.LogWarning("分片上下文未配置，使用默认数据库");
            return CreateRepository<TEntity, TKey>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建分片仓储实例失败，实体类型: {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }
    
    /// <summary>
    /// 创建仓储实例，使用默认数据库类型，并通过键值路由到对应分片
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="key">用于路由的键值</param>
    /// <returns>仓储实例</returns>
    public async Task<IRepository<TEntity, TKey>> CreateRepositoryAsync<TEntity, TKey>(TKey key) where TEntity : class, new()
    {
        try
        {
            _logger.LogDebug("正在异步为类型 {EntityType} 创建分片仓储实例，键值: {Key}", typeof(TEntity).Name, key);
            
            // 使用分片上下文计算路由
            if (_shardingContext != null)
            {
                var shardIndex = _shardingContext.GetShardIndex<TEntity, TKey>(key);
                var shardName = _shardingContext.GetShardName<TEntity, TKey>(key);
                
                _logger.LogDebug("路由到分片: {ShardName}", shardName);
                
                // 这里需要实现根据分片名称获取对应的数据库连接
                // 暂时返回默认数据库的仓储实例
                return await CreateRepositoryAsync<TEntity, TKey>();
            }
            
            _logger.LogWarning("分片上下文未配置，使用默认数据库");
            return await CreateRepositoryAsync<TEntity, TKey>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "异步创建分片仓储实例失败，实体类型: {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }
    
    /// <summary>
    /// 创建SQL仓储实例，使用默认SQL数据库，并通过键值路由到对应分片
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="key">用于路由的键值</param>
    /// <returns>SQL仓储实例</returns>
    public ISqlRepository<TEntity, TKey> CreateSqlRepository<TEntity, TKey>(TKey key) where TEntity : class, new()
    {
        try
        {
            _logger.LogDebug("正在为类型 {EntityType} 创建分片SQL仓储实例，键值: {Key}", typeof(TEntity).Name, key);
            
            // 使用分片上下文计算路由
            if (_shardingContext != null)
            {
                var shardIndex = _shardingContext.GetShardIndex<TEntity, TKey>(key);
                var shardName = _shardingContext.GetShardName<TEntity, TKey>(key);
                
                _logger.LogDebug("路由到分片: {ShardName}", shardName);
                
                // 这里需要实现根据分片名称获取对应的数据库连接
                // 暂时返回默认数据库的仓储实例
                return CreateSqlRepository<TEntity, TKey>();
            }
            
            _logger.LogWarning("分片上下文未配置，使用默认数据库");
            return CreateSqlRepository<TEntity, TKey>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建分片SQL仓储实例失败，实体类型: {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }
    
    /// <summary>
    /// 创建SQL仓储实例，使用默认SQL数据库，并通过键值路由到对应分片
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    /// <param name="key">用于路由的键值</param>
    /// <returns>SQL仓储实例</returns>
    public async Task<ISqlRepository<TEntity, TKey>> CreateSqlRepositoryAsync<TEntity, TKey>(TKey key) where TEntity : class, new()
    {
        try
        {
            _logger.LogDebug("正在异步为类型 {EntityType} 创建分片SQL仓储实例，键值: {Key}", typeof(TEntity).Name, key);
            
            // 使用分片上下文计算路由
            if (_shardingContext != null)
            {
                var shardIndex = _shardingContext.GetShardIndex<TEntity, TKey>(key);
                var shardName = _shardingContext.GetShardName<TEntity, TKey>(key);
                
                _logger.LogDebug("路由到分片: {ShardName}", shardName);
                
                // 这里需要实现根据分片名称获取对应的数据库连接
                // 暂时返回默认数据库的仓储实例
                return await CreateSqlRepositoryAsync<TEntity, TKey>();
            }
            
            _logger.LogWarning("分片上下文未配置，使用默认数据库");
            return await CreateSqlRepositoryAsync<TEntity, TKey>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "异步创建分片SQL仓储实例失败，实体类型: {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }
    
    /// <summary>
    /// 检查是否为SQL数据库类型
    /// </summary>
    /// <param name="databaseType">数据库类型</param>
    /// <returns>是否为SQL数据库类型</returns>
    private bool IsSqlDatabaseType(DatabaseType databaseType)
    {
        return databaseType switch
        {
            DatabaseType.MySQL => true,
            DatabaseType.PostgreSQL => true,
            DatabaseType.MariaDB => true,
            DatabaseType.SQLServer => true,
            DatabaseType.SQLite => true,
            _ => false
        };
    }
}