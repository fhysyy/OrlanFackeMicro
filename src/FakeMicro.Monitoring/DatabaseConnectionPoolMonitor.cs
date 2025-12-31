using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SqlSugar;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using FakeMicro.DatabaseAccess;

namespace FakeMicro.Monitoring;

/// <summary>
/// 数据库连接池监控服务
/// 定期收集PostgreSQL和MongoDB的连接池指标
/// </summary>
public class DatabaseConnectionPoolMonitor : IHostedService, IDisposable
{
    private readonly ILogger<DatabaseConnectionPoolMonitor> _logger;
    private readonly PerformanceMonitor _performanceMonitor;
    private readonly MongoClient _mongoClient;
    private readonly System.Timers.Timer _timer;
    private readonly int _monitoringIntervalMs = 30000; // 默认监控间隔30秒
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    // 监控专用的SqlSugarClient
    private readonly ISqlSugarClient _monitoringSqlSugarClient;

    public DatabaseConnectionPoolMonitor(
        ILogger<DatabaseConnectionPoolMonitor> logger,
        PerformanceMonitor performanceMonitor,
        MongoClient mongoClient,
        IOptions<SqlSugarConfig.SqlSugarOptions> sqlSugarOptions,
        IOptions<ConnectionStringsOptions> connectionStringsOptions)
    {
        _logger = logger;
        _performanceMonitor = performanceMonitor;
        _mongoClient = mongoClient;
        _timer = new System.Timers.Timer(_monitoringIntervalMs);
        _timer.Elapsed += async (sender, e) => await CollectConnectionPoolMetricsAsync();
        
        // 创建监控专用的SqlSugarClient实例
        var options = sqlSugarOptions.Value;
        if (string.IsNullOrEmpty(options.ConnectionString) && connectionStringsOptions.Value?.DefaultConnection != null)
        {
            options.ConnectionString = connectionStringsOptions.Value.DefaultConnection;
        }
        
        _monitoringSqlSugarClient = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = options.ConnectionString,
            DbType = SqlSugarConfig.ConvertToSqlSugarDbType(options.DbType),
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute,
            MoreSettings = new ConnMoreSettings
            {
                PgSqlIsAutoToLower = true, // PostgreSQL表名自动转小写
                PgSqlIsAutoToLowerCodeFirst = true, // CodeFirst时也自动转小写
                IsAutoToUpper = false, // 关闭自动转大写
            }
        });
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("数据库连接池监控服务已启动");
        _timer.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("数据库连接池监控服务已停止");
        _timer.Stop();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 收集数据库连接池指标
    /// </summary>
    private async Task CollectConnectionPoolMetricsAsync()
    {
        if (!_semaphore.Wait(0))
        {
            _logger.LogWarning("上一次监控任务还在执行，跳过本次监控");
            return;
        }

        try
        {
            await CollectPostgreSqlConnectionPoolMetricsAsync();
            CollectMongoDbConnectionPoolMetrics();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "收集数据库连接池指标时出错");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// 收集PostgreSQL连接池指标
    /// 通过查询pg_stat_activity视图获取连接信息
    /// </summary>
    private async Task CollectPostgreSqlConnectionPoolMetricsAsync()
    {
        try
        {
            // 查询当前连接数
            var connectionCountResult = await _monitoringSqlSugarClient.Ado.SqlQueryAsync<dynamic>(@"
                SELECT 
                    COUNT(*) AS total_connections,
                    COUNT(CASE WHEN state = 'active' THEN 1 END) AS active_connections,
                    COUNT(CASE WHEN state = 'idle' THEN 1 END) AS idle_connections
                FROM pg_stat_activity;");

            if (connectionCountResult.Any())
            {
                var result = connectionCountResult.First();
                long totalConnections = result.total_connections;
                long activeConnections = result.active_connections;
                long idleConnections = result.idle_connections;

                // 获取连接池最大连接数
                long maxConnections = await GetPostgreSqlMaxConnectionsAsync();

                // 更新性能监控指标
                _performanceMonitor.UpdatePostgreSqlConnectionPoolMetrics(activeConnections, idleConnections, maxConnections);

                _logger.LogDebug("PostgreSQL连接池指标: 总连接数={Total}, 活跃连接={Active}, 空闲连接={Idle}, 最大连接={Max}",
                    totalConnections, activeConnections, idleConnections, maxConnections);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "收集PostgreSQL连接池指标时出错");
        }
    }

    /// <summary>
    /// 获取PostgreSQL连接池最大连接数
    /// </summary>
    private async Task<long> GetPostgreSqlMaxConnectionsAsync()
    {
        try
        {
            var result = await _monitoringSqlSugarClient.Ado.SqlQueryAsync<dynamic>(
                "SHOW max_connections;");
            return long.Parse(result.First().max_connections.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取PostgreSQL最大连接数时出错");
            return 100; // 默认值
        }
    }

    /// <summary>
    /// 收集MongoDB连接池指标
    /// </summary>
    private void CollectMongoDbConnectionPoolMetrics()
    {
        try
        {
            // 获取MongoDB连接池配置
            var maxConnections = _mongoClient.Settings.MaxConnectionPoolSize;
            
            // 尝试通过MongoDB驱动的监控API获取连接池统计信息
            // 注意：MongoDB .NET Driver 2.14+ 提供了更完善的监控API
            
            // 使用反射获取内部连接池信息
            long activeConnections = 0;
            long idleConnections = 0;
            
            try
            {
                // 通过反射访问MongoDB驱动的内部连接池信息
                var clientType = _mongoClient.GetType();
                var serverProperty = clientType.GetProperty("Topology", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (serverProperty != null)
                {
                    var topology = serverProperty.GetValue(_mongoClient);
                    if (topology != null)
                    {
                        var serversProperty = topology.GetType().GetProperty("Servers");
                        if (serversProperty != null)
                        {
                            var servers = serversProperty.GetValue(topology) as System.Collections.IEnumerable;
                            if (servers != null)
                            {
                                foreach (var server in servers)
                                {
                                    var connectionPoolProperty = server.GetType().GetProperty("ConnectionPool");
                                    if (connectionPoolProperty != null)
                                    {
                                        var connectionPool = connectionPoolProperty.GetValue(server);
                                        if (connectionPool != null)
                                        {
                                            // 获取活跃连接数
                                            var inUseProperty = connectionPool.GetType().GetProperty("InUseConnections");
                                            if (inUseProperty != null)
                                            {
                                                activeConnections += Convert.ToInt64(inUseProperty.GetValue(connectionPool));
                                            }
                                            
                                            // 获取空闲连接数
                                            var availableProperty = connectionPool.GetType().GetProperty("AvailableConnections");
                                            if (availableProperty != null)
                                            {
                                                idleConnections += Convert.ToInt64(availableProperty.GetValue(connectionPool));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception reflectionEx)
            {
                // 如果反射失败，使用安全的默认值
                _logger.LogDebug(reflectionEx, "使用反射获取MongoDB连接池信息失败，使用默认值");
                
                // 执行一个简单操作来保持连接活跃
                _mongoClient.ListDatabaseNames().FirstOrDefault();
                
                // 使用合理的默认值
                activeConnections = 1;
                idleConnections = 0;
            }
            
            // 更新性能监控指标
            _performanceMonitor.UpdateMongoDbConnectionPoolMetrics(activeConnections, idleConnections, maxConnections);

            _logger.LogDebug("MongoDB连接池指标: 活跃连接={Active}, 空闲连接={Idle}, 最大连接={Max}",
                activeConnections, idleConnections, maxConnections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "收集MongoDB连接池指标时出错");
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}

/// <summary>
/// 数据库连接池监控扩展方法
/// </summary>
public static class DatabaseConnectionPoolMonitorExtensions
{
    /// <summary>
    /// 添加数据库连接池监控服务
    /// </summary>
    public static IServiceCollection AddDatabaseConnectionPoolMonitor(this IServiceCollection services)
    {
        services.AddHostedService<DatabaseConnectionPoolMonitor>();
        return services;
    }
}