using System; 
using System.Reflection;
using FakeMicro.DatabaseAccess.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        // 绑定配置选项（使用更现代的Get方法）
        var options = configuration.GetSection(sectionName).Get<MongoDBConfig.MongoDBOptions>() ?? throw new InvalidOperationException($"MongoDB配置未找到: {sectionName}");
        services.AddSingleton(options);

        // 注册MongoDB客户端
        services.AddSingleton<IMongoClient>(provider =>
        {
            var logger = provider.GetService<ILogger<IMongoClient>>();
            try
            {
                logger?.LogInformation("正在创建MongoDB客户端连接");
                var client = new MongoClient(options.ConnectionString);
                logger?.LogInformation("MongoDB客户端连接创建成功");
                return client;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "创建MongoDB客户端失败");
                throw;
            }
        });

        // 注册MongoDB数据库
        services.AddSingleton<IMongoDatabase>(provider =>
        {
            var client = provider.GetRequiredService<IMongoClient>();
            var logger = provider.GetService<ILogger<IMongoDatabase>>();
            try
            {
                logger?.LogInformation("正在获取MongoDB数据库: {DatabaseName}", options.DatabaseName);
                var database = client.GetDatabase(options.DatabaseName);
                logger?.LogInformation("成功获取MongoDB数据库: {DatabaseName}", options.DatabaseName);
                return database;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "获取MongoDB数据库失败: {DatabaseName}", options.DatabaseName);
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
