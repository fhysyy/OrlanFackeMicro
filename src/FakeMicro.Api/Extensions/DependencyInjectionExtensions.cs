using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FakeMicro.Api.Services;
using FakeMicro.Api.Config;
using FakeMicro.DatabaseAccess;
using FakeMicro.Utilities.Storage;
using FakeMicro.Utilities.CodeGenerator;
using Hangfire;
using Hangfire.PostgreSql;
using FakeMicro.Utilities.Configuration;
using FakeMicro.Utilities.CodeGenerator.DependencyInjection;
using FakeMicro.Api.Security;
using FakeMicro.Grains.Eventing;
using FakeMicro.Interfaces.Eventing;
using FakeMicro.Grains.Services;
using FakeMicro.Grains.Extensions;
using StackExchange.Redis;
using System;
using Orleans.Hosting;
using Prometheus.Client.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using FakeMicro.Monitoring.OpenTelemetry;
using FakeMicro.Monitoring;


namespace FakeMicro.Api.Extensions;

/// <summary>
/// 集中式依赖注入扩展方法
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// 注册所有服务依赖
    /// </summary>
    public static void AddAllServices(this WebApplicationBuilder builder)
    {
        // 获取配置
        var configuration = builder.Configuration;
        var appSettings = configuration.GetAppSettings();
        var environment = builder.Environment;

        // 添加内存缓存服务
        builder.Services.AddMemoryCache();
        
        // 注册限流服务
        builder.Services.AddRateLimitServices(configuration);

        // 配置MVC控制器
        builder.Services.AddControllers()
            .AddNewtonsoftJson(op =>
            {
                op.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
                op.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                op.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                op.SerializerSettings.DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ";
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All);
            });

        // 注册配置服务
        builder.Services.AddConfigurationServices(configuration);

        // 注：由于已经配置了Orleans Silo，不需要单独配置客户端，Silo内置了客户端功能

        // 注册系统健康服务
        builder.Services.AddTransient<SystemHealthService>();

        // 注册Orleans任务执行器
        builder.Services.AddTransient<OrleansTaskExecutor>();

        // 配置HangFire
        builder.Services.AddHangfire(configuration => configuration
            .UsePostgreSqlStorage(appSettings.Hangfire.ConnectionString));

        // 添加HangFire服务器
        builder.Services.AddHangfireServer(options =>
        {
            options.WorkerCount = appSettings.Hangfire.WorkerCount;
        });

        // 添加认证和授权策略
        builder.Services.AddAuthorizationPolicies();

        // 配置CAP事件总线服务
        builder.Services.AddCapEventBus(configuration, environment);

        // 注册数据库服务
        builder.Services.AddDatabaseServices(configuration);

        // 注册表单配置相关服务
        builder.Services.AddFormConfigServices();
        builder.Services.AddFormConfigGrainProxies();

        // 注册文件存储服务
        builder.Services.AddFileStorage(configuration);

        // 注册代码生成器服务
        builder.Services.AddCodeGenerator(configuration);

        // 注册JWT服务
        builder.Services.AddScoped<JwtService>();
        builder.Services.AddScoped<JwtTokenService>();

        // 注册Orleans监控服务
        builder.Services.AddOrleansMonitoring(options =>
        {
            options.SlowQueryThresholdMs = 500;
            options.SlowGrainCallThresholdMs = 1000;
            options.EnableAlerting = true;
        });
        
        // 注册Grain服务
        builder.Services.AddGrainServices();
        
        // 配置OpenTelemetry
        builder.Services.AddOpenTelemetry()
            .WithMetrics(builder =>
            {
                // 收集ASP.NET Core指标
                builder.AddAspNetCoreInstrumentation();
                
                // 收集系统运行时指标（CPU、内存、网络等）
                builder.AddRuntimeInstrumentation();
                
                // 添加Orleans监控仪表化
                builder.AddMeter("FakeMicro.Orleans.Monitoring");
                
                // 导出到Prometheus
                builder.AddPrometheusExporter();
            })
            .WithTracing(builder =>
            {
                // 收集ASP.NET Core请求追踪
                builder.AddAspNetCoreInstrumentation();
                
                // 收集HTTP客户端请求追踪
                builder.AddHttpClientInstrumentation();
                
                // 配置采样率
                builder.SetSampler(new OpenTelemetry.Trace.AlwaysOnSampler());
                
                // 导出到控制台（生产环境可替换为Jaeger、Zipkin等）
                builder.AddConsoleExporter(options =>
                {
                    options.Targets = OpenTelemetry.Exporter.ConsoleExporterOutputTargets.Console;
                });
            });
            
        // 添加数据库连接池监控服务
        builder.Services.AddDatabaseConnectionPoolMonitor();

        // 注册用户服务
        builder.Services.AddScoped<IUserService, SimpleUserService>();
        
        // 注册事件流服务
        builder.Services.AddSingleton<IStreamProviderFactory, StreamProviderFactory>();
        builder.Services.AddSingleton<IEventPublisher, OrleansEventPublisher>();
        builder.Services.AddSingleton<IEventSubscriber, OrleansEventSubscriber>();
        builder.Services.AddSingleton<IEventStreamProvider, OrleansEventStreamProvider>();
        
        // 注册Redis缓存服务
        if (appSettings.Redis != null && !string.IsNullOrEmpty(appSettings.Redis.ConnectionString))
        {
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var config = ConfigurationOptions.Parse(appSettings.Redis.ConnectionString);
                config.ConnectTimeout = appSettings.Redis.ConnectTimeout;
                config.SyncTimeout = appSettings.Redis.SyncTimeout;
                config.AllowAdmin = appSettings.Redis.AllowAdmin;
                return ConnectionMultiplexer.Connect(config);
            });
            
            builder.Services.AddSingleton<IRedisCacheProvider>(sp =>
            {
                var redis = sp.GetRequiredService<IConnectionMultiplexer>();
                var logger = sp.GetRequiredService<ILogger<RedisCacheProvider>>();
                return new RedisCacheProvider(redis, logger, appSettings.Redis.Database);
            });
            
            builder.Services.AddSingleton<IRedisCacheManager, RedisCacheManager>();
        }
        
        // 不再需要注册OrleansDatabaseFixService为IHostedService，因为在Program.cs中已经手动调用了修复逻辑
    }

    /// <summary>
    /// 配置所有中间件
    /// </summary>
    public static void ConfigureAllMiddleware(this WebApplication app)
    {
        // 只在开发环境中启用Swagger，生产环境中禁用
        if (app.Environment.IsDevelopment())
        {
            // 配置Swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "FakeMicro API V1");
                c.RoutePrefix = "swagger";
                c.DocumentTitle = "FakeMicro API Documentation";
                c.DefaultModelsExpandDepth(-1); // 隐藏模型
                c.DisplayRequestDuration(); // 显示请求耗时
                c.EnableDeepLinking();
                c.ShowExtensions();
            });
        }

        // 配置HTTPS重定向
        app.UseHttpsRedirection();

        // 配置CORS
        app.UseCors("AllowConfiguredOrigins");

        // 注册全局异常处理中间件
        app.UseMiddleware<FakeMicro.Api.Middleware.ExceptionHandlingMiddleware>();
        app.UseMiddleware<FakeMicro.Api.Middleware.RequestResponseLoggingMiddleware>();
        
        // 注册限流中间件
        app.UseMiddleware<FakeMicro.Api.Middleware.RateLimitMiddleware>();
        
        // 注册幂等性中间件
        app.UseIdempotency();

        // 暂时注释掉认证和授权中间件，因为JWT认证服务已被注释
        // app.UseAuthentication();
        // app.UseAuthorization();

        // 配置OpenTelemetry Prometheus指标端点
        app.UseOpenTelemetryPrometheusScrapingEndpoint();

      
    }
}