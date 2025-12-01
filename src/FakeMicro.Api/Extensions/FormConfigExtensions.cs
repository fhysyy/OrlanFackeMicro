using System;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using FakeMicro.Interfaces;

namespace FakeMicro.Api.Extensions
{
    /// <summary>
    /// 表单配置模块扩展方法
    /// 提供表单配置相关服务的注册功能
    /// </summary>
    public static class FormConfigExtensions
    {
        /// <summary>
        /// 注册表单配置相关服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddFormConfigServices(this IServiceCollection services)
        {
            // 注册表单配置Grain工厂服务（可选）
            services.AddSingleton<FormConfigGrainFactory>();

            // 注册表单配置客户端服务（提供更方便的API调用方式）
            services.AddScoped<IFormConfigClientService, FormConfigClientService>();

            return services;
        }

        /// <summary>
        /// 注册表单配置Grain接口的代理服务
        /// 此扩展方法用于简化服务层对Grain的访问
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddFormConfigGrainProxies(this IServiceCollection services)
        {
            // 注册IFormConfigGrain的代理工厂（自定义代理模式）
            services.AddSingleton<Func<string, IFormConfigGrain>>(provider => 
            {
                var client = provider.GetRequiredService<IClusterClient>();
                return id => client.GetGrain<IFormConfigGrain>(id);
            });

            // 注册IFormConfigService的单例代理
            services.AddSingleton<IFormConfigService>(provider => 
            {
                var client = provider.GetRequiredService<IClusterClient>();
                // 使用固定GUID来获取单例服务Grain
                return client.GetGrain<IFormConfigService>(Guid.Parse("00000000-0000-0000-0000-000000000001"));
            });

            return services;
        }
    }

    /// <summary>
    /// 表单配置Grain工厂
    /// 提供获取表单配置相关Grain的便捷方法
    /// </summary>
    public class FormConfigGrainFactory
    {
        private readonly IClusterClient _clusterClient;

        public FormConfigGrainFactory(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        /// <summary>
        /// 获取表单配置Grain
        /// </summary>
        /// <param name="id">表单配置ID</param>
        /// <returns>表单配置Grain实例</returns>
        public IFormConfigGrain GetFormConfigGrain(string id)
        {
            return _clusterClient.GetGrain<IFormConfigGrain>(id);
        }

        /// <summary>
        /// 获取表单配置服务Grain
        /// </summary>
        /// <returns>表单配置服务Grain实例</returns>
        public IFormConfigService GetFormConfigService()
        {
            // 使用固定GUID来确保获取同一个服务Grain实例
            return _clusterClient.GetGrain<IFormConfigService>(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        }

        /// <summary>
        /// 创建新的表单配置Grain
        /// </summary>
        /// <returns>新的表单配置Grain实例</returns>
        public IFormConfigGrain CreateNewFormConfigGrain()
        {
            // 使用新的GUID作为临时ID
            return _clusterClient.GetGrain<IFormConfigGrain>(System.Guid.NewGuid().ToString());
        }
    }

    /// <summary>
    /// 表单配置客户端服务接口
    /// 提供更友好的API调用方式
    /// </summary>
    public interface IFormConfigClientService
    {
        // 可以在这里定义更高级别的业务方法
        // 简化控制器中的代码
    }

    /// <summary>
    /// 表单配置客户端服务实现
    /// </summary>
    public class FormConfigClientService : IFormConfigClientService
    {
        private readonly FormConfigGrainFactory _grainFactory;

        public FormConfigClientService(FormConfigGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
        }

        // 实现IFormConfigClientService接口的方法
        // 可以在这里添加业务逻辑处理
    }
}