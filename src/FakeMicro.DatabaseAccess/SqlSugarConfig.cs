using FakeMicro.DatabaseAccess.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SqlSugar;
using System.Data;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System;
using System.Collections.Generic;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// SqlSugar配置类
/// 提供SqlSugar的配置和数据库连接管理
/// </summary>
public static class SqlSugarConfig
{
    /// <summary>
    /// 数据库类型枚举
    /// </summary>
    public enum DatabaseType
    {
        MySQL,
        SQLServer,
        PostgreSQL,
        MariaDB
    }
    
    // 数据库连接管理器匿名实现
    public class DatabaseConnectionManagerAnonymous : IDatabaseConnectionManager
    {
        private readonly SqlSugarClient _db;
        private readonly string _connectionString;
        private readonly FakeMicro.DatabaseAccess.DatabaseType _dbType;
        private readonly ILogger _logger;
        
        public DatabaseConnectionManagerAnonymous(SqlSugarClient db, string connectionString, SqlSugarConfig.DatabaseType dbType, ILogger logger)
        {
            _db = db;
            _connectionString = connectionString;
            _logger = logger;
            // 转换数据库类型，移除Oracle类型
            _dbType = dbType switch
            {
                SqlSugarConfig.DatabaseType.MySQL => FakeMicro.DatabaseAccess.DatabaseType.MySQL,
                SqlSugarConfig.DatabaseType.SQLServer => FakeMicro.DatabaseAccess.DatabaseType.SQLServer,
                SqlSugarConfig.DatabaseType.PostgreSQL => FakeMicro.DatabaseAccess.DatabaseType.PostgreSQL,
                SqlSugarConfig.DatabaseType.MariaDB => FakeMicro.DatabaseAccess.DatabaseType.MariaDB,
                _ => FakeMicro.DatabaseAccess.DatabaseType.PostgreSQL
            };
        }
        
        public DbConnection CreateConnection()
        {
            return (DbConnection)_db.Ado.Connection;
        }
        
        public FakeMicro.DatabaseAccess.DatabaseType DatabaseType => _dbType;
        
        public async Task<DbConnection> GetConnectionAsync()
        {
            if (_db.Ado.Connection.State == ConnectionState.Closed)
            {
                await _db.Ado.OpenAsync();
            }
            return (DbConnection)_db.Ado.Connection;
        }
        
        public string BuildConnectionString(DatabaseConfig config)
        {
            return config?.ToString() ?? string.Empty;
        }
        
        public async Task<ConnectionTestResult> TestConnectionAsync()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                await _db.Ado.ExecuteCommandAsync("SELECT 1");
                var endTime = DateTime.UtcNow;
                var latency = (endTime - startTime).TotalMilliseconds;
                
