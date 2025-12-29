using FakeMicro.Api.Extensions;
using FakeMicro.Api.Security;
using FakeMicro.Api.Services;
using FakeMicro.DatabaseAccess;
using FakeMicro.Utilities.Storage;
using FakeMicro.Utilities.Configuration;
using FakeMicro.Interfaces;
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
using Npgsql;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System;
using System.IO;
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
            
            // 使用集中式配置管理 - 加载所有配置源
            builder.Configuration.AddDefaultConfiguration();
            
            // 使用集中式配置管理
            var appSettings = builder.Configuration.GetAppSettings();
            
            // 修复Orleans数据库表的大小写敏感性问题
           // await FixOrleansDatabaseTables(builder.Configuration);
            
            // 配置Orleans Silo
            var configuration = builder.Configuration;
          
            var siloPort = int.TryParse(Environment.GetEnvironmentVariable("ORLEANS_SILO_PORT"), out var sp) ? sp : 11111;
            var gatewayPort = int.TryParse(Environment.GetEnvironmentVariable("ORLEANS_GATEWAY_PORT"), out var gp) ? gp : 30000;
            
            // 配置Orleans Silo - 同时启动Silo和Client
            builder.Host.ConfigureOrleansSilo();
            
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
            
            // 添加幂等性服务
            builder.Services.AddIdempotency();

            // 配置HTTPS
            if (!builder.Environment.IsDevelopment())
            {
                builder.Services.AddHsts(options =>
                {
                    options.MaxAge = TimeSpan.FromDays(30);
                    options.IncludeSubDomains = true;
                    options.Preload = true;
                });
            }
            
            // 添加CORS配置 - 使用正确的方法
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            // 构建应用，添加异常处理以获取详细错误信息
            var app = builder.Build();
            
            // 配置HTTPS重定向
            app.UseHttpsRedirection();
            
            // 在生产环境中使用HSTS
            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }
            
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
        }
        
        // Orleans客户端生命周期服务，负责Orleans客户端的初始化和关闭
        public class OrleansClientLifecycleService : IHostedService
        {
            private readonly IClusterClient _client;
            private readonly ILogger _logger;

            public OrleansClientLifecycleService(IClusterClient client, ILogger<OrleansClientLifecycleService> logger)
            {
                _client = client;
                _logger = logger;
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                _logger.LogInformation("正在初始化Orleans客户端连接...");
                try
                {
                    // 在Orleans 7.x中，连接是自动处理的，但我们可以添加验证逻辑
                    // 尝试调用一个简单的grain来验证连接
                    var helloGrain = _client.GetGrain<IHelloGrain>("system");
                     await helloGrain.SayHelloAsync("Connection Test");
                    _logger.LogInformation("Orleans客户端连接验证成功");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Orleans客户端连接验证失败，但应用将继续运行");
                }
            }

            public async Task StopAsync(CancellationToken cancellationToken)
            {
                try
                {
                    _logger.LogInformation("正在断开与Orleans Silo的连接...");
                    // 尝试安全地处理客户端连接
                    try
                    {
                        // 对于较新版本的Orleans，使用DisposeAsync方法关闭连接
                        if (_client is IAsyncDisposable asyncDisposable)
                        {
                            await asyncDisposable.DisposeAsync();
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