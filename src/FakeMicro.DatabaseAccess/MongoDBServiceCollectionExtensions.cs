using System;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.DatabaseAccess.Interfaces.Mongo;
using FakeMicro.DatabaseAccess.Repositories.Mongo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

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
        // 绑定配置选项 - 使用强类型配置替代字符串键访问
        services.Configure<MongoDBConfig.MongoDBOptions>(configuration.GetSection(sectionName));

        // 直接注册MongoClient
        services.AddSingleton<MongoClient>(provider =>
        {
            var logger = provider.GetService<ILogger<MongoClient>>();
            var options = provider.GetRequiredService<IOptions<MongoDBConfig.MongoDBOptions>>().Value;
            
            try
            {
                logger?.LogInformation("正在创建MongoDB客户端");
                
                // 创建MongoDB客户端
                var client = new MongoClient(options.ConnectionString);
                
                // 验证连接
                client.ListDatabaseNames().FirstOrDefault();
                
                logger?.LogInformation("MongoDB客户端创建成功并已验证连接");
                return client;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "创建MongoDB客户端失败");
                throw;
            }
        });

        // 注册MongoDB仓储工厂
        services.AddScoped<IMongoRepositoryFactory, MongoRepositoryFactory>();

        // 注册通用MongoDB仓储
        services.AddScoped(typeof(IMongoRepository<,>), typeof(MongoRepository<,>));
        
        // 注册MongoActRepository
        services.AddScoped(typeof(IMongoActRepository<,>), typeof(MongoActRepository<,>));
        services.AddScoped<IMongoActRepository, MongoActRepository>();

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
