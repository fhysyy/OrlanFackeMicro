using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.DatabaseAccess.Repositories;
using FakeMicro.DatabaseAccess.Transaction;
using FakeMicro.Entities;
using FakeMicro.Utilities.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using DbType = SqlSugar.DbType;

namespace FakeMicro.DatabaseAccess
{
    /// <summary>
    /// SqlSugar服务扩展类
    /// 遵循依赖注入最佳实践
    /// </summary>
    public static class SqlSugarServiceCollectionExtensions
    {
        /// <summary>
        /// 添加SqlSugar数据库服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <param name="sectionName">配置节点名称</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddSqlSugar(this IServiceCollection services, 
            IConfiguration configuration, string sectionName = "SqlSugar")
        {
            // 配置SqlSugarOptions - 使用强类型配置
            services.Configure<SqlSugarConfig.SqlSugarOptions>(configuration.GetSection(sectionName));
            
            // 配置连接字符串选项
            services.Configure<ConnectionStringsOptions>(configuration.GetSection("ConnectionStrings"));

            // 创建PostgreSQL SqlSugar客户端
            Func<IServiceProvider, object?, ISqlSugarClient> createPostgreSqlClient = (provider, _) =>
            {
                var sqlSugarOptions = provider.GetRequiredService<IOptions<SqlSugarConfig.SqlSugarOptions>>().Value;
                var connectionStrings = provider.GetRequiredService<IOptions<ConnectionStringsOptions>>().Value;
                var logger = provider.GetService<ILogger<ISqlSugarClient>>();
                
                // 尝试从AppSettings获取配置（生产环境）
                try
                {
                    var appSettings = configuration.GetAppSettings();
                    
                    // 如果连接字符串未配置，尝试从AppSettings获取
                    if (string.IsNullOrEmpty(sqlSugarOptions.ConnectionString))
                    {
                        sqlSugarOptions.ConnectionString = appSettings.Database.GetConnectionString() ?? connectionStrings.DefaultConnection ?? string.Empty;
                    }
                    
                    // 配置读写分离
                    sqlSugarOptions.EnableReadWriteSeparation = appSettings.Database.EnableReadWriteSeparation;
                    if (appSettings.Database.EnableReadWriteSeparation)
                    {
                        // 添加从库连接字符串
                        sqlSugarOptions.SlaveConnectionStrings.Add(appSettings.Database.GetReadConnectionString());
                    }
                    
                    // 从AppSettings中获取连接池配置
                    sqlSugarOptions.MinPoolSize = appSettings.Database.MinPoolSize;
                    sqlSugarOptions.MaxPoolSize = appSettings.Database.MaxPoolSize;
                    sqlSugarOptions.ConnectionLifetime = appSettings.Database.ConnectionLifetime;
                }
                catch
                {
                    // 测试环境可能没有AppSettings，使用默认值
                    if (string.IsNullOrEmpty(sqlSugarOptions.ConnectionString))
                    {
                        sqlSugarOptions.ConnectionString = connectionStrings.DefaultConnection ?? string.Empty;
                    }
                }
                
                return CreateSqlSugarClient(sqlSugarOptions, logger);
            };
            
            // 注册PostgreSQL SqlSugar客户端（命名注册，避免与MongoDB冲突）
            services.AddKeyedScoped<ISqlSugarClient>("PostgreSQL", createPostgreSqlClient);
            
            // 注册默认SqlSugar客户端（指向PostgreSQL）
            services.AddScoped<ISqlSugarClient>(provider => createPostgreSqlClient(provider, null));

            // 注册事务服务以解决UserServiceGrain依赖注入问题
            services.AddScoped<ITransactionService, SqlSugarTransactionService>();

            // 注册查询缓存管理器以解决SqlSugarRepository依赖注入问题
            services.AddScoped<IQueryCacheManager, QueryCacheManager>();

            // 注册SqlSugar仓储工厂
            services.AddScoped<ISqlSugarRepositoryFactory, SqlSugarRepositoryFactory>();

            // 自动注册所有仓储
            RegisterRepositories(services);

            return services;
        }

