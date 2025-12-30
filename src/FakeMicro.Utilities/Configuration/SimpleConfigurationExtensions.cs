using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FakeMicro.Utilities.Configuration;

/// <summary>
/// 简化配置扩展方法
/// </summary>
public static class SimpleConfigurationExtensions
{
    /// <summary>
    /// 添加简化配置服务
    /// </summary>
    public static IServiceCollection AddSimpleAppSettings(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = new SimpleAppSettings();
        configuration.Bind(settings);
        
        // 只从appsettings.json读取配置，不再从环境变量覆盖
        
        services.AddSingleton(settings);
        return services;
    }
}