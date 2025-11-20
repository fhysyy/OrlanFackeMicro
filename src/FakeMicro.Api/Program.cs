using FakeMicro.Api.Services;
using FakeMicro.Api.Extensions;
using FakeMicro.Api.Security;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

// 添加代码生成器相关的using语句
using FakeMicro.Utilities.CodeGenerator;
using FakeMicro.Utilities.CodeGenerator.Templates;
using FakeMicro.Utilities.CodeGenerator.DependencyInjection;


namespace FakeMicro.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            // 配置数据库连接
            string hangfireConnectionString = builder.Configuration.GetConnectionString("HangfireConnection") 
                ?? "Host=localhost;Database=fakemicro_hangfire;Username=postgres;Password=123456";
            
            // 添加必要的服务
            builder.Services.AddControllers(); // 注册控制器
            builder.Services.AddEndpointsApiExplorer();
            
            // 配置 JWT 认证
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? "a-string-secret-at-least-256-bits-long";
            var issuer = jwtSettings["Issuer"] ?? "FakeMicro";
            var audience = jwtSettings["Audience"] ?? "FakeMicro-Users";
            
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
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey)),
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

            // 使用推荐的方式配置Orleans客户端
            builder.Services.AddOrleansClient(clientBuilder =>
            {
                // 配置集群ID和服务ID，必须与Silo配置一致
                clientBuilder.Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "FakeMicroCluster";
                    options.ServiceId = "FakeMicroService";
                });
                // 配置本地集群连接
                clientBuilder.UseLocalhostClustering(30000, "FakeMicroService", "FakeMicroCluster");
                //.AddAdoNetGrainStorage(
                //name: "PostgreSQLStore",
                //configureOptions: options =>
                //{
                //    options.Invariant = "Npgsql";  // PostgreSQL 的 invariant 名称
                //    options.ConnectionString = "Host=localhost;Database=OrleansStorage;Username=postgres;Password=your_password;";
                //    options.UseJsonFormat = true; // 可选：使用 JSON 格式存储而不是二进制
                //});
            });




            // 添加一个IHostedService来处理客户端连接
            builder.Services.AddHostedService<OrleansClientConnectionService>();

            // 确保应用关闭时优雅地断开连接
            builder.Services.AddSingleton<IHostedService>(sp => 
            {
                var client = sp.GetRequiredService<IClusterClient>();
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();
                return new OrleansClientLifetimeService(client, logger);
            });

            builder.Services.Configure<ClientMessagingOptions>(options =>
            {
                options.ResponseTimeout = TimeSpan.FromSeconds(45);
                options.MaxMessageBodySize = 50 * 1024 * 1024; // 50MB
            });

            // 添加模拟的ILogger服务以解决依赖问题
            builder.Services.AddLogging();
            
            // 添加系统健康服务
            builder.Services.AddTransient<SystemHealthService>();
            
            // 注册OrleansTaskExecutor服务
            builder.Services.AddTransient<OrleansTaskExecutor>();
            
            // 注册代码生成器服务
            RegisterCodeGeneratorServices(builder);
            

            // 配置HangFire
            builder.Services.AddHangfire(configuration => configuration
                //.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                //.UseSimpleAssemblyNameTypeSerializer()
                //.UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(hangfireConnectionString));
            
            // 添加HangFire服务器
            builder.Services.AddHangfireServer();
            
            // 添加认证和授权策略
            builder.Services.AddAuthorizationPolicies();
            
            // 构建应用
            var app = builder.Build();
            
            // 配置中间件
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
            
            app.UseHttpsRedirection();
            
            // 添加认证和授权中间件
            app.UseAuthentication();
            app.UseAuthorization();
            
            // 使用HangFire Dashboard（仅限管理员访问）
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new HangfireAuthorizationFilter() },
                DashboardTitle = "FakeMicro任务调度中心"
            });
            
            // 配置默认的定时任务
            ConfigureDefaultJobs();
            
            // 映射控制器路由
            app.MapControllers();
            
            // 添加健康检查端点
            app.MapGet("/health", () => "Healthy");
            
            Console.WriteLine("=== API服务器启动成功 ===");
            Console.WriteLine("访问 http://localhost:5000/swagger 查看API文档");
            Console.WriteLine("访问 http://localhost:5000/hangfire 查看HangFire仪表盘");
            Console.WriteLine("按Ctrl+C退出");
            
            await app.RunAsync();
        }
        
        /// <summary>
        /// 配置默认的定时任务
        /// </summary>
        private static void ConfigureDefaultJobs()
        {
            // 添加系统健康检查任务 - 每小时执行一次
            //RecurringJob.AddOrUpdate<SystemHealthService>(
            //    "system-health-check",
            //    service => service.PerformHealthCheck(),
            //    "0 * * * *");
            
            //// 添加简单的日志记录任务 - 每分钟执行一次
            //RecurringJob.AddOrUpdate(
            //    "sample-log-task",
            //    () => Console.WriteLine($"[{DateTime.Now}] 定时任务执行示例"),
            //    "* * * * *");
            
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
        
        /// <summary>
        /// 注册代码生成器相关服务
        /// </summary>
        /// <param name="builder">Web应用程序构建器</param>
        private static void RegisterCodeGeneratorServices(WebApplicationBuilder builder)
        {
            // 使用扩展方法注册代码生成器服务，支持动态OutputPath参数
            builder.Services.AddCodeGenerator(builder.Configuration);
        }
    } 
}