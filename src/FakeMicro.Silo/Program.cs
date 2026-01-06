using FakeMicro.Grains.Eventing;
using FakeMicro.Grains.Services;
using FakeMicro.Interfaces.Eventing;
using FakeMicro.Utilities.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Streams;
using OrleansDashboard.Model;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace FakeMicro.Silo;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Orleans Silo 启动中 ===");

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                var appSettings = context.Configuration.GetAppSettings();
                
                services.AddSingleton(appSettings);
                
                services.AddLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                });

                services.AddOptions();

                services.AddSingleton<IDistributedLockManager, DistributedLockManager>();
                services.AddSingleton<IStreamProviderFactory, StreamProviderFactory>();
                services.AddSingleton<IEventPublisher, OrleansEventPublisher>();
                services.AddSingleton<IEventSubscriber, OrleansEventSubscriber>();
                services.AddSingleton<IEventStreamProvider, OrleansEventStreamProvider>();

                if (appSettings.Redis != null && !string.IsNullOrEmpty(appSettings.Redis.ConnectionString))
                {
                    services.AddSingleton<IConnectionMultiplexer>(sp =>
                    {
                        var config = ConfigurationOptions.Parse(appSettings.Redis.ConnectionString);
                        config.ConnectTimeout = appSettings.Redis.ConnectTimeout;
                        config.SyncTimeout = appSettings.Redis.SyncTimeout;
                        config.AllowAdmin = appSettings.Redis.AllowAdmin;
                        return ConnectionMultiplexer.Connect(config);
                    });
                    
                    services.AddSingleton<IRedisCacheProvider>(sp =>
                    {
                        var redis = sp.GetRequiredService<IConnectionMultiplexer>();
                        var logger = sp.GetRequiredService<ILogger<RedisCacheProvider>>();
                        return new RedisCacheProvider(redis, logger, appSettings.Redis.Database);
                    });
                    
                    services.AddSingleton<IRedisCacheManager, RedisCacheManager>();
                    
                    Console.WriteLine($"Redis缓存已配置: {appSettings.Redis.ConnectionString}");
                }
                else
                {
                    Console.WriteLine("Redis未配置，将使用默认缓存方式");
                }
            })
            .UseOrleans((context, siloBuilder) =>
            {
                var appSettings = context.Configuration.GetAppSettings();
                var configuration = context.Configuration;
                
                ConfigureClusterOptions(siloBuilder, appSettings);
                ConfigureClustering(siloBuilder, appSettings);
                
                var redisConfigured = ConfigureRedis(siloBuilder, appSettings);
                
                if (!appSettings.Orleans.UseLocalhostClustering && !redisConfigured)
                {
                    ConfigureStorage(siloBuilder, appSettings);
                }
                else if (appSettings.Orleans.UseLocalhostClustering && !redisConfigured)
                {
                    siloBuilder.AddMemoryGrainStorageAsDefault();
                    siloBuilder.AddMemoryGrainStorage("PubSubStore");
                }
                
                ConfigureMessaging(siloBuilder);
                ConfigureClusteringOptions(siloBuilder);
                ConfigureLogging(siloBuilder);
                ConfigureDashboard(siloBuilder, appSettings);
                ConfigureStreaming(siloBuilder, appSettings);
            })
            .Build();

        try
        {
            Console.WriteLine("=== Orleans Silo 正在启动 ===");
            await host.StartAsync();
            
            Console.WriteLine("=== Orleans Silo 启动成功 ===");
            Console.WriteLine("Silo已成功启动并运行");
            Console.WriteLine("按Ctrl+C退出");
            
            await host.WaitForShutdownAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Silo启动失败: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }
        finally
        {
            Console.WriteLine("=== Orleans Silo 正在关闭 ===");
        }
    }

    private static void ConfigureClusterOptions(ISiloBuilder builder, AppSettings appSettings)
    {
        builder.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = appSettings.Orleans.ClusterId;
            options.ServiceId = appSettings.Orleans.ServiceId;
        });
    }

    private static void ConfigureClustering(ISiloBuilder builder, AppSettings appSettings)
    {
        if (appSettings.Orleans.UseLocalhostClustering)
        {
            builder.UseLocalhostClustering(
                clusterId: appSettings.Orleans.ClusterId,
                serviceId: appSettings.Orleans.ServiceId
            );
        }
        else
        {
            var connectionString = appSettings.Database.GetConnectionString();
            builder.UseAdoNetClustering(options =>
            {
                options.Invariant = "Npgsql";
                options.ConnectionString = connectionString;
            });

            builder.Configure<SiloOptions>(options =>
            {
                options.SiloName = "FakeMicroSilo";
            });
        }

        ConfigureReminderService(builder, appSettings);
    }

    private static void ConfigureReminderService(ISiloBuilder builder, AppSettings appSettings)
    {
        builder.UseInMemoryReminderService();
        Console.WriteLine("Orleans Reminder服务已配置（使用内存存储）");
    }

    private static void ConfigureStorage(ISiloBuilder builder, AppSettings appSettings)
    {
        var connectionString = appSettings.Database.GetConnectionString();
        
        builder.AddAdoNetGrainStorageAsDefault(options =>
        {
            options.Invariant = "Npgsql";
            options.ConnectionString = connectionString;
        });

        builder.AddAdoNetGrainStorage("PubSubStore", options =>
        {
            options.Invariant = "Npgsql";
            options.ConnectionString = connectionString;
        });

        builder.AddAdoNetGrainStorage("UserStateStore", options =>
        {
            options.Invariant = "Npgsql";
            options.ConnectionString = connectionString;
        });
    }

    private static void ConfigureMessaging(ISiloBuilder builder)
    {
        builder.Configure<MessagingOptions>(options =>
        {
            options.ResponseTimeout = TimeSpan.FromSeconds(30);
            options.MaxMessageBodySize = 10 * 1024 * 1024;
        });

        builder.Configure<SiloMessagingOptions>(options =>
        {
            options.MaxForwardCount = 10;
        });
    }

    private static void ConfigureClusteringOptions(ISiloBuilder builder)
    {
        builder.Configure<ClusterMembershipOptions>(options =>
        {
            options.NumMissedProbesLimit = 3;
            options.ProbeTimeout = TimeSpan.FromSeconds(10);
        });

        builder.Configure<SiloOptions>(options =>
        {
            options.SiloName = Environment.MachineName;
        });
    }

    private static void ConfigureLogging(ISiloBuilder builder)
    {
        builder.ConfigureLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });
    }

    private static void ConfigureDashboard(ISiloBuilder builder, AppSettings appSettings)
    {
        if (appSettings.Orleans.EnableDashboard)
        {
            builder.UseDashboard(options =>
            {
                options.HostSelf = true;
                options.Port = appSettings.Orleans.DashboardPort;
                options.Host = "*";
                options.CounterUpdateIntervalMs = 1000;
            });
        }
    }

    private static void ConfigureStreaming(ISiloBuilder builder, AppSettings appSettings)
    {
        builder.AddMemoryStreams("SMSProvider");

        builder.AddMemoryGrainStorage("PubSubStore");

        builder.AddMemoryStreams("DefaultStream");

        builder.AddMemoryStreams("UserEventsStream");

        builder.AddMemoryStreams("MessageEventsStream");

        builder.AddMemoryStreams("AuthEventsStream");

        Console.WriteLine("Orleans流提供程序已配置: SMSProvider, DefaultStream, UserEventsStream, MessageEventsStream, AuthEventsStream");
    }

    private static bool ConfigureRedis(ISiloBuilder builder, AppSettings appSettings)
    {
        try
        {
            if (appSettings.Redis == null || string.IsNullOrEmpty(appSettings.Redis.ConnectionString))
            {
                Console.WriteLine("Redis连接字符串未配置，将使用默认存储方式");
                return false;
            }

            var redisConnectionString = appSettings.Redis.ConnectionString;
            
            // 暂时注释掉Redis存储配置，使用内存存储替代
            // 等待Redis包引用问题解决后再启用
            Console.WriteLine($"Redis配置已检测到，但暂时使用内存存储: {redisConnectionString}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Redis配置失败，将使用默认存储方式: {ex.Message}");
            return false;
        }
    }
}
