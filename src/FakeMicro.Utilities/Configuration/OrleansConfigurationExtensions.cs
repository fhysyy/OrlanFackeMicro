using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Clustering.AdoNet;
using FakeMicro.Utilities.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Orleans.Storage;
using System;

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
            
            // 只有在不使用本地集群时才配置存储
            if (!appSettings.Orleans.UseLocalhostClustering)
            {
                ConfigureStorage(siloBuilder, appSettings);
            }
            else
            {
                // 对于本地集群，使用内存存储
                siloBuilder.AddMemoryGrainStorageAsDefault();
                siloBuilder.AddMemoryGrainStorage("PubSubStore");
            }
            
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
        if (appSettings.Orleans.UseLocalhostClustering)
        {
            // 开发环境使用本地集群
            builder.UseLocalhostClustering(
                gatewayPort: appSettings.Orleans.GatewayPort,
                serviceId: appSettings.Orleans.ServiceId,
                clusterId: appSettings.Orleans.ClusterId
            );
        }
        else
        {
            // 生产环境使用PostgreSQL作为集群成员资格存储
            var connectionString = appSettings.Database.GetConnectionString();
            builder.UseAdoNetClustering(options =>
            {
                options.Invariant = "Npgsql";
                options.ConnectionString = connectionString;
                // 在新版本Orleans中，UseJsonFormat已被移除，不需要设置
            });
        }
    }

    /// <summary>
    /// 配置集群连接（Silo）
    /// </summary>
    private static void ConfigureClustering(ISiloBuilder builder, AppSettings appSettings)
    {
        if (appSettings.Orleans.UseLocalhostClustering)
        {
            // 开发环境使用本地集群
            builder.UseLocalhostClustering(
                clusterId: appSettings.Orleans.ClusterId,
                serviceId: appSettings.Orleans.ServiceId
            );
        }
        else
        {
            // 生产环境使用PostgreSQL作为集群成员资格存储
            var connectionString = appSettings.Database.GetConnectionString();
            builder.UseAdoNetClustering(options =>
            {
                options.Invariant = "Npgsql";
                options.ConnectionString = connectionString;
                // 在新版本Orleans中，UseJsonFormat已被移除，不需要设置
            });

            // 添加AdoNet事务存储
            // builder.AddAdoNetTransactionLogStorage(options =>
            // {
            //     options.Invariant = "Npgsql";
            //     options.ConnectionString = connectionString;
            // });
            
            // 配置Silo选项
            builder.Configure<SiloOptions>(options =>
            {
                // 设置Silo名称
                options.SiloName = "FakeMicroSilo";
                // 在新版本Orleans中，ActivationCountBasedPlacementEnabled已被移除，不需要设置
            });

            // 配置集群选项
            builder.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = appSettings.Orleans.ClusterId;
                options.ServiceId = appSettings.Orleans.ServiceId;
            });

            // 配置数据存储
            ConfigureGrainStorage(builder, connectionString);
        }
    }

    /// <summary>
    /// 配置存储
    /// </summary>
    private static void ConfigureStorage(ISiloBuilder builder, AppSettings appSettings)
    {
        var connectionString = appSettings.Database.GetConnectionString();
        ConfigureGrainStorage(builder, connectionString);
    }

    /// <summary>
    /// 配置Grain存储
    /// </summary>
    private static void ConfigureGrainStorage(ISiloBuilder builder, string connectionString)
    {
        // 使用PostgreSQL作为默认存储
        builder.AddAdoNetGrainStorageAsDefault(options =>
        {
            options.Invariant = "Npgsql";
            options.ConnectionString = connectionString;
            // 在新版本Orleans中，UseJsonFormat已被移除，不需要设置
        });

        // 使用PostgreSQL作为发布/订阅存储
        builder.AddAdoNetGrainStorage("PubSubStore", options =>
        {
            options.Invariant = "Npgsql";
            options.ConnectionString = connectionString;
            // 在新版本Orleans中，UseJsonFormat已被移除，不需要设置
        });

        // 配置用户状态存储
        builder.AddAdoNetGrainStorage("UserStateStore", options =>
        {
            options.Invariant = "Npgsql";
            options.ConnectionString = connectionString;
            // 在新版本Orleans中，UseJsonFormat已被移除，不需要设置
        });

        // 配置Orleans系统存储
        builder.AddAdoNetGrainStorage("OrleansClusterManifest", options =>
        {
            options.Invariant = "Npgsql";
            options.ConnectionString = connectionString;
            // 在新版本Orleans中，UseJsonFormat已被移除，不需要设置
        });
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