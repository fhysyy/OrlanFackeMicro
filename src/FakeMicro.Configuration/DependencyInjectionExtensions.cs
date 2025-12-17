using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using SqlSugar;
using FakeMicro.DatabaseAccess;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.DatabaseAccess.Contexts;

/// <summary>
/// Orleans + SqlSugar 依赖注入配置
/// 提供完整的分布式数据访问层配置
/// </summary>
namespace FakeMicro.Configuration
{
    /// <summary>
    /// 数据访问层依赖注入配置扩展类
    /// </summary>
    public static class DataAccessServiceExtensions
    {
        /// <summary>
        /// 添加SqlSugar数据访问层服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <param name="databaseType">数据库类型</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddSqlSugarDataAccess(
            this IServiceCollection services,
            string connectionString,
            DbType databaseType = DbType.SqlServer)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("数据库连接字符串不能为空", nameof(connectionString));

            // 添加SqlSugar配置
            services.AddSingleton<SqlSugarScope>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<SqlSugarScope>>();
                return new SqlSugarScope(new ConnectionConfig()
                {
                    ConnectionString = connectionString,
                    DbType = databaseType,
                    IsAutoCloseConnection = true,
                    InitKeyType = InitKeyType.Attribute, // 支持特性配置
                    ConfigureExternalServices = new ConfigureExternalServices()
                    {
                        EntityService = (property, column) =>
                        {
                            // 全局实体配置
                            if (column.PropertyType == typeof(DateTime) || column.PropertyType == typeof(DateTime?))
                            {
                                column.DataType = "datetime2";
                            }
                            if (column.PropertyType == typeof(Guid) || column.PropertyType == typeof(Guid?))
                            {
                                column.DataType = "uniqueidentifier";
                            }
                        },
                        EntityNameService = (type, entity) =>
                        {
                            // 全局表名配置
                            entity.TableDescription = $"{type.Name}数据表";
                        }
                    },
                    // AOP配置
                    MoreSettings = new ConnMoreSettings()
                    {
                        SqlTraceLog = (sql, pars) =>
                        {
                            // SQL执行日志
                            logger.LogDebug("SQL执行: {Sql}\n参数: {Parameters}", sql, 
                                string.Join(", ", pars.Select(p => $"{p.ParameterName}={p.Value}")));
                        }
                    }
                }, (sql, pars) =>
                {
                    // 错误日志
                    logger.LogError("SQL执行错误: {Sql}\n参数: {Parameters}", sql,
                        string.Join(", ", pars.Select(p => $"{p.ParameterName}={p.Value}")));
                    return true;
                });
            });

            // 注册仓储接口和实现
            services.AddScoped(typeof(IRepository<,>), typeof(SqlSugarRepository<,>));

            // 注册数据库上下文
            services.AddScoped<IDatabaseContext, SqlSugarDatabaseContext>();

            // 添加数据访问异常处理
            services.AddScoped<IDataAccessExceptionHandler, DataAccessExceptionHandler>();

            return services;
        }

        /// <summary>
        /// 添加Orleans Grain服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="orleansOptions">Orleans配置选项</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddOrleansGrains(
            this IServiceCollection services,
            Action<OrleansOptions> orleansOptions = null)
        {
            // Orleans选项配置
            var options = new OrleansOptions();
            orleansOptions?.Invoke(options);
            
            services.Configure(orleansOptions);

            // 添加Grain注册服务
            services.AddTransient(typeof(IGrainRegistrationService<>), typeof(GrainRegistrationService<>));
            
            // 初始化断路器管理器
            services.AddSingleton(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                FakeMicro.Grains.CircuitBreakerManager.Initialize(loggerFactory);
                return null;
            });

            // Grain生命周期管理
            services.AddSingleton<IGrainLifecycleManager, GrainLifecycleManager>();

            return services;
        }

        /// <summary>
        /// 构建Orleans Silo主机
        /// </summary>
        /// <param name="builder">Silo主机构建器</param>
        /// <param name="siloOptions">Silo配置选项</param>
        /// <returns>Silo主机构建器</returns>
        public static ISiloBuilder ConfigureOrleansSilo(
            this ISiloBuilder builder,
            Action<OrleansSiloOptions> siloOptions = null)
        {
            var options = new OrleansSiloOptions();
            siloOptions?.Invoke(options);

            // 配置文件存储
            builder.UseFileBasedClusterInfo(options.ClusterId, options.ServiceId, options.FileName);
            
            // 开发环境配置
            if (options.UseDevelopmentCluster)
            {
                builder.UseLocalhostClustering();
            }
            else
            {
                builder.Configure<SiloOptions>(options.SiloOptionsAction);
            }

            // 配置端点
            builder.Configure<EndpointOptions>(options.EndpointOptionsAction);

            // 数据库存储配置
            if (options.UseDatabaseClustering)
            {
                builder.AddAdoNetClustering(options.AdoNetClusteringOptions);
            }

            // 应用程序部件
            builder.ConfigureApplicationParts(parts =>
            {
                parts.AddApplicationPart(options.ApplicationAssembly);
            });

            return builder;
        }

        /// <summary>
        /// 添加工作Grain服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="grainTypes">Grain类型集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddWorkerGrains(
            this IServiceCollection services,
            IEnumerable<Type> grainTypes)
        {
            // 注册所有工作Grain类型
            foreach (var grainType in grainTypes)
            {
                if (grainType.IsClass && !grainType.IsAbstract)
                {
                    services.AddTransient(grainType);
                }
            }

            // Grain注册表
            services.AddSingleton<IGrainRegistry, GrainRegistry>(provider =>
            {
                return new GrainRegistry(grainTypes);
            });

            return services;
        }

        /// <summary>
        /// 添加事务管理服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddTransactionManagement(this IServiceCollection services)
        {
            // 事务上下文
            services.AddScoped<ITransactionContext, TransactionContext>();
            
            // 事务管理器
            services.AddSingleton<ITransactionManager, TransactionManager>();

            // 事务性仓储
            services.AddScoped(typeof(ITransactionalRepository<,>), typeof(TransactionalRepository<,>));

            return services;
        }

        /// <summary>
        /// 添加缓存服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="cacheOptions">缓存配置选项</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDataCache(
            this IServiceCollection services,
            Action<DataCacheOptions> cacheOptions = null)
        {
            var options = new DataCacheOptions();
            cacheOptions?.Invoke(options);

            services.Configure<DataCacheOptions>(opts =>
            {
                opts.DefaultExpiration = options.DefaultExpiration;
                opts.EnableCaching = options.EnableCaching;
                opts.CacheProviders = options.CacheProviders;
            });

            // 内存缓存
            if (options.EnableCaching && options.CacheProviders.Contains("Memory"))
            {
                services.AddMemoryCache();
                services.AddSingleton<IMemoryCacheProvider, MemoryCacheProvider>();
            }

            // 分布式缓存支持
            if (options.EnableCaching && options.CacheProviders.Contains("Distributed"))
            {
                services.AddStackExchangeRedisCache(options.RedisOptions);
                services.AddSingleton<IDistributedCacheProvider, RedisCacheProvider>();
            }

            // 缓存管理服务
            services.AddSingleton<ICacheManager, CacheManager>();

            return services;
        }
    }

    #region 配置选项类

    /// <summary>
    /// Orleans配置选项
    /// </summary>
    public class OrleansOptions
    {
        public string? ClusterId { get; set; }
        public string? ServiceId { get; set; }
        public bool UseDevelopmentCluster { get; set; } = true;
        public bool UseDatabaseClustering { get; set; }
        public Action<SiloOptions>? SiloOptionsAction { get; set; }
        public Action<EndpointOptions>? EndpointOptionsAction { get; set; }
        public Action<AdoNetClusteringOptions>? AdoNetClusteringOptions { get; set; }
        public System.Reflection.Assembly? ApplicationAssembly { get; set; }
        public string FileName { get; set; } = "orleans";
    }

    /// <summary>
    /// Orleans Silo配置选项
    /// </summary>
    public class OrleansSiloOptions
    {
        public string ClusterId { get; set; } = "FakeMicroCluster";
        public string ServiceId { get; set; } = "FakeMicroService";
        public string FileName { get; set; } = "orleans_cluster";
        public bool UseDevelopmentCluster { get; set; } = true;
        public bool UseDatabaseClustering { get; set; }
        public Action<SiloOptions>? SiloOptionsAction { get; set; }
        public Action<EndpointOptions>? EndpointOptionsAction { get; set; }
        public Action<AdoNetClusteringOptions>? AdoNetClusteringOptions { get; set; }
        public System.Reflection.Assembly? ApplicationAssembly { get; set; }
    }

    /// <summary>
    /// 数据缓存配置选项
    /// </summary>
    public class DataCacheOptions
    {
        public bool EnableCaching { get; set; } = true;
        public TimeSpan DefaultMemoryExpiration { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan DefaultDistributedExpiration { get; set; } = TimeSpan.FromHours(1);
        public List<string> CacheProviders { get; set; } = new List<string> { "Memory" };
        public Action<RedisCacheOptions>? RedisOptions { get; set; }
        public bool EnableMultiLevelCache { get; set; } = true;
        public int CacheWarmupBatchSize { get; set; } = 100;
    }

    #endregion

    #region 服务接口定义

    /// <summary>
    /// 数据访问异常处理器接口
    /// </summary>
    public interface IDataAccessExceptionHandler
    {
        Task HandleExceptionAsync(Exception exception, string operation);
    }

    /// <summary>
    /// Grain注册服务接口
    /// </summary>
    public interface IGrainRegistrationService<TGrain> where TGrain : IGrain
    {
        Task RegisterGrainAsync();
    }

    /// <summary>
    /// Grain生命周期管理器接口
    /// </summary>
    public interface IGrainLifecycleManager
    {
        Task StartAsync();
        Task StopAsync();
    }

    /// <summary>
    /// Grain注册表接口
    /// </summary>
    public interface IGrainRegistry
    {
        IEnumerable<Type> GetGrainTypes();
    }

    /// <summary>
    /// 事务上下文接口
    /// </summary>
    public interface ITransactionContext
    {
        SqlSugarScope DbContext { get; }
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
        bool HasActiveTransaction { get; }
    }

    /// <summary>
    /// 事务管理器接口
    /// </summary>
    public interface ITransactionManager
    {
        Task ExecuteInTransactionAsync(Func<Task> operation);
    }

    /// <summary>
    /// 事务性仓储接口
    /// </summary>
    public interface ITransactionalRepository<in TEntity, TKey> where TEntity : class
    {
        Task<TEntity?> GetByIdAsync(TKey id);
        Task AddAsync(TEntity entity);
        Task UpdateAsync(TEntity entity);
        Task DeleteAsync(TEntity entity);
        Task SaveChangesAsync();
    }

    /// <summary>
    /// 缓存提供商接口
    /// </summary>
    public interface ICacheProvider
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class;
        Task RemoveAsync(string key);
        Task ClearAsync();
    }

    /// <summary>
    /// 内存缓存提供商接口
    /// </summary>
    public interface IMemoryCacheProvider : ICacheProvider { }

    /// <summary>
    /// 分布式缓存提供商接口
    /// </summary>
    public interface IDistributedCacheProvider : ICacheProvider { }

    /// <summary>
    /// 缓存管理器接口
    /// </summary>
    public interface ICacheManager
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? memoryExpiration = null, TimeSpan? distributedExpiration = null) where T : class;
        Task RemoveAsync(string key);
        Task ClearByPatternAsync(string pattern);
        Task WarmupCacheAsync<T>(IEnumerable<KeyValuePair<string, T>> items) where T : class;
    }

    #endregion

    #region 服务实现类

    /// <summary>
    /// 数据访问异常处理器实现
    /// </summary>
    public class DataAccessExceptionHandler : IDataAccessExceptionHandler
    {
        private readonly ILogger<DataAccessExceptionHandler> _logger;

        public DataAccessExceptionHandler(ILogger<DataAccessExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async Task HandleExceptionAsync(Exception exception, string operation)
        {
            _logger.LogError(exception, "数据访问操作异常: {Operation}", operation);
            
            // 这里可以添加更多的异常处理逻辑
            // 比如：发送通知、记录到监控系统等
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Grain注册服务实现
    /// </summary>
    public class GrainRegistrationService<TGrain> : IGrainRegistrationService<TGrain> 
        where TGrain : IGrain
    {
        private readonly ILogger<GrainRegistrationService<TGrain>> _logger;
        private readonly IGrainRegistry _registry;

        public GrainRegistrationService(
            ILogger<GrainRegistrationService<TGrain>> logger,
            IGrainRegistry registry)
        {
            _logger = logger;
            _registry = registry;
        }

        public async Task RegisterGrainAsync()
        {
            _logger.LogInformation("注册Grain类型: {GrainType}", typeof(TGrain).Name);
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Grain生命周期管理器实现
    /// </summary>
    public class GrainLifecycleManager : IGrainLifecycleManager
    {
        private readonly ILogger<GrainLifecycleManager> _logger;
        private readonly IServiceProvider _serviceProvider;

        public GrainLifecycleManager(
            ILogger<GrainLifecycleManager> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync()
        {
            _logger.LogInformation("启动Grain生命周期管理器");
            await Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            _logger.LogInformation("停止Grain生命周期管理器");
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Grain注册表实现
    /// </summary>
    public class GrainRegistry : IGrainRegistry
    {
        private readonly IEnumerable<Type> _grainTypes;

        public GrainRegistry(IEnumerable<Type> grainTypes)
        {
            _grainTypes = grainTypes ?? throw new ArgumentNullException(nameof(grainTypes));
        }

        public IEnumerable<Type> GetGrainTypes()
        {
            return _grainTypes;
        }
    }

    /// <summary>
    /// 事务上下文实现
    /// </summary>
    public class TransactionContext : ITransactionContext
    {
        private readonly SqlSugarScope _dbContext;
        private readonly ILogger<TransactionContext> _logger;

        public TransactionContext(SqlSugarScope dbContext, ILogger<TransactionContext> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public SqlSugarScope DbContext => _dbContext;

        public bool HasActiveTransaction => _dbContext.CurrentConnectionConfig?.MoreSettings?.EnableUnderLine != false;

        public async Task BeginTransactionAsync()
        {
            try
            {
                _dbContext.BeginTran();
                _logger.LogDebug("开始事务");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开始事务失败");
                throw;
            }
        }

        public async Task CommitAsync()
        {
            try
            {
                _dbContext.CommitTran();
                _logger.LogDebug("提交事务");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提交事务失败");
                throw;
            }
        }

        public async Task RollbackAsync()
        {
            try
            {
                _dbContext.RollbackTran();
                _logger.LogDebug("回滚事务");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "回滚事务失败");
                throw;
            }
        }
    }

    /// <summary>
    /// 事务管理器实现
    /// </summary>
    public class TransactionManager : ITransactionManager
    {
        private readonly ITransactionContext _transactionContext;
        private readonly ILogger<TransactionManager> _logger;

        public TransactionManager(
            ITransactionContext transactionContext,
            ILogger<TransactionManager> logger)
        {
            _transactionContext = transactionContext ?? throw new ArgumentNullException(nameof(transactionContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ExecuteInTransactionAsync(Func<Task> operation)
        {
            await _transactionContext.BeginTransactionAsync();
            try
            {
                await operation();
                await _transactionContext.CommitAsync();
            }
            catch
            {
                await _transactionContext.RollbackAsync();
                throw;
            }
        }
    }

    /// <summary>
    /// 事务性仓储实现
    /// </summary>
    public class TransactionalRepository<in TEntity, TKey> : ITransactionalRepository<in TEntity, TKey> 
        where TEntity : class
    {
        private readonly ITransactionContext _transactionContext;
        private readonly IRepository<in TEntity, TKey> _repository;
        private readonly ILogger<TransactionalRepository<in TEntity, TKey>> _logger;

        public TransactionalRepository(
            ITransactionContext transactionContext,
            IRepository<in TEntity, TKey> repository,
            ILogger<TransactionalRepository<in TEntity, TKey>> logger)
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

        public async Task<in TEntity?> GetByIdAsync(TKey id)
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

    /// <summary>
    /// 内存缓存提供商实现
    /// </summary>
    public class MemoryCacheProvider : IMemoryCacheProvider
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheProvider> _logger;

        public MemoryCacheProvider(IMemoryCache cache, ILogger<MemoryCacheProvider> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ClearAsync()
        {
            // 内存缓存没有直接清空所有缓存的方法
            _logger.LogWarning("内存缓存不支持批量清空操作");
            await Task.CompletedTask;
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            if (_cache.TryGetValue(key, out T? value))
            {
                _logger.LogDebug("从内存缓存获取数据: {Key}", key);
                return value;
            }
            
            _logger.LogDebug("内存缓存未命中: {Key}", key);
            return null;
        }

        public async Task RemoveAsync(string key)
        {
            _cache.Remove(key);
            _logger.LogDebug("从内存缓存移除数据: {Key}", key);
            await Task.CompletedTask;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            
            _cache.Set(key, value, options);
            _logger.LogDebug("设置内存缓存数据: {Key}, 过期时间: {Expiration}", key, expiration);
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 分布式缓存提供商实现
    /// </summary>
    public class RedisCacheProvider : IDistributedCacheProvider
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisCacheProvider> _logger;

        public RedisCacheProvider(IDistributedCache cache, ILogger<RedisCacheProvider> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ClearAsync()
        {
            _logger.LogWarning("分布式缓存不支持批量清空操作");
            await Task.CompletedTask;
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            var json = await _cache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(json))
            {
                _logger.LogDebug("从分布式缓存获取数据: {Key}", key);
                return System.Text.Json.JsonSerializer.Deserialize<T>(json);
            }
            
            _logger.LogDebug("分布式缓存未命中: {Key}", key);
            return null;
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
            _logger.LogDebug("从分布式缓存移除数据: {Key}", key);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
        {
            var json = System.Text.Json.JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            
            await _cache.SetStringAsync(key, json, options);
            _logger.LogDebug("设置分布式缓存数据: {Key}, 过期时间: {Expiration}", key, expiration);
        }
    }

    /// <summary>
    /// 缓存管理器实现 - 支持多级缓存策略
    /// </summary>
    public class CacheManager : ICacheManager
    {
        private readonly IMemoryCacheProvider? _memoryCacheProvider;
        private readonly IDistributedCacheProvider? _distributedCacheProvider;
        private readonly ILogger<CacheManager> _logger;
        private readonly DataCacheOptions _options;

        public CacheManager(
            IEnumerable<ICacheProvider> providers,
            ILogger<CacheManager> logger,
            IOptions<DataCacheOptions> options)
        {
            _memoryCacheProvider = providers.OfType<IMemoryCacheProvider>().FirstOrDefault();
            _distributedCacheProvider = providers.OfType<IDistributedCacheProvider>().FirstOrDefault();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task ClearByPatternAsync(string pattern)
        {
            _logger.LogWarning("缓存提供商不支持按模式清空操作");
            await Task.CompletedTask;
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            if (!_options.EnableCaching)
            {
                return null;
            }

            T? result = null;

            // 1. 先检查内存缓存
            if (_memoryCacheProvider != null)
            {
                result = await _memoryCacheProvider.GetAsync<T>(key);
                if (result != null)
                {
                    _logger.LogDebug("从内存缓存获取数据命中: {Key}", key);
                    return result;
                }
            }

            // 2. 再检查分布式缓存
            if (_distributedCacheProvider != null)
            {
                result = await _distributedCacheProvider.GetAsync<T>(key);
                if (result != null)
                {
                    _logger.LogDebug("从分布式缓存获取数据命中: {Key}", key);
                    
                    // 3. 如果分布式缓存有数据，同步到内存缓存
                    if (_memoryCacheProvider != null && _options.EnableMultiLevelCache)
                    {
                        await _memoryCacheProvider.SetAsync(key, result, _options.DefaultMemoryExpiration);
                        _logger.LogDebug("将分布式缓存数据同步到内存缓存: {Key}", key);
                    }
                }
            }
            
            if (result == null)
            {
                _logger.LogDebug("所有缓存都未命中: {Key}", key);
            }

            return result;
        }

        public async Task RemoveAsync(string key)
        {
            if (!_options.EnableCaching)
            {
                return;
            }

            // 从所有缓存中移除
            if (_memoryCacheProvider != null)
            {
                await _memoryCacheProvider.RemoveAsync(key);
            }
            
            if (_distributedCacheProvider != null)
            {
                await _distributedCacheProvider.RemoveAsync(key);
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? memoryExpiration = null, TimeSpan? distributedExpiration = null) where T : class
        {
            if (!_options.EnableCaching)
            {
                return;
            }

            // 设置内存缓存
            if (_memoryCacheProvider != null)
            {
                var memExp = memoryExpiration ?? _options.DefaultMemoryExpiration;
                await _memoryCacheProvider.SetAsync(key, value, memExp);
                _logger.LogDebug("设置内存缓存数据: {Key}, 过期时间: {Expiration}", key, memExp);
            }

            // 设置分布式缓存
            if (_distributedCacheProvider != null)
            {
                var distExp = distributedExpiration ?? _options.DefaultDistributedExpiration;
                await _distributedCacheProvider.SetAsync(key, value, distExp);
                _logger.LogDebug("设置分布式缓存数据: {Key}, 过期时间: {Expiration}", key, distExp);
            }
        }

        public async Task WarmupCacheAsync<T>(IEnumerable<KeyValuePair<string, T>> items) where T : class
        {
            if (!_options.EnableCaching || items == null)
            {
                return;
            }

            var itemList = items.ToList();
            _logger.LogInformation("开始缓存预热，共 {Count} 个项目", itemList.Count);

            // 批量预热内存缓存
            if (_memoryCacheProvider != null)
            {
                var memoryTasks = itemList.Select(item => 
                    _memoryCacheProvider.SetAsync(item.Key, item.Value, _options.DefaultMemoryExpiration));
                
                await Task.WhenAll(memoryTasks);
                _logger.LogInformation("内存缓存预热完成");
            }

            // 批量预热分布式缓存（分批处理）
            if (_distributedCacheProvider != null)
            {
                var batches = itemList
                    .Select((item, index) => new { item, index })
                    .GroupBy(x => x.index / _options.CacheWarmupBatchSize)
                    .Select(g => g.Select(x => x.item).ToList());

                foreach (var batch in batches)
                {
                    var distributedTasks = batch.Select(item => 
                        _distributedCacheProvider.SetAsync(item.Key, item.Value, _options.DefaultDistributedExpiration));
                    
                    await Task.WhenAll(distributedTasks);
                    _logger.LogInformation("分布式缓存预热批次完成，处理了 {Count} 个项目", batch.Count);
                }
                
                _logger.LogInformation("分布式缓存预热完成");
            }

            _logger.LogInformation("缓存预热全部完成");
        }
    }

    #endregion
}