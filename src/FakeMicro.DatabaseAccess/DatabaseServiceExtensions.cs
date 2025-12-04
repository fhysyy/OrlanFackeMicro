using FakeMicro.DatabaseAccess.Extensions;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.DatabaseAccess.Interfaces.Mongo;
using FakeMicro.DatabaseAccess.Repositories;
using FakeMicro.DatabaseAccess.Repositories.Mongo;
using FakeMicro.DatabaseAccess.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using static Microsoft.AspNetCore.Hosting.Internal.HostingApplication;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// 数据库服务扩展类（SqlSugar实现）
/// 提供在ASP.NET Core和Orleans中注册数据库服务的方法
/// 优化版本：统一使用SqlSugar作为唯一ORM框架
/// </summary>
public static class DatabaseServiceExtensions
{
    /// <summary>
    /// 将数据库服务添加到服务集合中
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置信息</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // 注册SqlSugar服务（主ORM框架）
        services.AddSqlSugar(configuration,sectionName:"SqlSugar");

        // 注册MongoDB服务
        services.AddMongoDB(configuration);

        // 注册基础仓储服务
        services.AddScoped(typeof(IRepository<,>), typeof(SqlSugarRepository<,>));
        services.AddScoped(typeof(IMongoRepository<,>), typeof(MongoRepository<,>));
        
        // 注册具体仓储服务
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ISubjectRepository, SubjectRepository>();
        services.AddScoped<IDictionaryTypeRepository, DictionaryTypeRepository>();
        services.AddScoped<IDictionaryItemRepository, DictionaryItemRepository>();
        services.AddScoped<ISysOpenRepository, SysOpenRepository>();
        services.AddScoped<IManagerVersionRepository, ManagerVersionRepository>();
        services.AddScoped<IMongoActRepository, MongoActRepository>();
        // 移除Dapper相关配置和依赖，统一使用SqlSugar
        // Dapper相关服务已从依赖注入中移除

        // 注册动态仓储工厂
        services.AddSingleton<IRepositoryFactory, DynamicRepositoryFactory>();
        
        // 注册仓储创建策略
        services.AddSingleton(typeof(IRepositoryCreationStrategy<,>), typeof(PostgreSqlRepositoryCreationStrategy<,>));
        services.AddSingleton(typeof(IRepositoryCreationStrategy<,>), typeof(MongoDbRepositoryCreationStrategy<,>));

        // 暂时注释掉数据库初始化服务，专注于测试Orleans持久化状态配置
        services.AddHostedService<DatabaseInitializerHostedService>();
        // services.AddDatabaseInitializer(configuration.GetConnectionString);
        return services;
    }
}