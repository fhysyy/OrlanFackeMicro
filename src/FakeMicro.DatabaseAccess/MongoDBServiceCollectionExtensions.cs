using System; 
using System.Reflection;
using FakeMicro.DatabaseAccess.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SqlSugar;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// MongoDB服务扩展类
/// 遵循依赖注入最佳实践
/// </summary>
public static class MongoDBServiceCollectionExtensions
{
    /// <summary>
    /// 添加MongoDB数据库服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <param name="sectionName">配置节点名称</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMongoDB(this IServiceCollection services, 
        IConfiguration configuration, string sectionName = "MongoDB")
    {
        // 绑定配置选项（使用更现代的Get方法）
        var options = configuration.GetSection(sectionName).Get<MongoDBConfig.MongoDBOptions>() ?? throw new InvalidOperationException($"MongoDB配置未找到: {sectionName}");
        services.AddSingleton(options);

        // 配置SqlSugar.MongoDbCore
        services.AddSingleton<ISqlSugarClient>(provider =>
        {
            var logger = provider.GetService<ILogger<ISqlSugarClient>>();
            try
            {
                logger?.LogInformation("正在创建SqlSugar MongoDB客户端");
                
                // 注册DLL防止找不到DLL
                InstanceFactory.CustomAssemblies = new Assembly[] { typeof(SqlSugar.MongoDb.MongoDbProvider).Assembly };
                
                // 创建SqlSugar客户端，配置为MongoDB
                var db = new SqlSugarClient(new ConnectionConfig()
                {
                    IsAutoCloseConnection = true,
                    DbType = DbType.MongoDb,
                    ConnectionString = options.ConnectionString
                }, it =>
                {
                    it.Aop.OnLogExecuting = (sql, pars) =>
                    {
                        logger?.LogDebug("MongoDB SQL执行: {Sql}", sql);
                    };
                });
                
                logger?.LogInformation("SqlSugar MongoDB客户端创建成功");
                return db;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "创建SqlSugar MongoDB客户端失败");
                throw;
            }
        });

        // 注册MongoDB仓储工厂
        services.AddScoped<IMongoRepositoryFactory, MongoRepositoryFactory>();

        // 注册通用MongoDB仓储
        services.AddScoped(typeof(IMongoRepository<,>), typeof(MongoRepository<,>));
        services.AddScoped(typeof(IRepository<,>), typeof(MongoRepository<,>));

        return services;
    }

    /// <summary>
    /// 添加MongoDB配置验证
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMongoDBConfigValidation(this IServiceCollection services)
    {
        // 可以在这里添加配置验证逻辑
        return services;
    }
}
