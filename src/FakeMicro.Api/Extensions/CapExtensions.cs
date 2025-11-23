using DotNetCore.CAP;
using FakeMicro.Api.Middleware;
using FakeMicro.Api.Services;
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
            // 从配置中获取CAP相关配置
            string connectionString = configuration.GetConnectionString("CAPConnection");
            int failedRetryCount = configuration.GetValue<int>("CAP:FailedRetryCount", 3);
            int failedRetryInterval = configuration.GetValue<int>("CAP:FailedRetryInterval", 30);
            int succeedMessageExpiredAfter = configuration.GetValue<int>("CAP:SucceedMessageExpiredAfter", 3600);

            // 注册CAP服务
            services.AddCap(options =>
            {
                // 使用PostgreSQL作为存储
                options.UsePostgreSql(connectionString);
                
                // 启用Dashboard
                options.UseDashboard();
                
                // 配置重试策略
                options.FailedRetryCount = failedRetryCount;
                options.FailedRetryInterval = failedRetryInterval;
                options.SucceedMessageExpiredAfter = succeedMessageExpiredAfter;
                
                // 配置消费者线程数
                options.ConsumerThreadCount = 1;
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