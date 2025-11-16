using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using FakeMicro.DatabaseAccess.Services;

namespace FakeMicro.DatabaseAccess.Extensions
{
    /// <summary>
    /// 数据库初始化服务扩展方法
    /// 遵循Orleans框架最佳实践，提供便捷的服务注册方式
    /// </summary>
    public static class DatabaseInitializerServiceExtensions
    {
        /// <summary>
        /// 添加数据库初始化服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置对象</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDatabaseInitializer(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // 配置数据库初始化选项
            services.Configure<DatabaseInitializerOptions>(
                configuration.GetSection("DatabaseInitializer"));

            // 注册数据库初始化托管服务
            services.AddHostedService<DatabaseInitializerHostedService>();

            return services;
        }

        /// <summary>
        /// 添加数据库初始化服务（带自定义配置）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configureOptions">配置选项的委托</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDatabaseInitializer(
            this IServiceCollection services,
            Action<DatabaseInitializerOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            // 配置数据库初始化选项
            services.Configure(configureOptions);

            // 注册数据库初始化托管服务
            services.AddHostedService<DatabaseInitializerHostedService>();

            return services;
        }

        /// <summary>
        /// 添加数据库初始化服务（完整配置）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置对象</param>
        /// <param name="configureOptions">额外配置选项的委托</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDatabaseInitializer(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<DatabaseInitializerOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            // 配置数据库初始化选项
            services.Configure<DatabaseInitializerOptions>(
                configuration.GetSection("DatabaseInitializer"));
            
            services.Configure(configureOptions);

            // 注册数据库初始化托管服务
            services.AddHostedService<DatabaseInitializerHostedService>();

            return services;
        }
    }
}