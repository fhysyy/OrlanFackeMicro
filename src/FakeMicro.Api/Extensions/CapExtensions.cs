using System;
using DotNetCore.CAP;
using FakeMicro.Api.Middleware;
using FakeMicro.Api.Services;
using FakeMicro.Utilities.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FakeMicro.Api.Extensions
{
    /// <summary>
    /// CAP事件总线扩展方法
    /// 提供CAP服务的注册和配置功能
    /// </summary>
    public static class CapExtensions
    {
        /// <summary>
        /// 添加CAP事件总线服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置信息</param>
        /// <param name="hostEnvironment">主机环境信息</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddCapEventBus(this IServiceCollection services, 
            IConfiguration configuration, IHostEnvironment hostEnvironment)
        {
            // 获取应用配置
            var appSettings = configuration.Get<AppSettings>() ?? new AppSettings();
            
            // 如果CAP未启用，则跳过注册
            if (!appSettings.CAP.Enabled)
            {
                // 注册一个空的扩展发布器，避免依赖项注入错误
                services.AddScoped<IExtendedCapPublisher>(sp => 
                    throw new InvalidOperationException("CAP is not enabled. Enable CAP in configuration to use it."));
                return services;
            }

            // 从配置中获取数据库连接字符串
            string connectionString = appSettings.Database.GetConnectionString();
            
            // 获取RabbitMQ配置
            var rabbitMQConfig = appSettings.RabbitMQ;
            var capConfig = appSettings.CAP;

            // 注册CAP服务
            services.AddCap(options =>
            {
                // 使用PostgreSQL作为存储
                options.UsePostgreSql(connectionString);
                
                // 配置RabbitMQ
                options.UseRabbitMQ(opt =>
                {
                    opt.HostName = rabbitMQConfig.GetHostName();
                    opt.Port = rabbitMQConfig.Port;
                    opt.UserName = rabbitMQConfig.UserName;
                    opt.Password = rabbitMQConfig.GetPassword();
                    opt.VirtualHost = rabbitMQConfig.VirtualHost;
                });
                
                // 配置Dashboard
                if (capConfig.UseDashboard)
                {
                    options.UseDashboard(d =>
                    {
                        d.AllowAnonymousExplicit = capConfig.DashboardAllowAnonymous;
                        d.PathMatch = capConfig.DashboardPath;
                    });
                }
                
                // 配置重试策略
                options.FailedRetryCount = capConfig.FailedRetryCount;
                options.FailedRetryInterval = capConfig.FailedRetryInterval;
                options.SucceedMessageExpiredAfter = capConfig.SucceedMessageExpiredAfter;
                
                // 配置消费者线程数
                options.ConsumerThreadCount = capConfig.ConsumerThreadCount;
            });
            
            // 添加外部订阅管理服务
            services.AddScoped<IExtendedCapPublisher, ExtendedCapPublisher>();

            return services;
        }

        /// <summary>
        /// 配置CAP中间件
        /// </summary>
        /// <param name="builder">应用构建器</param>
        /// <returns>应用构建器</returns>
        public static IApplicationBuilder UseCap(this IApplicationBuilder builder)
        {
            // 确保CAP中间件被正确添加
            builder.UseMiddleware<CapMiddleware>();
            
            return builder;
        }
    }
}