                return new ConnectionTestResult
                {
                    Success = true,
                    Message = "数据库连接测试成功",
                    Latency = latency,
                    PoolingInfo = "连接池已启用"
                };
            }
            catch (Exception ex)
            {
                return new ConnectionTestResult
                {
                    Success = false,
                    Error = ex.Message,
                    Message = "数据库连接测试失败"
                };
            }
        }
        
        public string GetConnectionString()
        {
            return _connectionString;
        }
        
        public ConnectionPoolStatus GetConnectionPoolStatus()
        {
            return new ConnectionPoolStatus
            {
                IsPoolingEnabled = true,
                PoolSize = 100,
                MinPoolSize = 5,
                MaxPoolSize = 100,
                ActiveConnections = _db.Ado.Connection?.State == ConnectionState.Open ? 1 : 0,
                DatabaseType = _dbType,
                ConnectionStringConfigured = !string.IsNullOrEmpty(_connectionString),
                PoolName = "SqlSugar Connection Pool"
            };
        }
    }

    /// <summary>
    /// SqlSugar配置选项
    /// </summary>
    public class SqlSugarOptions
    {
        /// <summary>
        /// 数据库类型
        /// </summary>
        public DatabaseType DbType { get; set; } = DatabaseType.PostgreSQL;

        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// 是否启用SQL日志
        /// </summary>
        public bool EnableSqlLog { get; set; } = false;

        /// <summary>
        /// 是否启用慢查询日志（单位：毫秒）
        /// </summary>
        public int SlowQueryThreshold { get; set; } = 2000;

        /// <summary>
        /// 是否启用AOP
        /// </summary>
        public bool EnableAop { get; set; } = true;

        /// <summary>
        /// 连接池大小
        /// </summary>
        public int ConnectionPoolSize { get; set; } = 50;

        /// <summary>
        /// 连接超时时间（秒）
        /// </summary>
        public int ConnectionTimeout { get; set; } = 30;

        /// <summary>
        /// 是否启用读写分离
        /// </summary>
        public bool EnableReadWriteSeparation { get; set; } = false;

        /// <summary>
        /// 从库连接字符串列表
        /// </summary>
        public List<string> SlaveConnectionStrings { get; set; } = new();
    }

    // 使用BaseDatabaseConnectionManager中的ConnectionTestResult类

    /// <summary>
    /// SqlSugar数据库连接管理器实现
    /// </summary>
    public class SqlSugarConnectionManager : IDatabaseConnectionManager
    {
        private readonly SqlSugarClient _db;
        private readonly ILogger<SqlSugarConnectionManager> _logger;
        private readonly string _connectionString;
        private readonly FakeMicro.DatabaseAccess.DatabaseType _dbType;

        public SqlSugarConnectionManager(SqlSugarOptions options, ILogger<SqlSugarConnectionManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionString = options.ConnectionString ?? throw new ArgumentNullException(nameof(options.ConnectionString));
            // 转换为接口要求的DatabaseType
            _dbType = options.DbType switch
            {
                SqlSugarConfig.DatabaseType.MySQL => FakeMicro.DatabaseAccess.DatabaseType.MySQL,
                SqlSugarConfig.DatabaseType.SQLServer => FakeMicro.DatabaseAccess.DatabaseType.SQLServer,
                SqlSugarConfig.DatabaseType.PostgreSQL => FakeMicro.DatabaseAccess.DatabaseType.PostgreSQL,
                SqlSugarConfig.DatabaseType.MariaDB => FakeMicro.DatabaseAccess.DatabaseType.MariaDB,
                _ => FakeMicro.DatabaseAccess.DatabaseType.PostgreSQL
            };
            
            _db = CreateSqlSugarClient(options);
        }

        // 适配IDatabaseConnectionManager接口的方法
    public DbConnection CreateConnection()
    {
        return (DbConnection)_db.Ado.Connection;
    }

        public FakeMicro.DatabaseAccess.DatabaseType DatabaseType => _dbType;

        public async Task<DbConnection> GetConnectionAsync()
        {
            // 确保连接已打开
            if (_db.Ado.Connection.State == ConnectionState.Closed)
            {
                await _db.Ado.OpenAsync();
            }
            return (DbConnection)_db.Ado.Connection;
        }



        public string BuildConnectionString(DatabaseConfig config)
        {
            if (config == null)
                return string.Empty;
            
            // 使用连接字符串属性或返回空字符串
            return config.ToString() ?? string.Empty;
        }

        public async Task<ConnectionTestResult> TestConnectionAsync()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                await _db.Ado.ExecuteCommandAsync("SELECT 1");
                var endTime = DateTime.UtcNow;
                var latency = (endTime - startTime).TotalMilliseconds;
                
                _logger.LogInformation("数据库连接测试成功");
                return new ConnectionTestResult
                {
                    Success = true,
                    Message = "数据库连接测试成功",
                    Latency = latency,
                    PoolingInfo = "连接池已启用"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库连接测试失败");
                return new ConnectionTestResult
                {
                    Success = false,
                    Error = ex.Message,
                    Message = "数据库连接测试失败"
                };
            }
        }

        public string GetConnectionString()
        {
            return _connectionString;
        }

        // 添加额外的SqlSugar特定功能
        public ISqlSugarClient GetSqlSugarClient()
        {
            return _db;
        }

        public bool InitializeDatabase(params Type[] entityTypes)
        {
            try
            {
                _logger.LogInformation("开始初始化数据库...");
                
                // 创建表（如果不存在）
                _db.DbMaintenance.CreateDatabase();
                _db.CodeFirst.InitTables(entityTypes);
                
                _logger.LogInformation("数据库初始化完成");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库初始化失败");
                return false;
            }
        }

        // 实现GetConnectionPoolStatus方法
        public ConnectionPoolStatus GetConnectionPoolStatus()
        {
            return new ConnectionPoolStatus
            {
                IsPoolingEnabled = true,
                PoolSize = 100,
                ActiveConnections = _db.Ado.Connection?.State == ConnectionState.Open ? 1 : 0,
                DatabaseType = _dbType
            };
        }
    }

    /// <summary>
    /// 创建SqlSugarClient实例
    /// </summary>
    private static SqlSugarClient CreateSqlSugarClient(SqlSugarOptions options)
    {
        ConnectionConfig config = new()
        {
            ConnectionString = options.ConnectionString,
            DbType = SqlSugarConfig.ConvertToSqlSugarDbType(options.DbType), // 使用转换方法避免命名空间冲突
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute,
            MoreSettings = new ConnMoreSettings()
            {
                PgSqlIsAutoToLower = true, // PostgreSQL表名自动转小写
                PgSqlIsAutoToLowerCodeFirst = true, // CodeFirst时也自动转小写
                IsAutoToUpper = false, // 关闭自动转大写
                //WithNoLockQuery = true // 启用WITH NOLOCK查询优化
            }
        };

        SqlSugarClient client = new(config);

        // 配置AOP
        if (options.EnableAop)
        {
            client.Aop.OnLogExecuting = (sql, pars) =>
            {
                var stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();
                
                client.Aop.OnLogExecuted = (sql, pars) =>
                {
                    stopwatch.Stop();
                    if (options.EnableSqlLog || stopwatch.ElapsedMilliseconds > options.SlowQueryThreshold)
                    {
                        var parameters = string.Join(", ", pars.Select(p => $"{p.ParameterName}={p.Value}"));
                        Console.WriteLine($"SQL: {sql}\n参数: {parameters}\n耗时: {stopwatch.ElapsedMilliseconds}ms");
                    }
                };
                
                // 处理PostgreSQL的枚举参数转换
                foreach (var p in pars)
                {
                    if (p.Value?.GetType().IsEnum == true)
                    {
                        p.DbType = System.Data.DbType.String;
                        p.Value = p.Value.ToString();
                    }
                }
            };
        }

        return client;
    }

    /// <summary>
    /// 转换SqlSugarConfig的数据库类型到FakeMicro.DatabaseAccess的数据库类型
    /// </summary>
    public static FakeMicro.DatabaseAccess.DatabaseType ConvertDatabaseType(SqlSugarConfig.DatabaseType dbType)
    {
        return dbType switch
        {
            SqlSugarConfig.DatabaseType.MySQL => FakeMicro.DatabaseAccess.DatabaseType.MySQL,
            SqlSugarConfig.DatabaseType.SQLServer => FakeMicro.DatabaseAccess.DatabaseType.SQLServer,
            SqlSugarConfig.DatabaseType.PostgreSQL => FakeMicro.DatabaseAccess.DatabaseType.PostgreSQL,
            SqlSugarConfig.DatabaseType.MariaDB => FakeMicro.DatabaseAccess.DatabaseType.MariaDB,
            _ => FakeMicro.DatabaseAccess.DatabaseType.PostgreSQL
        };
    }

    /// <summary>
    /// 转换数据库类型枚举
    /// </summary>
    private static SqlSugar.DbType GetSqlSugarDbType(FakeMicro.DatabaseAccess.DatabaseType dbType)
    {
        return dbType switch
        {
            FakeMicro.DatabaseAccess.DatabaseType.MySQL => SqlSugar.DbType.MySql,
            FakeMicro.DatabaseAccess.DatabaseType.SQLServer => SqlSugar.DbType.SqlServer,
            FakeMicro.DatabaseAccess.DatabaseType.PostgreSQL => SqlSugar.DbType.PostgreSQL,
            FakeMicro.DatabaseAccess.DatabaseType.MariaDB => SqlSugar.DbType.MySql,
            _ => SqlSugar.DbType.PostgreSQL // 默认PostgreSQL
        };
    }

    /// <summary>
    /// 转换SqlSugarConfig的数据库类型枚举到SqlSugar的DbType
    /// </summary>
    public static SqlSugar.DbType ConvertToSqlSugarDbType(SqlSugarConfig.DatabaseType dbType)
    {
        var convertedType = ConvertDatabaseType(dbType);
        return GetSqlSugarDbType(convertedType);
    }
}


