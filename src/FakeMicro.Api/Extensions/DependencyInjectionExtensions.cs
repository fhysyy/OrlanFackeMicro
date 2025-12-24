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

        // 配置Orleans客户端 - 使用统一配置方式
        builder.AddOrleansClientWithConfiguration();

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

        // 注册用户服务
        builder.Services.AddScoped<IUserService, SimpleUserService>();
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

        // 配置路由
        app.MapControllers();
        app.MapGet("/health", () => "Healthy");
    }
}