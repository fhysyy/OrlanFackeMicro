using FakeMicro.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using System;
using System.IO;

namespace FakeMicro.Api
{
    /// <summary>
    /// API主机类，提供更完善的配置和服务管理
    /// </summary>
    public class OrleansApiHost
    {
        public static async Task RunAsync(string[] args)
        {
            try
            {
                // 使用基本方式创建配置
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
                    .AddCommandLine(args)
                    .Build();
                
                // 配置日志记录
                var loggerFactory = LoggerFactory.Create(loggingBuilder =>
                {
                    loggingBuilder.AddConsole();
                    loggingBuilder.AddDebug();
                });
                
                // 创建服务集合
                var services = new ServiceCollection();
                
                // 添加配置和日志服务
                services.AddSingleton<IConfiguration>(configuration);
                services.AddSingleton<ILoggerFactory>(loggerFactory);
                
                // 配置应用程序的服务
                await ConfigureServices(services, configuration, loggerFactory);
                
                // 构建服务提供者
                var serviceProvider = services.BuildServiceProvider();
                
                // 显示启动信息
                var logger = loggerFactory.CreateLogger<OrleansApiHost>();
                logger.LogInformation("=== 启动 API 服务器 ===");
                
                // 启动API服务
                var server = serviceProvider.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHost>();
                await server.RunAsync();
            }
            catch (Exception ex)
            {
                var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<OrleansApiHost>();
                logger.LogError(ex, "API启动失败");
                throw;
            }
        }

        private static async Task ConfigureServices(IServiceCollection services, IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            // 添加Swagger
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() { Title = "FakeMicro API", Version = "v1" });
            });

            // 添加控制器
            services.AddControllers();

            // 配置JWT认证
            //ConfigureJwtAuthentication(services, configuration);

            // 配置Orleans客户端
            var clientBuilder = new Orleans.ClientBuilder()
                .UseLocalhostClustering(gatewayPort: 30000, siloPort: 11111) // 配置Silo端口
                .ConfigureLogging(logging => 
                {
                    logging.AddConsole();
                    logging.AddDebug();
                })
                .ConfigureServices(services => 
                {
                    services.AddSingleton(typeof(ILoggerFactory), loggerFactory);
                });
            
            // 构建并连接客户端
            var client = clientBuilder.Build();
            
            // 注册客户端创建和连接的任务
            services.AddSingleton<IClusterClient>(async sp => 
            {
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<OrleansApiHost>();
                try
                {
                    logger.LogInformation("开始连接Orleans Silo...");
                    await client.Connect(retryFilter: async error =>
                    {
                        logger.LogWarning("连接Orleans Silo失败: {Error}. 将在5秒后重试...", error.Message);
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        return true;
                    });
                    logger.LogInformation("成功连接到Orleans Silo");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "无法连接到Orleans Silo");
                    // 如果连接失败，仍然返回客户端实例，但在使用时会出错
                    // 在实际应用中，可能需要更好的错误处理策略
                }
                return client;
            });
            
            // 确保应用关闭时优雅地断开连接
            services.AddSingleton<IHostedService>(new OrleansClientLifetimeService(client));
            
            // 添加健康检查
            services.AddHealthChecks();

            // 添加CORS支持
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });
            
            // 添加WebHost
            services.AddWebHostDefaults(webBuilder =>
            {
                webBuilder.Configure(app =>
                {
                    ConfigurePipeline(app, configuration, loggerFactory);
                });
            });

            await Task.CompletedTask;
        }

        private static void ConfigurePipeline(IApplicationBuilder app, IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            var webApp = app as WebApplication;
            if (webApp == null)
            {
                // 如果不是WebApplication类型，获取环境信息的替代方法
                var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
                
                // 启用Swagger UI
                if (env.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }
                
                // 启用CORS
                app.UseCors("AllowAll");
                
                // 暂时注释认证相关中间件，简化启动流程
                // app.UseAuthentication();
                // app.UseAuthorization();
                
                // 添加健康检查端点
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapHealthChecks("/health");
                    endpoints.MapGet("/api/test", async context =>
                    {
                        var response = new { Message = "API is working", Timestamp = DateTime.UtcNow };
                        await context.Response.WriteAsJsonAsync(response);
                    });
                    endpoints.MapControllers();
                });
            }
            else
            {
                // 原始WebApplication逻辑
                // 启用Swagger UI
                if (webApp.Environment.IsDevelopment())
                {
                    webApp.UseSwagger();
                    webApp.UseSwaggerUI();
                }

                // 启用CORS
                webApp.UseCors("AllowAll");

                // 暂时注释认证相关中间件，简化启动流程
                // webApp.UseAuthentication();
                // webApp.UseAuthorization();

                // 添加健康检查端点
                webApp.MapHealthChecks("/health");

                // 添加基本测试端点
                webApp.MapGet("/api/test", () => new { Message = "API is working", Timestamp = DateTime.UtcNow });

                // 配置控制器路由
                webApp.MapControllers();
            }
        }

        /// <summary>
        /// Orleans客户端生命周期服务，负责在应用关闭时断开连接
        /// </summary>
        private class OrleansClientLifetimeService : IHostedService
        {
            private readonly IClusterClient _client;
            private readonly ILogger _logger;

            public OrleansClientLifetimeService(IClusterClient client)
            {
                _client = client;
                _logger = new LoggerFactory().CreateLogger<OrleansClientLifetimeService>();
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
                    await _client.Close();
                    _logger.LogInformation("已成功断开与Orleans Silo的连接");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "断开Orleans Silo连接时发生错误");
                }
            }
        }

        private static void ConfigureJwtAuthentication(IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = new JwtSettings();
            configuration.GetSection("JwtSettings").Bind(jwtSettings);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer ?? "FakeMicro",
                    ValidAudience = jwtSettings.Audience ?? "FakeMicro-Users",
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(jwtSettings.SecretKey ?? "your-super-secret-key-at-least-32-characters-long"))
                };
            });
        }
    }

    // JWT设置类
    public class JwtSettings
    {
        public string SecretKey { get; set; } = "";
        public string Issuer { get; set; } = "";
        public string Audience { get; set; } = "";
        public int ExpireMinutes { get; set; } = 60;
    }
}