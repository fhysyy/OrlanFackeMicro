using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using FakeMicro.Utilities.Configuration.Extensions;

namespace FakeMicro.Utilities.Configuration;

/// <summary>
/// 配置扩展方法
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// 获取应用程序配置
    /// </summary>
    public static AppSettings GetAppSettings(this IConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }
        
        var settings = new AppSettings();
        configuration.Bind(settings);
        return settings;
    }
    
    /// <summary>
    /// 添加配置服务
    /// </summary>
    public static IServiceCollection AddConfigurationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 注册AppSettings
        var settings = configuration.GetAppSettings();
        Console.WriteLine(settings);
        // 验证配置
        settings.ValidateConfiguration();
        
        // 注册配置验证器服务
        services.AddSingleton<IConfigurationValidator, ConfigurationValidatorService>();
        
        // 直接注册各配置类的单例实例
        services.AddSingleton(settings);
        services.AddSingleton(settings.Database);
        services.AddSingleton(settings.Orleans);
        
        services.AddSingleton(settings.Jwt);
        services.AddSingleton(settings.RabbitMQ);
        services.AddSingleton(settings.RateLimit);
        services.AddSingleton(settings.AdvancedRateLimit);
        services.AddSingleton(settings.Cors);
        services.AddSingleton(settings.FileStorage);
        services.AddSingleton(settings.Elasticsearch);
        
        // 添加配置验证启动过滤器
        services.AddTransient<IStartupFilter, ConfigurationValidationStartupFilter>();
        
        return services;
    }
    
    /// <summary>
    /// 添加简化的配置服务
    /// </summary>
    public static IServiceCollection AddSimpleAppSettings(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddConfigurationServices(configuration);
    }
    
    /// <summary>
    /// 验证配置的有效性
    /// </summary>
    public static void ValidateConfiguration(this AppSettings settings)
    {
        ConfigurationValidator.ValidateConfiguration(settings);
        ConfigurationValidator.ValidateEnvironmentConfiguration(settings);
        ConfigurationValidator.ValidateOrleansConfiguration(settings.Orleans);
    }
    
    /// <summary>
    /// 构建配置并添加环境变量占位符支持
    /// </summary>
    public static IConfigurationBuilder AddDefaultConfiguration(this IConfigurationBuilder builder, string envPrefix = "FAKEMICRO_")
    {
        return builder
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    }
}