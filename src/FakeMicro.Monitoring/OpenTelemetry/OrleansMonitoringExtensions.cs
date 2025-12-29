using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using FakeMicro.Monitoring;
using Microsoft.AspNetCore.Builder;

namespace FakeMicro.Monitoring.OpenTelemetry;

/// <summary>
/// Orleans监控扩展 - 用于配置OpenTelemetry集成
/// </summary>
public static class OrleansMonitoringExtensions
{
    /// <summary>
    /// 添加Orleans监控仪表化到OpenTelemetry
    /// </summary>
    public static MeterProviderBuilder AddOrleansMonitoringInstrumentation(
        this MeterProviderBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }
        
        // 配置仪表收集器
        builder.AddMeter("FakeMicro.Orleans.Monitoring");
        
        return builder;
    }
    
    /// <summary>
    /// 注册Orleans监控服务
    /// </summary>
    public static IServiceCollection AddOrleansMonitoring(
        this IServiceCollection services,
        Action<MonitoringOptions>? configureOptions = null)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }
        
        // 注册PerformanceMonitor为单例
        services.AddSingleton<PerformanceMonitor>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<PerformanceMonitor>>();
            return new PerformanceMonitor(logger, configureOptions);
        });
        
        // 注册Orleans监控仪表化，使用工厂模式确保正确的依赖注入
        services.AddSingleton<OrleansMonitoringInstrumentation>(sp =>
        {
            var performanceMonitor = sp.GetRequiredService<PerformanceMonitor>();
            return new OrleansMonitoringInstrumentation(performanceMonitor);
        });
        
        // 注册数据库连接池监控服务
        services.AddHostedService<DatabaseConnectionPoolMonitor>();
        
        return services;
    }
    
    /// <summary>
    /// 配置Orleans监控
    /// </summary>
    public static void ConfigureOrleansMonitoring(
        this IApplicationBuilder app)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }
        
        // 确保OrleansMonitoringInstrumentation已初始化
        app.ApplicationServices.GetRequiredService<OrleansMonitoringInstrumentation>();
    }
}