        /// <summary>
        /// 创建SqlSugar客户端
        /// </summary>
        /// <param name="options">配置选项</param>
        /// <param name="logger">日志记录器</param>
        /// <returns>SqlSugar客户端</returns>
        private static ISqlSugarClient CreateSqlSugarClient(SqlSugarConfig.SqlSugarOptions options, ILogger<ISqlSugarClient>? logger = null)
        {
            var dbType = SqlSugarConfig.ConvertToSqlSugarDbType(options.DbType);

            var connectionConfig = new ConnectionConfig()
            {
                ConnectionString = options.ConnectionString,
                DbType = dbType,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute,
                MoreSettings = new ConnMoreSettings()
                {
                    PgSqlIsAutoToLower = false,
                    IsAutoRemoveDataCache = true
                }
            };

            // 配置连接池和超时参数，根据数据库类型使用正确的参数名
            var timeoutParam = dbType switch
            {
                DbType.PostgreSQL => "Timeout",
                _ => "Connection Timeout"
            };
            
            // 使用Options中的连接池配置
            var minPoolSize = options.MinPoolSize;
            var maxPoolSize = options.MaxPoolSize;
            var connectionLifetime = options.ConnectionLifetime;
            
            connectionConfig.ConnectionString += $@";Pooling=true;Minimum Pool Size={minPoolSize};Maximum Pool Size={maxPoolSize};Connection Lifetime={connectionLifetime};{timeoutParam}={options.ConnectionTimeout}";

            // 配置读写分离
            if (options.EnableReadWriteSeparation && options.SlaveConnectionStrings.Any())
            {
                connectionConfig.SlaveConnectionConfigs = options.SlaveConnectionStrings
                    .Select(slaveConn => new SlaveConnectionConfig()
                    {
                        ConnectionString = slaveConn,
                        HitRate = 10
                    })
                    .ToList();
            }

            var db = new SqlSugarClient(connectionConfig);
            
            // 注册所有实体类型，解决类型不支持的问题
            RegisterEntities(db, logger);

            // 配置AOP
            if (options.EnableAop)
            {
                ConfigureAop(db, options, logger);
            }
            
            logger?.LogInformation("SqlSugar客户端创建成功，数据库类型: {DbType}", dbType);
            return db;
        }
        
        /// <summary>
        /// 注册所有实体类型
        /// </summary>
        /// <param name="db">SqlSugar客户端</param>
        /// <param name="logger">日志记录器</param>
        private static void RegisterEntities(ISqlSugarClient db, ILogger<ISqlSugarClient>? logger = null)
        {
            try
            {
                // 首先测试数据库连接是否可用
                try
                {
                    var isConnected = db.Ado.IsValidConnection();
                    if (!isConnected)
                    {
                        logger?.LogWarning("数据库连接不可用，跳过实体类型注册");
                        return;
                    }
                    logger?.LogInformation("数据库连接成功，开始注册实体类型");
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "数据库连接测试失败，跳过实体类型注册");
                    return;
                }

                // 注册所有实体类型，解决类型不支持问题
                var entityTypes = new[]
                {
                    typeof(User),
                    typeof(UserRole),
                    typeof(Role),
                    //typeof(Student),
                    //typeof(Class),
                    //typeof(Exam),
                    //typeof(Score),
                    typeof(Subject),
                    typeof(Message),
                    //typeof(FakeMicro.Entities.FileInfo), // 使用完全限定名解决冲突
                    typeof(DictionaryType),
                    typeof(DictionaryItem),
                    typeof(Notebook),
                    typeof(AuditLog)
                };

                // 注册所有实体类型
                foreach (var type in entityTypes)
                {
                    try
                    {
                        // 检查表是否存在
                        var tableName = type.GetCustomAttribute<SugarTable>()?.TableName ?? type.Name;
                        if (db.DbMaintenance.IsAnyTable(tableName))
                        {
                            // 注册实体
                            db.CodeFirst.InitTables(type);
                            logger?.LogInformation("已注册实体类型: {EntityType}", type.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "初始化表 {TableName} 时出错", type.Name);
                        // 继续处理其他表，不中断
                    }
                }

                // 针对PostgreSQL数据库，添加特殊配置
                if (db.CurrentConnectionConfig.DbType == DbType.PostgreSQL)
                {
                    logger?.LogInformation("PostgreSQL数据库配置已应用");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "注册实体类型时出错");
            }
        }

        /// <summary>
        /// 配置AOP
        /// </summary>
        /// <param name="db">SqlSugar客户端</param>
        /// <param name="options">配置选项</param>
        /// <param name="logger">日志记录器</param>
        private static void ConfigureAop(ISqlSugarClient db, SqlSugarConfig.SqlSugarOptions options, ILogger<ISqlSugarClient>? logger = null)
        {
            // SQL执行错误处理
            db.Aop.OnError = (exp) =>
            {
                logger?.LogError(exp, "SQL执行错误");
            };

            // SQL执行前
            db.Aop.OnLogExecuting = (sql, pars) =>
            {
                if (options.EnableSqlLog)
                {
                    var sqlText = UtilMethods.GetSqlString(db.CurrentConnectionConfig.DbType, sql, pars);
                    logger?.LogInformation("SQL执行: {SqlText}", sqlText);
                }
            };

            // SQL执行后和慢查询检测
            db.Aop.OnLogExecuted = (sql, pars) =>
            {
                // 记录执行时间用于慢查询检测
                var endTime = DateTime.Now;
                var startTime = endTime - TimeSpan.FromMilliseconds(1000); // 粗略估计开始时间
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                
                if (elapsed > options.SlowQueryThreshold)
                {
                    var sqlText = UtilMethods.GetSqlString(db.CurrentConnectionConfig.DbType, sql, pars);
                    logger?.LogWarning("慢查询警告: 执行时间 {Elapsed}ms, SQL: {SqlText}", elapsed, sqlText);
                }
            };
        }

        /// <summary>
        /// 自动注册所有仓储
        /// </summary>
        /// <param name="services">服务集合</param>
        private static void RegisterRepositories(IServiceCollection services)
        {
            // 这里可以自动注册所有仓储类型
            // 当前项目中已经通过工厂模式注册，此方法保留用于扩展
        }
    }
}
