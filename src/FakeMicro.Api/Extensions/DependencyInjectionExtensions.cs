using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FakeMicro.Api.Services;
using FakeMicro.DatabaseAccess;
using FakeMicro.Utilities.Storage;
using FakeMicro.Utilities.CodeGenerator;
using Hangfire;
using Hangfire.PostgreSql;
using FakeMicro.Utilities.Configuration;
using FakeMicro.Utilities.CodeGenerator.DependencyInjection;
using FakeMicro.Api.Security;
using System;
using Orleans.Hosting;
using Prometheus.Client.AspNetCore;
using OpenTelemetry.Metrics;
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
        
        // 配置OpenTelemetry指标收集
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
            });
            
        // 添加数据库连接池监控服务
        builder.Services.AddDatabaseConnectionPoolMonitor();

        // 注册用户服务
        builder.Services.AddScoped<IUserService, SimpleUserService>();
        
        // 不再需要注册OrleansDatabaseFixService为IHostedService，因为在Program.cs中已经手动调用了修复逻辑
    }

    /// <summary>
    /// 配置所有中间件
    /// </summary>
    public static void ConfigureAllMiddleware(this WebApplication app)
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

        // 配置HTTPS重定向
        app.UseHttpsRedirection();

        // 注册全局异常处理中间件
        app.UseMiddleware<FakeMicro.Api.Middleware.ExceptionHandlingMiddleware>();
        app.UseMiddleware<FakeMicro.Api.Middleware.RequestResponseLoggingMiddleware>();
        
        // 注册幂等性中间件
        app.UseIdempotency();

        // 添加认证和授权中间件
        app.UseAuthentication();
        app.UseAuthorization();

        // 配置OpenTelemetry Prometheus指标端点
        app.UseOpenTelemetryPrometheusScrapingEndpoint();

        // 配置路由
        app.MapControllers();
        app.MapGet("/health", () => "Healthy");
    }
}