/// <summary>
/// SqlSugar服务扩展
/// </summary>
public static class SqlSugarServiceExtensions
{
    /// <summary>
    /// 添加SqlSugar服务
    /// </summary>
    public static IServiceCollection AddSqlSugar(this IServiceCollection services, IConfiguration configuration, string configSection = "SqlSugar")
    {
        // 配置SqlSugarOptions
        services.Configure<SqlSugarConfig.SqlSugarOptions>(configuration.GetSection(configSection));
        
        // 添加数据库连接管理器
        services.AddSingleton<IDatabaseConnectionManager>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<SqlSugarConfig.SqlSugarOptions>>().Value;
            var logger = serviceProvider.GetRequiredService<ILogger<IDatabaseConnectionManager>>();
            
            // 如果连接字符串未配置，尝试从ConnectionStrings获取
            if (string.IsNullOrEmpty(options.ConnectionString))
            {
                options.ConnectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
            }
            
            // 创建SqlSugarClient
            var db = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = options.ConnectionString,
                DbType = SqlSugarConfig.ConvertToSqlSugarDbType(options.DbType), // 使用转换方法获取正确的数据库类型
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute,
                MoreSettings = new ConnMoreSettings()
                {
                    PgSqlIsAutoToLower = true, // PostgreSQL表名自动转小写
                    PgSqlIsAutoToLowerCodeFirst = true, // CodeFirst时也自动转小写
                    IsAutoToUpper = false, // 关闭自动转大写
                }
            });
            
            // 创建并返回连接管理器实现
            return new SqlSugarConfig.DatabaseConnectionManagerAnonymous(db, options.ConnectionString, options.DbType, logger);
        });
        
        // 添加SqlSugarClient
        services.AddSingleton<ISqlSugarClient>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<SqlSugarConfig.SqlSugarOptions>>().Value;
            
            // 直接创建SqlSugarClient实例
            var db = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = options.ConnectionString,
                DbType = SqlSugarConfig.ConvertToSqlSugarDbType(options.DbType), // 使用转换方法获取正确的数据库类型
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute,
                MoreSettings = new ConnMoreSettings()
                {
                    PgSqlIsAutoToLower = true, // PostgreSQL表名自动转小写
                    PgSqlIsAutoToLowerCodeFirst = true, // CodeFirst时也自动转小写
                    IsAutoToUpper = false, // 关闭自动转大写
                }
            });
            
            // 配置AOP和监控
            db.Aop.OnLogExecuting = (sql, parameters) =>
            {
                var logger = serviceProvider.GetService<ILogger<ISqlSugarClient>>();
                if (logger != null && !sql.Contains("WHERE 1=0"))
                {
                    logger.LogInformation($"SQL执行: {sql}");
                }
                
                // 处理PostgreSQL的枚举参数转换
                foreach (var p in parameters)
                {
                    if (p.Value?.GetType().IsEnum == true)
                    {
                        p.DbType = System.Data.DbType.String;
                        p.Value = p.Value.ToString();
                    }
                }
            };
            
            return db;
        });
        
        // 添加事务服务
        services.AddScoped<ITransactionService, SqlSugarTransactionService>();
        
        return services;
    }
    
    /// <summary>
    /// 初始化数据库
    /// </summary>
    public static bool InitializeDatabase(this IServiceProvider serviceProvider, params Type[] entityTypes)
    {
        try
        {
            // 获取SqlSugarClient实例
            var db = serviceProvider.GetRequiredService<ISqlSugarClient>();
            var logger = serviceProvider.GetRequiredService<ILogger<ISqlSugarClient>>();
            
            logger.LogInformation("开始初始化数据库...");
            
            // 创建数据库（如果不存在）
            db.DbMaintenance.CreateDatabase();
            
            // 确保实体类型数组不为null
            if (entityTypes != null && entityTypes.Length > 0)
            {
                db.CodeFirst.InitTables(entityTypes);
            }
            
            logger.LogInformation("数据库初始化完成");
            return true;
        }
        catch (Exception ex)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<ISqlSugarClient>>();
            logger.LogError(ex, "数据库初始化失败");
            return false;
        }
    }
    
    /// <summary>
    /// 事务服务实现
    /// </summary>
    private class SqlSugarTransactionService : ITransactionService
    {
        private readonly ISqlSugarClient _db;
        
        public SqlSugarTransactionService(ISqlSugarClient db)
        {
            _db = db;
        }
        
        public async Task ExecuteInTransactionAsync(Func<Task> action)
        {
            // 开始事务
            _db.Ado.BeginTran();
            try
            {
                await action();
                _db.Ado.CommitTran();
            }
            catch (Exception)
            {
                _db.Ado.RollbackTran();
                throw;
            }
        }
    }
}

