using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Hosting;
using FakeMicro.Utilities.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Orleans.Storage;

namespace FakeMicro.Utilities.Configuration;

/// <summary>
/// Orleans配置扩展类 - 遵循Orleans 9.x最佳实践
/// </summary>
public static class OrleansConfigurationExtensions
{
    /// <summary>
    /// 配置Orleans客户端 - 统一配置方式
    /// </summary>
    public static IServiceCollection AddOrleansClientWithConfiguration(this IServiceCollection services, AppSettings appSettings)
    {
        services.AddOrleansClient(clientBuilder =>
        {
            ConfigureClusterOptions(clientBuilder, appSettings);
            ConfigureClustering(clientBuilder, appSettings);
            ConfigureMessaging(clientBuilder);
            ConfigureLogging(clientBuilder);
        });

        return services;
    }

    /// <summary>
    /// 配置Orleans客户端 - 支持WebApplicationBuilder
    /// </summary>
    public static void AddOrleansClientWithConfiguration(this WebApplicationBuilder builder)
    {
        var appSettings = builder.Configuration.GetAppSettings();
        builder.Services.AddOrleansClientWithConfiguration(appSettings);
    }

    /// <summary>
    /// 配置Orleans Silo - 统一配置方式
    /// </summary>
    public static IHostBuilder ConfigureOrleansSilo(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseOrleans((context, siloBuilder) =>
        {
            var appSettings = context.Configuration.GetAppSettings();
            
            ConfigureClusterOptions(siloBuilder, appSettings);
            ConfigureClustering(siloBuilder, appSettings);
            ConfigureStorage(siloBuilder, appSettings);
            ConfigureMessaging(siloBuilder);
            ConfigureClusteringOptions(siloBuilder);
            ConfigureLogging(siloBuilder);
        });

        return hostBuilder;
    }

    /// <summary>
    /// 配置集群选项
    /// </summary>
    private static void ConfigureClusterOptions(IClientBuilder builder, AppSettings appSettings)
    {
        builder.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = appSettings.Orleans.ClusterId;
            options.ServiceId = appSettings.Orleans.ServiceId;
        });
    }

    /// <summary>
    /// 配置集群选项（Silo）
    /// </summary>
    private static void ConfigureClusterOptions(ISiloBuilder builder, AppSettings appSettings)
    {
        builder.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = appSettings.Orleans.ClusterId;
            options.ServiceId = appSettings.Orleans.ServiceId;
        });
    }

    /// <summary>
    /// 配置集群连接
    /// </summary>
    private static void ConfigureClustering(IClientBuilder builder, AppSettings appSettings)
    {
        builder.UseLocalhostClustering(
            gatewayPort: appSettings.Orleans.GatewayPort,
            serviceId: appSettings.Orleans.ServiceId,
            clusterId: appSettings.Orleans.ClusterId
        );
    }

    /// <summary>
    /// 配置集群连接（Silo）
    /// </summary>
    private static void ConfigureClustering(ISiloBuilder builder, AppSettings appSettings)
    {
        builder.UseLocalhostClustering(
            clusterId: appSettings.Orleans.ClusterId,
            serviceId: appSettings.Orleans.ServiceId
        );
    }

    /// <summary>
    /// 配置存储
    /// </summary>
    private static void ConfigureStorage(ISiloBuilder builder, AppSettings appSettings)
    {
        var connectionString = appSettings.Database.GetConnectionString();
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            // 使用PostgreSQL作为默认存储
            builder.AddAdoNetGrainStorageAsDefault(options =>
            {
                options.Invariant = "Npgsql";
                options.ConnectionString = connectionString;
            });
            
            // 使用PostgreSQL作为发布/订阅存储
            builder.AddAdoNetGrainStorage("PubSubStore", options =>
            {
                options.Invariant = "Npgsql";
                options.ConnectionString = connectionString;
            });

            // 配置用户状态存储
            builder.AddAdoNetGrainStorage("UserStateStore", options =>
            {
                options.Invariant = "Npgsql";
                options.ConnectionString = connectionString;
            });

            // 配置Orleans系统存储
            builder.AddAdoNetGrainStorage("OrleansClusterManifest", options =>
            {
                options.Invariant = "Npgsql";
                options.ConnectionString = connectionString;
            });
        }
    }

    /// <summary>
    /// 配置消息选项
    /// </summary>
    private static void ConfigureMessaging(IClientBuilder builder)
    {
        builder.Configure<ClientMessagingOptions>(options =>
        {
            options.ResponseTimeout = TimeSpan.FromSeconds(45);
            options.MaxMessageBodySize = 50 * 1024 * 1024; // 50MB
        });
    }

    /// <summary>
    /// 配置消息选项（Silo）
    /// </summary>
    private static void ConfigureMessaging(ISiloBuilder builder)
    {
        builder.Configure<MessagingOptions>(options =>
        {
            options.ResponseTimeout = TimeSpan.FromSeconds(30);
            options.MaxMessageBodySize = 10 * 1024 * 1024; // 10MB
        });
    }

    /// <summary>
    /// 配置集群成员选项
    /// </summary>
    private static void ConfigureClusteringOptions(ISiloBuilder builder)
    {
        builder.Configure<ClusterMembershipOptions>(options =>
        {
            options.NumMissedProbesLimit = 3;
        });
    }

    /// <summary>
    /// 配置日志记录
    /// </summary>
    private static void ConfigureLogging(IClientBuilder builder)
    {
        // 在Orleans 9.x中，IClientBuilder不再直接支持ConfigureLogging
        // 日志配置应该在HostBuilder或WebApplicationBuilder层面进行
    }

    /// <summary>
    /// 配置日志记录（Silo）
    /// </summary>
    private static void ConfigureLogging(ISiloBuilder builder)
    {
        builder.ConfigureLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });
    }
}