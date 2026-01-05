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
using System.Linq;
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
          
            // 从appsettings.json中读取Orleans端口配置
            var siloPort = appSettings.Orleans.SiloPort;
            var gatewayPort = appSettings.Orleans.GatewayPort;
            
            // 配置Orleans Silo - 同时启动Silo和Client
            builder.Host.ConfigureOrleansSilo();
            
            // 添加必要的服务
            builder.Services.AddEndpointsApiExplorer();
            
            // 使用基于日期的密码认证保护 Hangfire 和 CAP Dashboard
            // 密码格式：yyyyMMdd（例如：20260105）
            // 认证方式：Basic Auth 或 URL 参数 ?password=yyyyMMdd

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
            
            // 添加CORS配置 - 使用配置文件中的设置
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowConfiguredOrigins", policy =>
                {
                    if (appSettings.Cors.AllowedOrigins != null && appSettings.Cors.AllowedOrigins.Any())
                    {
                        policy.WithOrigins(appSettings.Cors.AllowedOrigins.ToArray())
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    }
                    else
                    {
                        // 如果没有配置允许的源，默认拒绝所有跨域请求
                        policy.WithOrigins(Array.Empty<string>())
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    }
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
            
            // 首先配置Dashboard，确保它们不受其他中间件影响
            // 使用配置的Hangfire Dashboard路径
            if (appSettings.Hangfire.UseDashboard)
            {
                app.UseHangfireDashboard(appSettings.Hangfire.DashboardPath, new DashboardOptions
                {
                    Authorization = new[] { new FakeMicro.Api.Security.HangfireAuthorizationFilter() }
                });
            }

            // 添加 CAP Dashboard 认证中间件
            if (appSettings.CAP.UseDashboard)
            {
                app.UseMiddleware<Middleware.CapDashboardAuthMiddleware>(appSettings.CAP.DashboardPath);
            }

            // 启用CAP中间件（用于事件总线和CAP Dashboard）
            app.UseCap();
            
            // 配置所有其他中间件
            app.ConfigureAllMiddleware();
            
            app.MapControllers();
            app.MapGet("/health", () => "Healthy");
            // 暂时注释掉默认的定时任务
            // ConfigureDefaultJobs();

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
    }
}