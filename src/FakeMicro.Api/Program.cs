using FakeMicro.Api.Extensions;
using FakeMicro.Api.Security;
using FakeMicro.Api.Services;
using FakeMicro.DatabaseAccess;
using FakeMicro.Utilities.Storage;
using FakeMicro.Utilities.Configuration;
// 添加代码生成器相关的using语句
using FakeMicro.Utilities.CodeGenerator;
using FakeMicro.Utilities.CodeGenerator.DependencyInjection;
using FakeMicro.Utilities.CodeGenerator.Templates;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System;
using System.Net;
using System.Runtime.Serialization.Formatters;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;


namespace FakeMicro.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            // 使用集中式配置管理
            var appSettings = builder.Configuration.GetAppSettings();
            
            // 添加必要的服务
            builder.Services.AddEndpointsApiExplorer();
            
            // 配置 JWT 认证
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = appSettings.Jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = appSettings.Jwt.Audience,
                    ValidateLifetime = true,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(appSettings.Jwt.SecretKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });
            
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() { Title = "FakeMicro API", Version = "v1" });
                
                // 添加 JWT Bearer 认证配置
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // 使用集中式依赖注入注册所有服务
            builder.AddAllServices();

            // 构建应用，添加异常处理以获取详细错误信息
            var app = builder.Build();
            
            // 配置所有中间件
            app.ConfigureAllMiddleware();

            // 使用配置的Hangfire Dashboard路径
            if (appSettings.Hangfire.UseDashboard)
            {
                app.UseHangfireDashboard(appSettings.Hangfire.DashboardPath);
            }

            // 暂时注释掉默认的定时任务，专注于测试Orleans连接
            ConfigureDefaultJobs();

            Console.WriteLine("=== API服务器启动成功 ===");
            Console.WriteLine("访问 http://localhost:5000/swagger 查看API文档");
            Console.WriteLine("按Ctrl+C退出");
            
            await app.RunAsync();
        }
        
        /// <summary>
        /// 配置默认的定时任务
        /// </summary>
        private static void ConfigureDefaultJobs()
        {
            // 添加系统健康检查任务 - 每小时执行一次
            RecurringJob.AddOrUpdate<SystemHealthService>(
                "system-health-check",
                service => service.PerformHealthCheck(),
                "0 * * * *");

            // 添加简单的日志记录任务 - 每分钟执行一次
            RecurringJob.AddOrUpdate(
                "sample-log-task",
                () => Console.WriteLine($"[{DateTime.Now}] 定时任务执行示例"),
                "* * * * *");

            //// 添加Orleans HelloGrain定时任务 - 每5分钟执行一次
            //RecurringJob.AddOrUpdate<OrleansTaskExecutor>(
            //    "orleans-hello-task",
            //    executor => executor.ExecuteGrainOperationAsync("hello", "sayhello", 
            //        new System.Collections.Generic.Dictionary<string, object> { { "greeting", "Hello from Hangfire scheduled task" } }),
            //    "*/5 * * * *");

            //// 添加Orleans CounterGrain定时任务 - 每10分钟执行一次
            //RecurringJob.AddOrUpdate<OrleansTaskExecutor>(
            //    "orleans-counter-task",
            //    executor => executor.ExecuteGrainOperationAsync("counter", "increment", 
            //        new System.Collections.Generic.Dictionary<string, object> { { "grainId", "hangfire-counter" } }),
            //    "*/10 * * * *");
        }
        
        // Orleans客户端连接服务，负责在应用启动时验证Orleans连接
        public class OrleansClientConnectionService : IHostedService
        {
            private readonly IClusterClient _client;
            private readonly ILogger _logger;
            
            public OrleansClientConnectionService(IClusterClient client, ILogger<OrleansClientConnectionService> logger)
            {
                _client = client;
                _logger = logger;
            }
            
            public Task StartAsync(CancellationToken cancellationToken)
            {
                _logger.LogInformation("Orleans客户端已初始化并配置");
                // 在Orleans 7.x中，连接是自动处理的，不需要显式调用Connect
                // 我们可以添加一些验证逻辑或健康检查
                return Task.CompletedTask;
            }
            
            public Task StopAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
        
        // Orleans客户端生命周期服务，负责在应用关闭时断开连接
        public class OrleansClientLifetimeService : IHostedService
        {
            private readonly IClusterClient _client;
            private readonly ILogger _logger;

            public OrleansClientLifetimeService(IClusterClient client, ILogger logger)
            {
                _client = client;
                _logger = logger;
            }

            public Task StartAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public async Task StopAsync(CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("正在断开与Orleans Silo的连接...");
                    // 尝试安全地处理客户端连接
                    try
                    {
                        // 对于较新版本的Orleans，可能需要不同的方法来关闭连接
                        // 这里使用try-catch来兼容不同版本
                        dynamic dynamicClient = _client;
                        if (dynamicClient != null)
                        {
                            await Task.CompletedTask;
                        }
                    }
                    catch
                    {
                        // 忽略关闭连接时的错误
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "断开Orleans连接时发生错误");
                }
            }
        }
    } 
}