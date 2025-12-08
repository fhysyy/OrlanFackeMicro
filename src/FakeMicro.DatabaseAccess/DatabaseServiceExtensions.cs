using FakeMicro.DatabaseAccess.Extensions;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.DatabaseAccess.Interfaces.Mongo;
using FakeMicro.DatabaseAccess.Repositories;
using FakeMicro.DatabaseAccess.Repositories.Mongo;
using FakeMicro.DatabaseAccess.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// 数据库服务扩展类
/// 提供在ASP.NET Core和Orleans中注册数据库服务的方法
/// 支持MongoDB和PostgreSQL的仓储隔离
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

        return services.AddDatabaseServices(configuration, options =>
        {
            options.UsePostgreSQL = true;
            options.UseMongoDB = true;
            options.RegisterDefaultRepositories = true;
            options.RegisterDynamicRepositoryFactory = true;
            options.UseDatabaseInitializer = true;
        });
    }

    /// <summary>
    /// 将数据库服务添加到服务集合中，支持自定义配置
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置信息</param>
    /// <param name="configureOptions">配置选项</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration, Action<DatabaseServiceOptions> configureOptions)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));
        
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        // 配置选项
        var options = new DatabaseServiceOptions();
        configureOptions(options);

        // 注册PostgreSQL服务（使用SqlSugar）
        if (options.UsePostgreSQL)
        {
            services.AddSqlSugar(configuration, sectionName: "SqlSugar");
        }

        // 注册MongoDB服务
        if (options.UseMongoDB)
        {
            services.AddMongoDB(configuration);
        }

        // 注册基础仓储服务
        if (options.UsePostgreSQL)
        {
            services.AddScoped(typeof(IRepository<,>), typeof(SqlSugarRepository<,>));
            services.AddScoped(typeof(ISqlRepository<,>), typeof(SqlSugarRepository<,>));
        }

        if (options.UseMongoDB)
        {
            services.AddScoped(typeof(IMongoRepository<,>), typeof(MongoRepository<,>));
        }
        
        // 注册具体仓储服务
        if (options.RegisterDefaultRepositories)
        {
            RegisterDefaultRepositories(services, options);
        }

        // 注册动态仓储工厂
        if (options.RegisterDynamicRepositoryFactory)
        {
            services.AddSingleton<IRepositoryFactory, DynamicRepositoryFactory>();
            
            // 注册仓储创建策略
            if (options.UsePostgreSQL)
            {
                services.AddSingleton(typeof(IRepositoryCreationStrategy<,>), typeof(PostgreSqlRepositoryCreationStrategy<,>));
            }
            
            if (options.UseMongoDB)
            {
                services.AddSingleton(typeof(IRepositoryCreationStrategy<,>), typeof(MongoDbRepositoryCreationStrategy<,>));
            }
        }

        // 注册数据库初始化服务
        if (options.UseDatabaseInitializer)
        {
            services.AddHostedService<DatabaseInitializerHostedService>();
        }

        return services;
    }

    /// <summary>
    /// 注册默认仓储服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="options">数据库服务选项</param>
    private static void RegisterDefaultRepositories(IServiceCollection services, DatabaseServiceOptions options)
    {
        // PostgreSQL仓储
        if (options.UsePostgreSQL)
        {
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
        }

        // MongoDB仓储
        if (options.UseMongoDB)
        {
            services.AddScoped<IMongoActRepository, MongoActRepository>();
        }
    }

    /// <summary>
    /// 仅注册PostgreSQL服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置信息</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddPostgreSqlServices(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddDatabaseServices(configuration, options =>
        {
            options.UsePostgreSQL = true;
            options.UseMongoDB = false;
            options.RegisterDefaultRepositories = true;
            options.RegisterDynamicRepositoryFactory = true;
            options.UseDatabaseInitializer = true;
        });
    }

    /// <summary>
    /// 仅注册MongoDB服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置信息</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddMongoDbServices(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddDatabaseServices(configuration, options =>
        {
            options.UsePostgreSQL = false;
            options.UseMongoDB = true;
            options.RegisterDefaultRepositories = true;
            options.RegisterDynamicRepositoryFactory = true;
            options.UseDatabaseInitializer = false;
        });
    }
}

/// <summary>
/// 数据库服务配置选项
/// </summary>
public class DatabaseServiceOptions
{
    /// <summary>
    /// 是否使用PostgreSQL数据库
    /// </summary>
    public bool UsePostgreSQL { get; set; }

    /// <summary>
    /// 是否使用MongoDB数据库
    /// </summary>
    public bool UseMongoDB { get; set; }

    /// <summary>
    /// 是否注册默认仓储服务
    /// </summary>
    public bool RegisterDefaultRepositories { get; set; }

    /// <summary>
    /// 是否注册动态仓储工厂
    /// </summary>
    public bool RegisterDynamicRepositoryFactory { get; set; }

    /// <summary>
    /// 是否使用数据库初始化服务
    /// </summary>
    public bool UseDatabaseInitializer { get; set; }
}