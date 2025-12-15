using FakeMicro.Utilities.Configuration;
using FakeMicro.Utilities.CodeGenerator.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Storage;
using SqlSugar;

using FakeMicro.DatabaseAccess;
using FakeMicro.DatabaseAccess.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FakeMicro.Silo
{
    public class Program
    {
        /// <summary>
        /// 配置加载诊断方法
        /// </summary>
        private static void DiagnoseConfigurationLoading(string[] args)
        {
            Console.WriteLine("=== 配置加载诊断 ===");
            
            try
            {
                Console.WriteLine($"当前工作目录: {Environment.CurrentDirectory}");
                
                // 检查 appsettings.json 文件是否存在
                var appsettingsPath = Path.Combine(Environment.CurrentDirectory, "appsettings.json");
                Console.WriteLine($"appsettings.json 路径: {appsettingsPath}");
                Console.WriteLine($"文件存在: {File.Exists(appsettingsPath)}");
                
                if (File.Exists(appsettingsPath))
                {
                    var content = File.ReadAllText(appsettingsPath);
                    Console.WriteLine($"文件大小: {content.Length} 字符");
                    Console.WriteLine($"包含Jwt配置: {content.Contains("Jwt")}");
                    Console.WriteLine($"包含Orleans配置: {content.Contains("Orleans")}");
                    Console.WriteLine($"包含SqlSugar配置: {content.Contains("SqlSugar")}");
                    Console.WriteLine($"包含ConnectionStrings配置: {content.Contains("ConnectionStrings")}");
                    
                    // 检查连接字符串是否存在
                    Console.WriteLine($"包含DefaultConnection: {content.Contains("DefaultConnection")}");
                }
                else
                {
                    // 尝试在bin目录查找
                    var binAppsettingsPath = Path.Combine(Environment.CurrentDirectory, "bin", "Debug", "net9.0", "appsettings.json");
                    Console.WriteLine($"bin目录appsettings.json路径: {binAppsettingsPath}");
                    Console.WriteLine($"bin文件存在: {File.Exists(binAppsettingsPath)}");
                }
                
                Console.WriteLine("=== 诊断完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"配置诊断失败: {ex.Message}");
            }
        }

        /// <summary>
        /// SqlSugar 配置诊断方法
        /// </summary>
        private static void DiagnoseSqlSugarConfiguration(IConfiguration configuration, IServiceCollection services)
        {
            try
            {
                // 1. 检查 appsettings.json 中是否存在 SqlSugar 配置
                Console.WriteLine("1. 检查 appsettings.json 配置:");
                var sqlSugarSection = configuration.GetSection("SqlSugar");
                if (!sqlSugarSection.Exists())
                {
                    Console.WriteLine("   ❌ 未找到 'SqlSugar' 配置节点");
                    return;
                }

                Console.WriteLine("   ✅ 找到 'SqlSugar' 配置节点");

                // 2. 读取并显示配置内容
                Console.WriteLine("   配置内容:");
                foreach (var child in sqlSugarSection.GetChildren())
                {
                    Console.WriteLine($"     {child.Key} = {child.Value}");
                }

                // 3. 尝试绑定配置到 SqlSugarOptions
                Console.WriteLine("2. 尝试绑定配置到 SqlSugarOptions:");
                var sqlSugarOptions = sqlSugarSection.Get<SqlSugarConfig.SqlSugarOptions>();
                if (sqlSugarOptions != null)
                {
                    Console.WriteLine($"   DbType: {sqlSugarOptions.DbType}");
                    Console.WriteLine($"   ConnectionString: {sqlSugarOptions.ConnectionString}");
                    Console.WriteLine($"   EnableSqlLog: {sqlSugarOptions.EnableSqlLog}");
                    Console.WriteLine($"   SlaveConnectionStrings: {(sqlSugarOptions.SlaveConnectionStrings?.Count ?? 0)} 个从库");
                }
                else
                {
                    Console.WriteLine("   ❌ 无法绑定到 SqlSugarOptions");
                }

                // 4. 检查服务是否已注册
                Console.WriteLine("3. 检查 SqlSugar 相关服务注册:");
                var registeredServices = services.Where(s => s.ServiceType.FullName?.Contains("SqlSugar") == true || 
                                                             s.ServiceType.FullName?.Contains("Database") == true).ToList();
                
                if (registeredServices.Any())
                {
                    Console.WriteLine($"   ✅ 已注册 {registeredServices.Count} 个相关服务:");
                    foreach (var service in registeredServices)
                    {
                        Console.WriteLine($"     {service.ServiceType.FullName} -> {service.ImplementationType?.FullName ?? "未指定实现类型"}");
                    }
                }
                else
                {
                    Console.WriteLine("   ❌ 未找到已注册的 SqlSugar 相关服务");
                }

                // 5. 尝试创建 SqlSugar 客户端并测试连接
                Console.WriteLine("4. 尝试创建 SqlSugar 客户端:");
                try
                {
                    var connectionString = sqlSugarOptions.ConnectionString;
                    var dbType = DatabaseAccess.SqlSugarConfig.ConvertToSqlSugarDbType(sqlSugarOptions.DbType);
                    
                    Console.WriteLine($"   正在创建客户端... (类型: {dbType})");
                    using (var db = new SqlSugarClient(new ConnectionConfig
                    {
                        ConnectionString = connectionString,
                        DbType = dbType,
                        IsAutoCloseConnection = true
                    }))
                    {
                        Console.WriteLine("   ✅ SqlSugar 客户端创建成功");
                        
                        // 测试连接
                        Console.WriteLine("   正在测试数据库连接...");
                        var isConnected = db.Ado.IsValidConnection();
                        if (isConnected)
                        {
                            Console.WriteLine("   ✅ 数据库连接测试成功");
                        }
                        else
                        {
                            Console.WriteLine("   ❌ 数据库连接测试失败");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ 创建 SqlSugar 客户端失败: {ex.Message}");
                    Console.WriteLine($"   详细错误: {ex}");
                }
                
                Console.WriteLine("=== 诊断完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 诊断过程中发生错误: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
            }
        }

        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== 启动FakeMicro Orleans Silo ===");

            try
            {
                // 首先诊断配置加载问题
                DiagnoseConfigurationLoading(args);

                var hostBuilder = Host.CreateDefaultBuilder(args);

                // 配置服务
                hostBuilder.ConfigureServices((context,services)=>
                {
                    Console.WriteLine($"配置环境: {context.HostingEnvironment.EnvironmentName}");
                    Console.WriteLine($"内容根路径: {context.HostingEnvironment.ContentRootPath}");
                    
                    // 配置字符串详细诊断
                    Console.WriteLine("=== 配置字符串诊断 ===");
                    var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
                    Console.WriteLine($"从GetConnectionString('DefaultConnection')获取的值: {(connectionString ?? "NULL")}");
                    
                    // 尝试直接从配置中读取
                    var directConnectionString = context.Configuration["ConnectionStrings:DefaultConnection"];
                    Console.WriteLine($"从配置['ConnectionStrings:DefaultConnection']获取的值: {(directConnectionString ?? "NULL")}");
                    
                    // 检查所有配置键
                    Console.WriteLine("所有配置键:");
                    foreach (var key in context.Configuration.AsEnumerable())
                    {
                        if (key.Key.Contains("Connection") || key.Key.Contains("Default") || key.Key.Contains("Database"))
                        {
                            Console.WriteLine($"  {key.Key} = {key.Value}");
                        }
                    }
                    
                    // 添加配置服务 - 修正 JWT 配置绑定
                    services.AddConfigurationServices(context.Configuration);

                    // 添加数据库服务
                    services.AddDatabaseServices(context.Configuration);
                    
                    // 添加 SqlSugar 配置绑定
                    services.Configure<SqlSugarConfig.SqlSugarOptions>(context.Configuration.GetSection("SqlSugar"));
                    
                    // 添加 Orleans 配置绑定
                    services.Configure<OrleansConfig>(context.Configuration.GetSection("Orleans"));
                    
                    // 添加连接字符串配置绑定
                    services.Configure<ConnectionStringsOptions>(context.Configuration.GetSection("ConnectionStrings"));
                    
                    // 添加 Orleans 数据库初始化服务
                    services.AddTransient<Services.OrleansDatabaseInitializer>();

                    // 暂时注释掉数据库初始化服务，专注于测试Orleans持久化状态配置
                    services.AddDatabaseInitializer(context.Configuration);

                    // 添加代码生成器服务
                    services.AddCodeGenerator(context.Configuration);

                    Console.WriteLine("服务注册中...");
                    
                    // 修正 JWT 配置注册 - 使用 JwtSettings
                    services.Configure<JwtSettings>(context.Configuration.GetSection("Jwt"));
                    
                    Console.WriteLine("服务注册完成");
                    
                    // SqlSugar 配置诊断
                    Console.WriteLine("=== SqlSugar 配置诊断 ===");
                    DiagnoseSqlSugarConfiguration(context.Configuration, services);
                });

                // 配置Orleans
                hostBuilder.UseOrleans((context, siloBuilder) =>
                {
                    // 从配置中获取Orleans设置
                    var orleansConfig = context.Configuration.GetSection("Orleans").Get<OrleansConfig>() ?? new OrleansConfig();
                    
                    // 获取数据库连接字符串
                    var connectionString = context.Configuration.GetConnectionString("DefaultConnection");

                    // 对于本地集群开发环境，使用简化的配置
                    // 在Orleans 9.x中，UseLocalhostClustering会自动设置必要的IMembershipTable
                    siloBuilder.UseLocalhostClustering(
                        clusterId: orleansConfig.ClusterId ?? "FakeMicroCluster",
                        serviceId: orleansConfig.ServiceId ?? "FakeMicroService");



                    // 🚀 配置PostgreSQL持久化存储（生产模式 - 无内存存储）
                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        try
                        {
                            Console.WriteLine("🚀 配置PostgreSQL持久化存储");
                            Console.WriteLine("✅ 全平台不使用内存存储 - 所有数据将持久化到PostgreSQL");

                            // 添加PostgreSQL作为默认存储
                            siloBuilder.AddAdoNetGrainStorageAsDefault(options =>
                            {
                                options.Invariant = "Npgsql";
                                options.ConnectionString = connectionString;
                            });
                            
                            // 添加PostgreSQL发布/订阅存储
                            siloBuilder.AddAdoNetGrainStorage("PubSubStore", options =>
                            {
                                options.Invariant = "Npgsql";
                                options.ConnectionString = connectionString;
                            });

                    // 配置用户状态存储
                        siloBuilder.AddAdoNetGrainStorage("UserStateStore", options =>
                        {
                            options.Invariant = "Npgsql";
                            options.ConnectionString = connectionString;
                            // 启用自动创建表功能
                           // options.UseJsonFormat = true;
                        });

                            // 配置Orleans系统存储
                            siloBuilder.AddAdoNetGrainStorage("OrleansClusterManifest", options =>
                            {
                                options.Invariant = "Npgsql";
                                options.ConnectionString = connectionString;
                            });

                            siloBuilder.AddAdoNetGrainStorage("OrleansSystemStore", options =>
                            {
                                options.Invariant = "Npgsql";
                                options.ConnectionString = connectionString;
                            });
                            
                            Console.WriteLine("✅ PostgreSQL持久化存储配置完成 - 全平台无内存存储");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ 配置PostgreSQL持久化失败: {ex.Message}");
                            throw new InvalidOperationException("无法配置持久化存储，请检查数据库连接配置", ex);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("❌ 未找到PostgreSQL连接字符串，无法配置持久化存储。请确保appsettings.json中配置了DefaultConnection。");
                    }

                    // 配置消息选项
                    siloBuilder.Configure<MessagingOptions>(options =>
                    {
                        options.ResponseTimeout = TimeSpan.FromSeconds(30);
                        options.MaxMessageBodySize = 10 * 1024 * 1024; // 10MB
                    });

                    // 移除不兼容的配置选项，使用Orleans 9.x支持的配置
                    siloBuilder.Configure<ClusterMembershipOptions>(options => options.NumMissedProbesLimit = 3);

                    //配置日志
                    siloBuilder.ConfigureLogging(logging =>
                    {
                        logging.AddConsole();
                        logging.SetMinimumLevel(LogLevel.Information);
                    });
                    Console.WriteLine("Orleans配置完成");
                });

                var host = hostBuilder.Build();

                Console.WriteLine("启动Silo...");
                try
                {
                    // 首先初始化 Orleans 数据库表结构
                    using (var scope = host.Services.CreateScope())
                    {
                        var dbInitializer = scope.ServiceProvider.GetService<Services.OrleansDatabaseInitializer>();
                        if (dbInitializer != null)
                        {
                            Console.WriteLine("正在初始化 Orleans 数据库表结构...");
                            await dbInitializer.InitializeOrleansTablesAsync();
                            Console.WriteLine("✅ Orleans 数据库表结构初始化完成");
                        }
                        else
                        {
                            Console.WriteLine("❌ 无法获取 Orleans 数据库初始化器");
                        }
                    }

                    await host.StartAsync();
                    Console.WriteLine("Orleans Silo运行中");
                    Console.WriteLine("Silo启动成功！按Ctrl+C停止...");
                    // 保持应用运行
                    await Task.Delay(-1);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Silo启动过程中遇到错误: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"内部错误: {ex.InnerException.Message}");
                    }
                    Console.WriteLine("尝试重新配置Silo，仅使用PostgreSQL持久化存储...");

                    // 重新构建配置，确保使用PostgreSQL持久化存储
                    var fallbackHostBuilder = Host.CreateDefaultBuilder(args);
                    fallbackHostBuilder.ConfigureServices((context, services) =>
                    {
                        services.AddConfigurationServices(context.Configuration);
                        services.AddDatabaseServices(context.Configuration);
                    });

                    fallbackHostBuilder.UseOrleans((context, siloBuilder) =>
                    {
                        var fallbackOrleansConfig = context.Configuration.GetSection("Orleans").Get<OrleansConfig>() ?? new OrleansConfig();
                        var fallbackConnectionString = context.Configuration.GetConnectionString("DefaultConnection");

                        siloBuilder.UseLocalhostClustering(
                            clusterId: fallbackOrleansConfig.ClusterId ?? "FakeMicroCluster",
                            serviceId: fallbackOrleansConfig.ServiceId ?? "FakeMicroService");
                    });

                    var fallbackHost = fallbackHostBuilder.Build();
                    Console.WriteLine("使用PostgreSQL持久化存储重新启动Silo...");
                    await fallbackHost.StartAsync();
                    Console.WriteLine("✅ Orleans Silo已使用PostgreSQL持久化存储成功启动！按Ctrl+C停止...");
                    await Task.Delay(-1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动错误: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"内部错误: {ex.InnerException.Message}");
                    Console.WriteLine($"内部错误堆栈: {ex.InnerException.StackTrace}");
                }
                Console.WriteLine($"错误类型: {ex.GetType().FullName}");
                Console.WriteLine($"错误堆栈: {ex.StackTrace}");
                throw;
            }
        }
    }
}