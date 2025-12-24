using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Data;
using System.Linq.Expressions;
using FakeMicro.Utilities;
using FakeMicro.DatabaseAccess.Interfaces;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using Npgsql;
using Orleans;
using FakeMicro.Interfaces.Models;
using FakeMicro.Shared.Exceptions;

namespace FakeMicro.DatabaseAccess;


/// <summary>
/// SqlSugar仓储实现
/// 基于SqlSugar ORM实现通用仓储接口和SQL特定接口
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
    public class SqlSugarRepository<TEntity, TKey> : IRepository<TEntity, TKey>, ISqlRepository<TEntity, TKey> where TEntity : class, new()
    {
        private readonly ISqlSugarClient _db;
        private readonly ILogger<SqlSugarRepository<TEntity, TKey>> _logger;
        private readonly IQueryCacheManager? _queryCacheManager;
        // 使用AsyncLocal存储当前事务中的客户端，确保异步上下文中的事务一致性
        private static readonly AsyncLocal<ISqlSugarClient?> _transactionClient = new AsyncLocal<ISqlSugarClient?>();
        // 连接健康检查标志
        private bool _connectionHealthy = true;
        // 上次连接异常时间
        private DateTime? _lastConnectionErrorTime;
        // 连接恢复检测间隔（秒）
        private const int ConnectionRecoveryCheckIntervalSeconds = 30;
        
        /// <summary>
        /// 检查是否可以尝试恢复连接
        /// 实现退避策略，避免频繁检查导致性能问题
        /// </summary>
        private bool CanAttemptConnectionRecovery()
        {
            // 首次异常或时间间隔已过
            if (!_lastConnectionErrorTime.HasValue)
            {
                _lastConnectionErrorTime = DateTime.UtcNow;
                return true;
            }
            
            // 检查是否超过了检测间隔
            var timeSinceLastCheck = DateTime.UtcNow - _lastConnectionErrorTime.Value;
            if (timeSinceLastCheck.TotalSeconds >= ConnectionRecoveryCheckIntervalSeconds)
            {
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// 数据库操作重试策略配置
        /// </summary>
        private readonly RetryPolicy _retryPolicy;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="db">SqlSugar客户端</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="queryCacheManager">查询缓存管理器</param>
        public SqlSugarRepository(ISqlSugarClient db, ILogger<SqlSugarRepository<TEntity, TKey>> logger, IQueryCacheManager? queryCacheManager = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queryCacheManager = queryCacheManager;
            
            // 初始化重试策略
            _retryPolicy = new RetryPolicy(logger, maxRetries: 3, initialDelayMs: 100, maxDelayMs: 2000);
            
            // 配置AOP监控
            ConfigureAop();
        }
        
        /// <summary>
        /// 配置AOP监控和拦截
        /// 增加执行时间统计和性能监控
        /// </summary>
        private void ConfigureAop()
        {
            // SQL执行前拦截 - 添加执行时间记录
            _db.Aop.OnLogExecuting = (sql, pars) =>
            {
                // 记录参数信息
                var paramValues = pars.Select(p => $"{p.ParameterName}={p.Value}").ToList();
                _logger.LogDebug("SQL执行前: {Sql}, 参数: {Parameters}", sql, string.Join(", ", paramValues));
            };
            // SQL执行异常拦截 - 增强异常处理
            _db.Aop.OnError = (ex) =>
            {
                // 识别连接异常并更新健康状态
                if (IsConnectionException(ex))
                {
                    _connectionHealthy = false;
                    _logger.LogError(ex, "数据库连接异常 - SQL执行失败");
                }
                else
                {
                    _logger.LogError(ex, "SQL执行异常");
                }
            };
        }

        /// <summary>
        /// 获取所有实体（实现接口方法）
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>实体集合</returns>
        public async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("开始获取所有实体: {EntityType}", typeof(TEntity).Name);
                
                // 尝试从缓存获取
                if (_queryCacheManager != null)
                {
                    var cacheKey = _queryCacheManager.GenerateCacheKey<TEntity>("GetAll");
                    var cachedResult = await _queryCacheManager.GetOrCreateAsync(cacheKey, async () =>
                    {
                        _logger.LogDebug("缓存未命中，执行数据库查询");
                        return await _retryPolicy.ExecuteWithRetryAsync(
                            async () => await GetSqlSugarClient().Queryable<TEntity>().With(SqlWith.NoLock).ToListAsync(),
                            $"获取所有实体-{typeof(TEntity).Name}",
                            cancellationToken);
                    });
                    
                    stopwatch.Stop();
                    _logger.LogInformation("从缓存获取所有实体成功: {EntityType}, 记录数: {Count}, 耗时: {ElapsedMs}ms", 
                        typeof(TEntity).Name, cachedResult.Count(), stopwatch.ElapsedMilliseconds);
                    return cachedResult;
                }
                
                // 无缓存时直接查询
                var result = await _retryPolicy.ExecuteWithRetryAsync(
                    async () => await GetSqlSugarClient().Queryable<TEntity>().With(SqlWith.NoLock).ToListAsync(),
                    $"获取所有实体-{typeof(TEntity).Name}",
                    cancellationToken);
                
                stopwatch.Stop();
                _logger.LogInformation("获取所有实体成功: {EntityType}, 记录数: {Count}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, result.Count(), stopwatch.ElapsedMilliseconds);
                
                return result;
            }
            catch (Exception ex) when (ex is not DataAccessException)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "获取所有实体失败: {EntityType}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
                throw new DataAccessException($"获取所有实体失败: {typeof(TEntity).Name}", ex);
            }
        }

        /// <summary>
        /// 获取分页实体（实现接口方法并支持缓存）
        /// </summary>
        public async Task<PagedResult<TEntity>> GetPagedAsync(int pageNumber, int pageSize, 
            Expression<Func<TEntity, object>>? orderBy = null, 
            bool isDescending = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // 参数验证和边界处理
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Min(Math.Max(1, pageSize), 1000); // 限制最大页大小
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation("开始获取分页实体: {EntityType}, 页码: {PageNumber}, 页大小: {PageSize}", 
                    typeof(TEntity).Name, pageNumber, pageSize);
                
                // 直接执行数据库查询
                _logger.LogDebug("执行数据库查询获取分页数据");
                var result = await ExecutePagedQueryWithRetry(pageNumber, pageSize, orderBy, isDescending, cancellationToken);
                
                stopwatch.Stop();
                _logger.LogInformation("获取分页实体成功: {EntityType}, 页码: {PageNumber}, 页大小: {PageSize}, 总记录数: {TotalCount}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, pageNumber, pageSize, result.TotalCount, stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex) when (ex is not DataAccessException)
            {
                stopwatch.Stop();
                
                // 检查是否为连接异常
                if (ex is SqlSugarException sqlEx && IsConnectionException(sqlEx))
                {
                    // 标记连接异常
                    _connectionHealthy = false;
                    _logger.LogError(ex, "数据库连接异常: {EntityType}, 页码: {PageNumber}, 页大小: {PageSize}, 耗时: {ElapsedMs}ms", 
                        typeof(TEntity).Name, pageNumber, pageSize, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogError(ex, "获取分页实体失败: {EntityType}, 页码: {PageNumber}, 页大小: {PageSize}, 耗时: {ElapsedMs}ms", 
                        typeof(TEntity).Name, pageNumber, pageSize, stopwatch.ElapsedMilliseconds);
                }
                
                throw new DataAccessException($"获取分页实体失败: {typeof(TEntity).Name}", ex);
            }
        }

        /// <summary>
        /// 获取所有实体的分页结果
        /// 支持缓存以提高查询性能
        /// </summary>
        public async Task<PagedResult<TEntity>> GetAllAsync(int pageIndex = 1, int pageSize = 100, bool useCache = false, int cacheMinutes = 5, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // 强制分页约束，避免全表扫描
            pageIndex = Math.Max(1, pageIndex);
            pageSize = Math.Min(Math.Max(1, pageSize), 1000); // 最大限制为1000条记录
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // 尝试从缓存获取
                if (useCache && _queryCacheManager != null)
                {
                    var cacheKey = _queryCacheManager.GeneratePagedCacheKey<TEntity>(pageIndex, pageSize);
                    var cachedResult = await _queryCacheManager.GetOrCreateAsync(cacheKey, async () =>
                    {
                        _logger.LogDebug("缓存未命中，执行数据库查询");
                        return await ExecutePagedQuery(pageIndex, pageSize, cancellationToken);
                    }, cacheMinutes);
                    
                    stopwatch.Stop();
                    _logger.LogInformation("从缓存获取分页实体成功: {EntityType}, 页码: {PageIndex}, 耗时: {ElapsedMs}ms", 
                        typeof(TEntity).Name, pageIndex, stopwatch.ElapsedMilliseconds);
                    return cachedResult;
                }
                
                // 无缓存或缓存禁用时直接查询
                _logger.LogDebug("缓存未启用，执行数据库查询");
                var result = await ExecutePagedQuery(pageIndex, pageSize, cancellationToken);
                
                stopwatch.Stop();
                _logger.LogInformation("获取分页实体成功: {EntityType}, 页码: {PageIndex}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, pageIndex, stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex) when (ex is not DataAccessException)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "获取分页实体失败: {EntityType}, 页码: {PageIndex}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, pageIndex, stopwatch.ElapsedMilliseconds);
                throw new DataAccessException($"获取分页实体失败: {typeof(TEntity).Name}", ex);
            }
        }
        
        /// <summary>
        /// 执行分页查询的内部方法
        /// 包含重试策略以提高可靠性
        /// </summary>
        private async Task<PagedResult<TEntity>> ExecutePagedQuery(int pageIndex, int pageSize, CancellationToken cancellationToken)
        {
            return await _retryPolicy.ExecuteWithRetryAsync(async () =>
            {
                // 使用GetSqlSugarClient确保事务一致性
                var query = GetSqlSugarClient().Queryable<TEntity>().With(SqlWith.NoLock);
                
                // 使用反射安全地检查并添加租户过滤
                var entityType = typeof(TEntity);
                if (entityType.GetProperty("tenant_id") != null)
                {
                    // 注意：这里可以根据实际需求添加租户ID获取逻辑
                    // 为简化示例，这里暂不实现
                }
                
                // 执行分页查询
                var totalCount = await query.CountAsync(cancellationToken);
                var result = await query.ToPageListAsync(pageIndex, pageSize);
                
                // 返回分页结果
                return new PagedResult<TEntity>
                {
                    Data = result,
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                   
                };
            }, $"获取所有实体分页结果-{typeof(TEntity).Name}", cancellationToken);
        }
        
        /// <summary>
        /// 执行带重试的分页查询内部方法
        /// </summary>
        private async Task<PagedResult<TEntity>> ExecutePagedQueryWithRetry(int pageNumber, int pageSize, 
            Expression<Func<TEntity, object>>? orderBy, bool isDescending, CancellationToken cancellationToken)
        {
            return await _retryPolicy.ExecuteWithRetryAsync(async () =>
            {
                // 使用GetSqlSugarClient确保事务一致性
                var query = GetSqlSugarClient().Queryable<TEntity>().With(SqlWith.NoLock);
                
                // 使用反射安全地检查并添加租户过滤
                var entityType = typeof(TEntity);
                if (entityType.GetProperty("tenant_id") != null)
                {
                    // 注意：这里可以根据实际需求添加租户ID获取逻辑
                    // 为简化示例，这里暂不实现
                }
                
                // 排序
                if (orderBy != null)
                {
                    query = isDescending ? query.OrderBy(orderBy, OrderByType.Desc) : query.OrderBy(orderBy);
                }
                
                // 执行分页查询
                var pageResult = await query.ToPageListAsync(pageNumber, pageSize);
                
                // 获取总数
                var totalCount = query.Count();
                
                return new PagedResult<TEntity>
                {
                    Data = pageResult,
                    PageIndex = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                   
                };
            }, $"获取分页实体-{typeof(TEntity).Name}", cancellationToken);
        }

        /// <summary>
        /// 获取所有实体（带导航属性）
        /// </summary>
        public async Task<IEnumerable<TEntity>> GetAllWithIncludesAsync(
            CancellationToken cancellationToken = default,
            params Expression<Func<TEntity, object>>[] includes)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("开始获取带导航属性的所有实体: {EntityType}, 包含导航属性数: {IncludeCount}", 
                    typeof(TEntity).Name, includes.Length);
                
                var result = await _retryPolicy.ExecuteWithRetryAsync(async () =>
                {
                    var query = GetSqlSugarClient().Queryable<TEntity>();
                    
                    // 处理导航属性
                    foreach (var include in includes)
                    {
                        query = query.Includes(include);
                    }
                    
                    // 注意：移除了AsSplitQuery优化，因为当前版本的SqlSugar中SqlWith类型没有此方法
                    
                    return await query.ToListAsync();
                }, $"获取带导航属性的实体-{typeof(TEntity).Name}", cancellationToken);
                
                stopwatch.Stop();
                _logger.LogInformation("获取带导航属性的实体成功: {EntityType}, 记录数: {Count}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, result.Count(), stopwatch.ElapsedMilliseconds);
                
                return result;
            }
            catch (Exception ex) when (ex is not DataAccessException)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "获取带导航属性的实体失败: {EntityType}, 包含导航属性数: {IncludeCount}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, includes.Length, stopwatch.ElapsedMilliseconds);
                throw new DataAccessException($"获取带导航属性的实体失败: {typeof(TEntity).Name}", ex);
            }
        }

    /// <summary>
    /// 根据主键获取实体
    /// 支持连接健康检查、异常恢复和查询缓存
    /// </summary>
    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("开始获取实体: {EntityType}, ID: {Id}", typeof(TEntity).Name, id);
            
            // 尝试从缓存获取
            if (_queryCacheManager != null)
            {
                var idStr = id?.ToString() ?? string.Empty;
                var cacheKey = _queryCacheManager.GenerateCacheKey<TEntity>(idStr);
                var cachedResult = await _queryCacheManager.GetOrCreateAsync(cacheKey, async () =>
                {
                    _logger.LogDebug("缓存未命中，执行数据库查询获取实体: {EntityType}, ID: {Id}", typeof(TEntity).Name, id);
                    return await ExecuteGetByIdQuery(id, cancellationToken);
                }, 5); // 默认缓存5分钟
                
                stopwatch.Stop();
                _logger.LogInformation("从缓存获取实体成功: {EntityType}, ID: {Id}, 是否存在: {Exists}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, id, cachedResult != null, stopwatch.ElapsedMilliseconds);
                return cachedResult;
            }
            
            // 无缓存时直接查询
            _logger.LogDebug("缓存未启用，执行数据库查询获取实体: {EntityType}, ID: {Id}", typeof(TEntity).Name, id);
            var result = await ExecuteGetByIdQuery(id, cancellationToken);
            
            stopwatch.Stop();
            _logger.LogInformation("获取实体成功: {EntityType}, ID: {Id}, 是否存在: {Exists}, 耗时: {ElapsedMs}ms", 
                typeof(TEntity).Name, id, result != null, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            stopwatch.Stop();
            
            // 检查是否为连接异常
            if (IsConnectionException(ex))
            {
                // 标记连接异常
                _connectionHealthy = false;
                _logger.LogError(ex, "数据库连接异常: {EntityType}, ID: {Id}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, id, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogError(ex, "获取实体失败: {EntityType}, ID: {Id}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, id, stopwatch.ElapsedMilliseconds);
            }
            
            throw new DataAccessException($"获取实体失败: {typeof(TEntity).Name}, ID: {id}", ex);
        }
    }
    
    /// <summary>
    /// 执行根据主键获取实体的查询
    /// </summary>
    private async Task<TEntity?> ExecuteGetByIdQuery(TKey id, CancellationToken cancellationToken)
    {
        return await _retryPolicy.ExecuteWithRetryAsync(
            async () => {
                // 使用GetSqlSugarClient确保事务一致性
                var query = GetSqlSugarClient().Queryable<TEntity>().With(SqlWith.NoLock);
                
                // 使用反射安全地检查并添加租户过滤
                var entityType = typeof(TEntity);
                if (entityType.GetProperty("tenant_id") != null)
                {
                    // 注意：这里可以根据实际需求添加租户ID获取逻辑
                    // 为简化示例，这里暂不实现
                }
                
                return await query.InSingleAsync(id);
            },
            $"获取实体ById-{typeof(TEntity).Name}",
            cancellationToken);
    }
    
    /// <summary>
    /// 判断是否为数据库连接异常
    /// 增强的异常检测逻辑，支持PostgreSQL特定异常和内部异常递归检查
    /// </summary>
    private bool IsConnectionException(Exception ex)
    {
        // 递归检查内部异常
        Exception? currentException = ex;
        while (currentException != null)
        {
            // 检查是否为PostgreSQL特定异常
            if (currentException is Npgsql.NpgsqlException npgsqlEx)
            {
                // 检查PostgreSQL错误码或SQL状态
                if (npgsqlEx.SqlState != null)
                {
                    // 连接相关的SQL状态码
                    var connectionSqlStates = new[]
                    {
                        "08001", // SQLSTATE 08001 - 客户端无法建立连接
                        "08003", // SQLSTATE 08003 - 连接不存在
                        "08004", // SQLSTATE 08004 - 服务器拒绝连接
                        "08006", // SQLSTATE 08006 - 连接失败
                        "08007", // SQLSTATE 08007 - 事务中的连接失败
                        "08P01"  // SQLSTATE 08P01 - 协议错误
                    };
                    
                    if (connectionSqlStates.Contains(npgsqlEx.SqlState))
                    {
                        return true;
                    }
                }
            }
            
            // 检查异常消息模式
            var exceptionMessage = currentException.Message?.ToLowerInvariant() ?? "";
            
            // 常见的连接异常关键词（优化的模式匹配）
            var connectionKeywords = new[]
            {
                "connection refused", "connection timeout", "could not connect",
                "no connection", "broken pipe", "network error", "socket error",
                "timeout expired", "ssl connection failed", "database connection failed",
                "pg: could not connect to server", "unable to open database",
                "connection reset", "network is unreachable", "host is down",
                "operation timed out", "connection closed by peer"
            };
            
            // 中文异常关键词
            var chineseKeywords = new[]
            {"连接拒绝", "连接超时", "无法连接", "网络错误", "连接断开", "主机不可达"};
            
            // 检查是否包含连接异常关键词
            if (connectionKeywords.Any(keyword => exceptionMessage.Contains(keyword)) ||
                chineseKeywords.Any(keyword => exceptionMessage.Contains(keyword)))
            {
                return true;
            }
            
            // 检查是否为底层IOException
            if (currentException is System.IO.IOException)
            {
                return true;
            }
            
            // 检查内部异常
            currentException = currentException.InnerException;
        }
        
        return false;
    }

    /// <summary>
    /// 根据主键获取实体（带导航属性）
    /// </summary>
    public async Task<TEntity?> GetByIdWithIncludesAsync(TKey id, 
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includes)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("开始获取带导航属性的实体: {EntityType}, ID: {Id}, 包含导航属性数: {IncludeCount}", 
                typeof(TEntity).Name, id, includes.Length);
            
            var result = await _retryPolicy.ExecuteWithRetryAsync(async () =>
            {
                var query = GetSqlSugarClient().Queryable<TEntity>();
                
                // 处理导航属性
                foreach (var include in includes)
                {
                    query = query.Includes(include);
                }
                
                // 注意：移除了AsSplitQuery优化，因为当前版本的SqlSugar中SqlWith类型没有此方法
                
                return await query.InSingleAsync(id);
            }, $"获取带导航属性的实体ById-{typeof(TEntity).Name}", cancellationToken);
            
            stopwatch.Stop();
            _logger.LogInformation("获取带导航属性的实体成功: {EntityType}, ID: {Id}, 是否存在: {Exists}, 耗时: {ElapsedMs}ms", 
                typeof(TEntity).Name, id, result != null, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "获取带导航属性的实体失败: {EntityType}, ID: {Id}, 包含导航属性数: {IncludeCount}, 耗时: {ElapsedMs}ms", 
                typeof(TEntity).Name, id, includes.Length, stopwatch.ElapsedMilliseconds);
            throw new DataAccessException($"获取带导航属性的实体失败: {typeof(TEntity).Name}, ID: {id}", ex);
        }
    }

    /// <summary>
    /// 根据条件获取实体
    /// </summary>
    public async Task<IEnumerable<TEntity>> GetByConditionAsync(Expression<Func<TEntity, bool>> predicate, 
        CancellationToken cancellationToken = default)
    {
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }
        
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("开始根据条件获取实体: {EntityType}", typeof(TEntity).Name);
            
            var result = await _retryPolicy.ExecuteWithRetryAsync(
                async () => await GetSqlSugarClient().Queryable<TEntity>().With(SqlWith.NoLock).Where(predicate).ToListAsync(),
                $"根据条件获取实体-{typeof(TEntity).Name}",
                cancellationToken);
            
            stopwatch.Stop();
            _logger.LogInformation("根据条件获取实体成功: {EntityType}, 记录数: {Count}, 耗时: {ElapsedMs}ms", 
                typeof(TEntity).Name, result.Count(), stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            stopwatch.Stop();
            
            // 检查是否为连接异常
            if (IsConnectionException(ex))
            {
                // 标记连接异常
                _connectionHealthy = false;
                _logger.LogError(ex, "数据库连接异常: {EntityType}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogError(ex, "根据条件获取实体失败: {EntityType}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
            }
            
            throw new DataAccessException($"根据条件获取实体失败: {typeof(TEntity).Name}", ex);
        }
    }

    /// <summary>
    /// 根据条件获取分页实体
    /// </summary>
    public async Task<PagedResult<TEntity>> GetPagedByConditionAsync(Expression<Func<TEntity, bool>> predicate,
        int pageNumber, int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool isDescending = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        // 参数验证和边界处理
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }
        
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Min(Math.Max(1, pageSize), 1000); // 限制最大页大小
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("开始根据条件获取分页实体: {EntityType}, 页码: {PageNumber}, 页大小: {PageSize}", 
                typeof(TEntity).Name, pageNumber, pageSize);
            
            // 执行数据库查询
            _logger.LogDebug("执行数据库查询");
            var result = await ExecutePagedConditionQueryWithRetry(predicate, pageNumber, pageSize, orderBy, isDescending, cancellationToken);
            
            stopwatch.Stop();
            _logger.LogInformation("根据条件获取分页实体成功: {EntityType}, 页码: {PageNumber}, 页大小: {PageSize}, 总记录数: {TotalCount}, 耗时: {ElapsedMs}ms", 
                typeof(TEntity).Name, pageNumber, pageSize, result.TotalCount, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            stopwatch.Stop();
            
            // 检查是否为连接异常
            if (IsConnectionException(ex))
            {
                // 标记连接异常
                _connectionHealthy = false;
                _logger.LogError(ex, "数据库连接异常: {EntityType}, 页码: {PageNumber}, 页大小: {PageSize}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, pageNumber, pageSize, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogError(ex, "根据条件获取分页实体失败: {EntityType}, 页码: {PageNumber}, 页大小: {PageSize}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, pageNumber, pageSize, stopwatch.ElapsedMilliseconds);
            }
            
            throw new DataAccessException($"根据条件获取分页实体失败: {typeof(TEntity).Name}", ex);
        }
    }
    
    /// <summary>
    /// 执行带重试的条件分页查询内部方法
    /// </summary>
    private async Task<PagedResult<TEntity>> ExecutePagedConditionQueryWithRetry(
        Expression<Func<TEntity, bool>> predicate, 
        int pageNumber, 
        int pageSize,
        Expression<Func<TEntity, object>>? orderBy, 
        bool isDescending, 
        CancellationToken cancellationToken)
    {
        return await _retryPolicy.ExecuteWithRetryAsync(async () =>
        {
            // 使用GetSqlSugarClient确保事务一致性
            var query = GetSqlSugarClient().Queryable<TEntity>().With(SqlWith.NoLock).Where(predicate);
            
            // 使用反射安全地检查并添加租户过滤
            var entityType = typeof(TEntity);
            if (entityType.GetProperty("tenant_id") != null)
            {
                // 注意：这里可以根据实际需求添加租户ID获取逻辑
                // 为简化示例，这里暂不实现
            }
            
            // 排序
            if (orderBy != null)
            {
                query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
            }
            
            // 执行分页查询
            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query.ToPageListAsync(pageNumber, pageSize);
            
            // 创建分页结果
            return new PagedResult<TEntity> 
            { 
                Data = items, 
                TotalCount = totalCount, 
                PageIndex = pageNumber, 
                PageSize = pageSize,
           
            };
        }, $"条件分页查询-{typeof(TEntity).Name}", cancellationToken);
    }

    /// <summary>
    /// 检查实体是否存在
    /// </summary>
    public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("开始检查实体是否存在: {EntityType}", typeof(TEntity).Name);
            
            var result = await _retryPolicy.ExecuteWithRetryAsync(async () =>
            {
                // 使用GetSqlSugarClient确保事务一致性
                return await GetSqlSugarClient().Queryable<TEntity>().AnyAsync(predicate, cancellationToken);
            }, $"检查实体是否存在-{typeof(TEntity).Name}", cancellationToken);
            
            stopwatch.Stop();
            _logger.LogInformation("检查实体是否存在完成: {EntityType}, 结果: {Exists}, 耗时: {ElapsedMs}ms", 
                typeof(TEntity).Name, result, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            stopwatch.Stop();
            
            // 检查是否为连接异常
            if (IsConnectionException(ex))
            {
                // 标记连接异常
                _connectionHealthy = false;
                _logger.LogError(ex, "数据库连接异常: {EntityType}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogError(ex, "检查实体是否存在失败: {EntityType}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
            }
            
            throw new DataAccessException($"检查实体是否存在失败: {typeof(TEntity).Name}", ex);
        }
    }

    /// <summary>
    /// 获取实体数量
    /// </summary>
    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("开始获取实体数量: {EntityType}", typeof(TEntity).Name);
            
            var result = await _retryPolicy.ExecuteWithRetryAsync(async () =>
            {
                // 使用GetSqlSugarClient确保事务一致性
                var query = GetSqlSugarClient().Queryable<TEntity>();
                if (predicate != null)
                {
                    query = query.Where(predicate);
                }
                
                return await query.CountAsync(cancellationToken);
            }, $"获取实体数量-{typeof(TEntity).Name}", cancellationToken);
            
            stopwatch.Stop();
            _logger.LogInformation("获取实体数量完成: {EntityType}, 数量: {Count}, 耗时: {ElapsedMs}ms", 
                typeof(TEntity).Name, result, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            stopwatch.Stop();
            
            // 检查是否为连接异常
            if (IsConnectionException(ex))
            {
                // 标记连接异常
                _connectionHealthy = false;
                _logger.LogError(ex, "数据库连接异常: {EntityType}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogError(ex, "获取实体数量失败: {EntityType}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
            }
            
            throw new DataAccessException($"获取实体数量失败: {typeof(TEntity).Name}", ex);
        }
    }

    /// <summary>
    /// 添加实体
    /// 支持连接健康检查和异常恢复
    /// </summary>
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("开始添加实体: {EntityType}", typeof(TEntity).Name);
            
            // 设置创建和更新时间
            var now = DateTime.UtcNow;
            var entityType = entity.GetType();
            
            var createdAtProp = entityType.GetProperty("CreatedAt");
            if (createdAtProp != null)
            {
                createdAtProp.SetValue(entity, now);
            }
            var updatedAtProp = entityType.GetProperty("UpdatedAt");
            if (updatedAtProp != null)
            {
                updatedAtProp.SetValue(entity, now);
            }
            
            // 使用重试策略执行添加操作
            await _retryPolicy.ExecuteWithRetryAsync(async () =>
            {
                // 使用GetSqlSugarClient确保事务一致性
                await GetSqlSugarClient().Insertable(entity).ExecuteReturnEntityAsync();
            }, $"添加实体-{typeof(TEntity).Name}", cancellationToken);
            
            // 清除实体类型相关缓存
            if (_queryCacheManager != null)
            {
                await _queryCacheManager.RemoveEntityCacheAsync(typeof(TEntity));
                _logger.LogDebug("已清除实体类型缓存: {EntityType}", typeof(TEntity).Name);
            }
            
            stopwatch.Stop();
            _logger.LogInformation("添加实体成功: {EntityType}, 耗时: {ElapsedMs}ms", 
                typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            stopwatch.Stop();
            
            // 检查是否为连接异常
            if (IsConnectionException(ex))
            {
                // 标记连接异常
                _connectionHealthy = false;
                _logger.LogError(ex, "数据库连接异常: {EntityType}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
            }
            else if (ex.Message.Contains("duplicate key"))
            {
                // 主键冲突处理
                _logger.LogWarning(ex, "主键冲突: {EntityType}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
                throw new DataAccessException($"添加实体失败，主键冲突: {typeof(TEntity).Name}", ex);
            }
            else
            {
                _logger.LogError(ex, "添加实体失败: {EntityType}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
            }
            
            throw new DataAccessException($"添加实体失败: {typeof(TEntity).Name}", ex);
        }
    }

    /// <summary>
    /// 批量添加实体
    /// </summary>
    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _db.Insertable(entities.ToList()).ExecuteCommandAsync();
    }

    /// <summary>
    /// 分批添加实体
    /// </summary>
    public async Task AddBatchedAsync(IEnumerable<TEntity> entities, int batchSize = 1000, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var entityList = entities.ToList();
        for (int i = 0; i < entityList.Count; i += batchSize)
        {
            var batch = entityList.Skip(i).Take(batchSize).ToList();
            await _db.Insertable(batch).ExecuteCommandAsync();
            
            cancellationToken.ThrowIfCancellationRequested();
        }
        
        // 清除实体类型缓存
        if (_queryCacheManager != null)
        {
            await _queryCacheManager.RemoveEntityCacheAsync(typeof(TEntity));
            _logger.LogDebug("已清除实体类型缓存: {EntityType}", typeof(TEntity).Name);
        }
    }

    /// <summary>
    /// 更新实体
    /// 支持连接健康检查和异常恢复
    /// </summary>
    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("开始更新实体: {EntityType}", typeof(TEntity).Name);
            
            // 检查是否有更新时间字段，如果有则自动更新
            //var updatedAtProperty = typeof(TEntity).GetProperty("UpdatedAt");
            //if (updatedAtProperty != null)
            //{
            //    updatedAtProperty.SetValue(entity, DateTime.UtcNow);
            //}
            
            // 使用重试策略执行更新操作
            var result = await _retryPolicy.ExecuteWithRetryAsync(async () =>
            {
                // 使用GetSqlSugarClient确保事务一致性
                var updateable = GetSqlSugarClient().Updateable(entity);
                return await updateable.ExecuteCommandAsync();
            }, $"更新实体-{typeof(TEntity).Name}", cancellationToken);
            
            stopwatch.Stop();
            
            if (result == 0)
            {
                _logger.LogWarning("更新实体失败: {EntityType}, 可能记录不存在或已被删除, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation("更新实体成功: {EntityType}, 受影响行数: {RowsAffected}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, result, stopwatch.ElapsedMilliseconds);
                
                // 清除实体相关缓存
                if (_queryCacheManager != null)
                {
                    // 尝试获取实体主键
                    var entityType = typeof(TEntity);
                    var idProperty = entityType.GetProperty("id") ?? entityType.GetProperty("Id") ?? entityType.GetProperty("ID");
                    if (idProperty != null)
                    {
                        var idValue = idProperty.GetValue(entity)?.ToString();
                        if (!string.IsNullOrEmpty(idValue))
                        {
                            var cacheKey = _queryCacheManager.GenerateCacheKey<TEntity>(idValue);
                            await _queryCacheManager.RemoveAsync(cacheKey);
                            _logger.LogDebug("已清除单个实体缓存: {EntityType}, ID: {Id}", typeof(TEntity).Name, idValue);
                        }
                    }
                    // 同时清除实体类型的所有缓存
                    await _queryCacheManager.RemoveEntityCacheAsync(typeof(TEntity));
                    _logger.LogDebug("已清除实体类型缓存: {EntityType}", typeof(TEntity).Name);
                }
            }
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            stopwatch.Stop();
            
            // 检查是否为连接异常
            if (IsConnectionException(ex))
            {
                // 标记连接异常
                _connectionHealthy = false;
                _logger.LogError(ex, "数据库连接异常: {EntityType}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
            }
            else if (ex.Message.Contains("duplicate key"))
            {
                // 唯一键冲突处理
                _logger.LogWarning(ex, "唯一键冲突: {EntityType}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
                throw new DataAccessException($"更新实体失败，唯一键冲突: {typeof(TEntity).Name}", ex);
            }
            else
            {
                _logger.LogError(ex, "更新实体失败: {EntityType}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
            }
            
            throw new DataAccessException($"更新实体失败: {typeof(TEntity).Name}", ex);
        }
    }

    /// <summary>
    /// 部分更新实体
    /// </summary>
    public async Task UpdatePartialAsync(TEntity entity, params Expression<Func<TEntity, object>>[] properties)
    {
        if (properties == null || properties.Length == 0)
        {
            throw new ArgumentException("至少需要指定一个要更新的属性", nameof(properties));
        }

        var update = _db.Updateable(entity);
        foreach (var property in properties)
        {
            update = update.UpdateColumns(property);
        }

        await update.ExecuteCommandAsync();

        // 清除实体类型缓存
        if (_queryCacheManager != null)
        {
            // 清除单个实体缓存
            var entityType = typeof(TEntity);
            var idProperty = entityType.GetProperty("id") ?? entityType.GetProperty("Id") ?? entityType.GetProperty("ID");
            if (idProperty != null)
            {
                var idValue = idProperty.GetValue(entity)?.ToString();
                if (!string.IsNullOrEmpty(idValue))
                {
                    var cacheKey = _queryCacheManager.GenerateCacheKey<TEntity>(idValue);
                    await _queryCacheManager.RemoveAsync(cacheKey);
                    _logger.LogDebug("已清除单个实体缓存: {EntityType}, ID: {Id}", typeof(TEntity).Name, idValue);
                }
            }

            // 同时清除实体类型的所有缓存
            await _queryCacheManager.RemoveEntityCacheAsync(typeof(TEntity));
            _logger.LogDebug("已清除实体类型缓存: {EntityType}", typeof(TEntity).Name);
        }
    }

    /// <summary>
    /// 批量更新实体
    /// </summary>
    public async Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _db.Updateable(entities.ToList()).ExecuteCommandAsync();
        
        // 清除实体类型缓存
        if (_queryCacheManager != null)
        {
            await _queryCacheManager.RemoveEntityCacheAsync(typeof(TEntity));
            _logger.LogDebug("已清除实体类型缓存: {EntityType}", typeof(TEntity).Name);
        }
    }

    /// <summary>
    /// 删除实体
    /// </summary>
    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _db.Deleteable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 根据主键删除实体
    /// 支持连接健康检查和异常恢复
    /// </summary>
    public async Task DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("开始删除实体: {EntityType}, ID: {Id}", typeof(TEntity).Name, id);
            
            // 使用重试策略执行删除操作
            await _retryPolicy.ExecuteWithRetryAsync(async () =>
            {
                // 使用GetSqlSugarClient确保事务一致性
                var db = GetSqlSugarClient();
                
                // 优先尝试软删除
                var entity = await GetByIdAsync(id, cancellationToken);
                if (entity != null)
                {
                    var isDeletedProperty = typeof(TEntity).GetProperty("is_deleted");
                    if (isDeletedProperty != null && isDeletedProperty.PropertyType == typeof(bool))
                    {
                        isDeletedProperty.SetValue(entity, true);
                        
                        var updatedAtProperty = typeof(TEntity).GetProperty("UpdatedAt");
                        if (updatedAtProperty != null)
                        {
                            updatedAtProperty.SetValue(entity, DateTime.UtcNow);
                        }
                        
                        await UpdateAsync(entity, cancellationToken);
                        return;
                    }
                }
                
                // 如果不支持软删除或实体不存在，则执行硬删除
                await db.Deleteable<TEntity>().In(id).ExecuteCommandAsync();
            }, $"删除实体ById-{typeof(TEntity).Name}", cancellationToken);
            
            stopwatch.Stop();
            
            // 清除实体相关缓存
            if (_queryCacheManager != null)
            {
                // 清除单个实体缓存
                var idStr = id?.ToString() ?? string.Empty;
                var cacheKey = _queryCacheManager.GenerateCacheKey<TEntity>(idStr);
                await _queryCacheManager.RemoveAsync(cacheKey);
                _logger.LogDebug("已清除单个实体缓存: {EntityType}, ID: {Id}", typeof(TEntity).Name, id);
                
                // 同时清除实体类型的所有缓存
                await _queryCacheManager.RemoveEntityCacheAsync(typeof(TEntity));
                _logger.LogDebug("已清除实体类型缓存: {EntityType}", typeof(TEntity).Name);
            }
            
            // 确定是软删除还是硬删除
            var isSoftDelete = typeof(TEntity).GetProperty("is_deleted") != null;
            _logger.LogInformation("删除实体成功: {EntityType}, ID: {Id}, 操作类型: {DeleteType}, 耗时: {ElapsedMs}ms", 
                typeof(TEntity).Name, id, isSoftDelete ? "软删除" : "硬删除", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            stopwatch.Stop();
            
            // 检查是否为连接异常
            if (IsConnectionException(ex))
            {
                // 标记连接异常
                _connectionHealthy = false;
                _logger.LogError(ex, "数据库连接异常: {EntityType}, ID: {Id}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, id, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogError(ex, "删除实体失败: {EntityType}, ID: {Id}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, id, stopwatch.ElapsedMilliseconds);
            }
            
            throw new DataAccessException($"删除实体失败: {typeof(TEntity).Name}, ID: {id}", ex);
        }
    }

    /// <summary>
    /// 批量删除实体
    /// </summary>
    public async Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _db.Deleteable(entities.ToList()).ExecuteCommandAsync();
    }

    /// <summary>
    /// 分批删除实体
    /// </summary>
    public async Task DeleteBatchedAsync(IEnumerable<TEntity> entities, int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var entityList = entities.ToList();
        for (int i = 0; i < entityList.Count; i += batchSize)
        {
            var batch = entityList.Skip(i).Take(batchSize).ToList();
            await _db.Deleteable(batch).ExecuteCommandAsync();
            
            cancellationToken.ThrowIfCancellationRequested();
        }
        
        // 清除实体类型缓存
        if (_queryCacheManager != null)
        {
            await _queryCacheManager.RemoveEntityCacheAsync(typeof(TEntity));
            _logger.LogDebug("已清除实体类型缓存: {EntityType}", typeof(TEntity).Name);
        }
    }

    /// <summary>
    /// 根据条件删除实体
    /// </summary>
    public async Task<int> DeleteByConditionAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await _db.Deleteable<TEntity>().Where(predicate).ExecuteCommandAsync();
        
        // 清除实体类型缓存
        if (_queryCacheManager != null && result > 0)
        {
            await _queryCacheManager.RemoveEntityCacheAsync(typeof(TEntity));
            _logger.LogDebug("已清除实体类型缓存: {EntityType}", typeof(TEntity).Name);
        }
        
        return result;
    }

    /// <summary>
    /// 保存更改
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // SqlSugar的更改是立即执行的，这里返回0表示没有需要保存的更改
        
        // 清除实体类型缓存
        if (_queryCacheManager != null)
        {
            await _queryCacheManager.RemoveEntityCacheAsync(typeof(TEntity));
            _logger.LogDebug("已清除实体类型缓存: {EntityType}", typeof(TEntity).Name);
        }
        
        return await Task.FromResult(0);
    }

    /// <summary>
        /// 执行事务
        /// 统一事务处理逻辑，支持嵌套事务，确保资源正确释放
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
        { 
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("开始事务处理: {EntityType}", 
                    typeof(TEntity).Name);
                
                // 检查是否已经在事务中
                if (_transactionClient.Value != null)
                {
                    _logger.LogDebug("已在事务中，直接执行操作: {EntityType}", typeof(TEntity).Name);
                    // 已经在事务中，直接执行操作
                    await action();
                    
                    stopwatch.Stop();
                    _logger.LogInformation("嵌套事务操作成功: {EntityType}, 耗时: {ElapsedMs}ms", 
                        typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
                    return;
                }
                
                // 使用重试策略执行事务操作
                await _retryPolicy.ExecuteWithRetryAsync(async () =>
                {
                    try
                    {
                        // 使用UseTranAsync进行事务管理，更安全和简洁，支持指定隔离级别
                        await _db.Ado.UseTranAsync(async () => {
                            try
                            {
                                // 设置事务客户端到AsyncLocal
                                _transactionClient.Value = _db;
                                await action();
                                return new { Success = true };
                            }
                            finally
                            {
                                // 清理事务客户端
                                ClearTransactionClient();
                            }
                        });
                        
                        // 应用取消令牌
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // 事务特定异常处理
                        if (ex.Message.Contains("deadlock"))
                        {
                            _logger.LogWarning(ex, "事务发生死锁: {EntityType}", typeof(TEntity).Name);
                            // 死锁错误应该可以重试，所以这里直接抛出
                            throw;
                        }
                        // 检查是否为连接异常
                        else if (IsConnectionException(ex))
                        {
                            _connectionHealthy = false;
                            _lastConnectionErrorTime = DateTime.UtcNow;
                            _logger.LogError(ex, "数据库连接异常: {EntityType}", typeof(TEntity).Name);
                            throw;
                        }
                        throw;
                    }
                }, $"事务操作-{typeof(TEntity).Name}", cancellationToken);
                
                stopwatch.Stop();
                _logger.LogInformation("事务执行成功: {EntityType}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
                // 确保清理事务客户端
                ClearTransactionClient();
                stopwatch.Stop();
                _logger.LogWarning("事务被取消: {EntityType}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex) when (ex is not DataAccessException)
            {
                // 确保清理事务客户端
                ClearTransactionClient();
                stopwatch.Stop();
                _logger.LogError(ex, "事务执行失败: {EntityType}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
                throw new DataAccessException("事务执行失败", ex);
            }
        }

    /// <summary>
        /// 执行事务并返回结果
        /// 统一事务处理逻辑，支持嵌套事务，确保资源正确释放
        /// </summary>
        /// <typeparam name="TResult">返回结果类型</typeparam>
        /// <param name="action">要执行的操作</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<Task<TResult>> action,
            CancellationToken cancellationToken = default)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }
            
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("开始事务处理: {EntityType}",typeof(TEntity).Name);
                
                // 检查是否已经在事务中
                if (_transactionClient.Value != null)
                {
                    _logger.LogDebug("已在事务中，直接执行操作: {EntityType}", typeof(TEntity).Name);
                    // 已经在事务中，直接执行操作
                    var result = await action();
                    
                    stopwatch.Stop();
                    _logger.LogInformation("嵌套事务操作成功: {EntityType}, 耗时: {ElapsedMs}ms", 
                        typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
                    return result;
                }
                
                // 使用重试策略执行事务操作
                TResult transactionResult = await _retryPolicy.ExecuteWithRetryAsync(async () =>
                {
                    TResult txResult = default!;
                    try
                    {
                        // 使用UseTranAsync进行事务管理，更安全和简洁，支持指定隔离级别
                        await _db.Ado.UseTranAsync(async () => {
                            try
                            {
                                // 设置事务客户端到AsyncLocal
                                _transactionClient.Value = _db;
                                txResult = await action();
                                return new { Success = true, Data = txResult };
                            }
                            finally
                            {
                                // 清理事务客户端
                                ClearTransactionClient();
                            }
                        });
                        
                        // 应用取消令牌
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        return txResult;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // 事务特定异常处理
                        if (ex.Message.Contains("deadlock"))
                        {
                            _logger.LogWarning(ex, "事务发生死锁: {EntityType}", typeof(TEntity).Name);
                            // 死锁错误应该可以重试，所以这里直接抛出
                            throw;
                        }
                        // 检查是否为连接异常
                        else if (IsConnectionException(ex))
                        {
                            _connectionHealthy = false;
                            _lastConnectionErrorTime = DateTime.UtcNow;
                            _logger.LogError(ex, "数据库连接异常: {EntityType}", typeof(TEntity).Name);
                            throw;
                        }
                        throw;
                    }
                }, $"事务操作-{typeof(TEntity).Name}", cancellationToken);
                
                stopwatch.Stop();
                _logger.LogInformation("事务执行成功: {EntityType}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
                
                return transactionResult;
            }
            catch (OperationCanceledException)
            {
                // 确保清理事务客户端
                ClearTransactionClient();
                stopwatch.Stop();
                _logger.LogWarning("事务被取消: {EntityType}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex) when (ex is not DataAccessException)
            {
                // 确保清理事务客户端
                ClearTransactionClient();
                stopwatch.Stop();
                _logger.LogError(ex, "事务执行失败: {EntityType}, 耗时: {ElapsedMs}ms", 
                    typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
                throw new DataAccessException("事务执行失败", ex);
            }
        }

    /// <summary>
    /// 禁用实体跟踪（SqlSugar默认不跟踪实体，此方法为空实现）
    /// </summary>
    public void DisableTracking()
    {
        // SqlSugar默认不跟踪实体，无需特殊处理
    }

    /// <summary>
    /// 启用实体跟踪（SqlSugar默认不跟踪实体，此方法为空实现）
    /// </summary>
    public void EnableTracking()
    {
        // SqlSugar默认不跟踪实体，无需特殊处理
    }

    /// <summary>
    /// 清除实体跟踪缓存（SqlSugar默认不跟踪实体，此方法为空实现）
    /// </summary>
    public void ClearTracker()
    {
        // SqlSugar默认不跟踪实体，无需特殊处理
    }

    /// <summary>
    /// 获取SqlSugar客户端（用于高级操作）
    /// 统一连接管理，优先返回事务中的客户端，确保事务一致性
    /// 支持连接健康检查和自动恢复
    /// </summary>
    public ISqlSugarClient GetSqlSugarClient()
    {
        // 优先使用事务中的客户端，确保事务一致性
        var client = _transactionClient.Value ?? _db;
        
        // 连接健康检查（仅在非事务模式下）
        if (_transactionClient.Value == null && !_connectionHealthy)
        {
            // 实现连接恢复检测间隔，避免频繁检查
            if (CanAttemptConnectionRecovery())
            {
                try
                {
                    _logger.LogInformation("尝试恢复数据库连接: {EntityType}", typeof(TEntity).Name);
                    
                    // 验证连接 - 使用较短的超时设置
                    var stopwatch = Stopwatch.StartNew();
                    
                    // 验证连接
                    client.Ado.ExecuteCommand("SELECT 1");
                    stopwatch.Stop();
                    
                    _connectionHealthy = true;
                    _lastConnectionErrorTime = null;
                    _logger.LogInformation("数据库连接已恢复: {EntityType}, 检测耗时: {ElapsedMs}ms", 
                        typeof(TEntity).Name, stopwatch.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    _lastConnectionErrorTime = DateTime.UtcNow;
                    _logger.LogWarning(ex, "数据库连接仍然异常: {EntityType}", typeof(TEntity).Name);
                    // 不抛出异常，允许系统尝试恢复
                }
            }
        }
        
        return client;
    }
    
    /// <summary>
    /// 获取原生SqlSugar客户端（绕过事务上下文）
    /// 仅用于特殊场景，如独立的连接操作
    /// </summary>
    public ISqlSugarClient GetRawSqlSugarClient() => _db;
    
    /// <summary>
    /// 设置事务客户端（由事务服务调用）
    /// </summary>
    /// <summary>
    /// 设置事务客户端（由事务服务调用）
    /// 确保在事务上下文中使用正确的客户端实例
    /// </summary>
    internal void SetTransactionClient(ISqlSugarClient client)
    {
        if (client == null)
        {
            _logger.LogWarning("尝试设置空的事务客户端");
            return;
        }
        _transactionClient.Value = client;
        _logger.LogDebug("事务客户端已设置: {EntityType}", typeof(TEntity).Name);
    }
    
    /// <summary>
    /// 清除事务客户端（用于事务结束后的清理）
    /// 防止事务资源泄漏
    /// </summary>
    internal void ClearTransactionClient()
    {
        _transactionClient.Value = null;
        _logger.LogDebug("事务客户端已清除: {EntityType}", typeof(TEntity).Name);
    }

    /// <summary>
    /// 执行原始SQL查询
    /// </summary>
    public async Task<List<T>> ExecuteQueryAsync<T>(string sql, object? parameters = null, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _db.Ado.SqlQueryAsync<T>(sql, parameters);
    }

    /// <summary>
    /// 执行原始SQL命令
    /// </summary>
    public async Task<int> ExecuteCommandAsync(string sql, object? parameters = null, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var client = GetSqlSugarClient();
            var result = await client.Ado.ExecuteCommandAsync(sql, parameters);
            
            // 缓存清除逻辑
            if (result > 0 && _queryCacheManager != null)
            {
                await _queryCacheManager.RemoveEntityCacheAsync(typeof(TEntity));
            }
            
            stopwatch.Stop();
            _logger.LogInformation("执行SQL命令成功: {EntityType}, SQL: {Sql}, 影响行数: {AffectedRows}, 耗时: {ElapsedMs}ms", typeof(TEntity).Name, sql, result, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "执行SQL命令失败: {EntityType}, SQL: {Sql}", typeof(TEntity).Name, sql);
            throw new DataAccessException($"执行SQL命令失败: {sql}", ex);
        }
    }

    /// <summary>
    /// 执行原生SQL语句
    /// </summary>
    /// <param name="sql">SQL语句</param>
    /// <param name="parameters">参数</param>
    async Task<int> ISqlRepository<TEntity, TKey>.ExecuteSqlAsync(string sql, params object[] parameters)
    {
        return await ExecuteCommandAsync(sql, parameters);
    }

    /// <summary>
    /// 执行原生SQL查询
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="sql">SQL语句</param>
    /// <param name="parameters">参数</param>
    /// <returns>查询结果</returns>
    async Task<IEnumerable<T>> ISqlRepository<TEntity, TKey>.QuerySqlAsync<T>(string sql, params object[] parameters)
    {
        return await ExecuteQueryAsync<T>(sql, parameters);
    }

    /// <summary>
    /// 开始事务
    /// </summary>
    async Task ISqlRepository<TEntity, TKey>.BeginTransactionAsync()
    {
        _db.Ado.BeginTran();
    }

    /// <summary>
    /// 提交事务
    /// </summary>
    async Task ISqlRepository<TEntity, TKey>.CommitTransactionAsync()
    {
        _db.Ado.CommitTran();
    }

    /// <summary>
    /// 回滚事务
    /// </summary>
    async Task ISqlRepository<TEntity, TKey>.RollbackTransactionAsync()
    {
        _db.Ado.RollbackTran();
    }

    /// <summary>
    /// 创建存储过程调用器
    /// </summary>
    public object CreateStoredProcedure(string procedureName)
    {
        // SqlSugar存储过程调用方式不同
        return _db.Ado.UseStoredProcedure().GetDataTable(procedureName);
    }
}