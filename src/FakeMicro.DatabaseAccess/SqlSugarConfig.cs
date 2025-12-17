using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SqlSugar;
using System.Data;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System;
using System.Collections.Generic;
using FakeMicro.DatabaseAccess.Interfaces;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// SqlSugar配置类
/// 提供SqlSugar的配置和数据库连接管理
/// </summary>
public static class SqlSugarConfig
{
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
        public required string ConnectionString { get; set; }

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

    /// <summary>
    /// 创建SqlSugarClient实例
    /// </summary>
    private static SqlSugarClient CreateSqlSugarClient(SqlSugarOptions options)
    {
        ConnectionConfig config = new()
        {
            ConnectionString = options.ConnectionString,
            DbType = GetSqlSugarDbType(options.DbType),
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute,
            MoreSettings = new ConnMoreSettings()
            {
                PgSqlIsAutoToLower = true, // PostgreSQL表名自动转小写
                PgSqlIsAutoToLowerCodeFirst = true, // CodeFirst时也自动转小写
                IsAutoToUpper = false, // 关闭自动转大写
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
    /// 转换数据库类型枚举
    /// </summary>
    private static SqlSugar.DbType GetSqlSugarDbType(DatabaseType dbType)
    {
        return dbType switch
        {
            DatabaseType.MySQL => SqlSugar.DbType.MySql,
            DatabaseType.SQLServer => SqlSugar.DbType.SqlServer,
            DatabaseType.PostgreSQL => SqlSugar.DbType.PostgreSQL,
            DatabaseType.MariaDB => SqlSugar.DbType.MySql,
            _ => SqlSugar.DbType.PostgreSQL // 默认PostgreSQL
        };
    }

    /// <summary>
    /// 转换数据库类型枚举到SqlSugar的DbType
    /// </summary>
    public static SqlSugar.DbType ConvertToSqlSugarDbType(DatabaseType dbType)
    {
        return GetSqlSugarDbType(dbType);
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
        
        // 配置连接字符串选项
        services.Configure<ConnectionStringsOptions>(configuration.GetSection("ConnectionStrings"));
        
        // 添加SqlSugarClient
        services.AddSingleton<ISqlSugarClient>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<SqlSugarConfig.SqlSugarOptions>>().Value;
            var connectionStrings = serviceProvider.GetRequiredService<IOptions<ConnectionStringsOptions>>().Value;
            
            // 如果连接字符串未配置，尝试从强类型ConnectionStrings获取
            if (string.IsNullOrEmpty(options.ConnectionString))
            {
                options.ConnectionString = connectionStrings.DefaultConnection ?? string.Empty;
            }
            
            // 创建SqlSugarClient
            var db = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = options.ConnectionString,
                DbType = SqlSugarConfig.ConvertToSqlSugarDbType(options.DbType),
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute,
                MoreSettings = new ConnMoreSettings()
                {
                    PgSqlIsAutoToLower = true, // PostgreSQL表名自动转小写
                    PgSqlIsAutoToLowerCodeFirst = true, // CodeFirst时也自动转大写
                    IsAutoToUpper = false, // 关闭自动转大写
                }
            });
            
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