using FakeMicro.DatabaseAccess;
using FakeMicro.DatabaseAccess.Extensions;
using FakeMicro.Grains.Extensions;
using FakeMicro.Utilities.CodeGenerator.DependencyInjection;
using FakeMicro.Utilities.Configuration;
using FakeMicro.Utilities.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Storage;
using FakeMicro.Silo.Services; // 修复命名空间引用
using SqlSugar;
using System;
using System.Configuration;
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
 
            
            try
            {
               
                
                // 检查 appsettings.json 文件是否存在
                var appsettingsPath = Path.Combine(Environment.CurrentDirectory, "appsettings.json");
            
                
                if (File.Exists(appsettingsPath))
                {
                    var content = File.ReadAllText(appsettingsPath);
                  
                }
                else
                {
                    // 尝试在bin目录查找
                    var binAppsettingsPath = Path.Combine(Environment.CurrentDirectory, "bin", "Debug", "net9.0", "appsettings.json");
                   
                }
               
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
            
                var sqlSugarSection = configuration.GetSection("SqlSugar");
                if (!sqlSugarSection.Exists())
                {
                   
                    return;
                }

               
         
                // 3. 尝试绑定配置到 SqlSugarOptions
              
                var sqlSugarOptions = sqlSugarSection.Get<SqlSugarConfig.SqlSugarOptions>();
           
                var registeredServices = services.Where(s => s.ServiceType.FullName?.Contains("SqlSugar") == true || 
                                                             s.ServiceType.FullName?.Contains("Database") == true).ToList();
                
           

          
            
                try
                {
                    var connectionString = sqlSugarOptions.ConnectionString;
                    var dbType = DatabaseAccess.SqlSugarConfig.ConvertToSqlSugarDbType(sqlSugarOptions.DbType);
                    
                  
                    using (var db = new SqlSugarClient(new ConnectionConfig
                    {
                        ConnectionString = connectionString,
                        DbType = dbType,
                        IsAutoCloseConnection = true
                    }))
                    {
                      
                        var isConnected = db.Ado.IsValidConnection();
                
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ 创建 SqlSugar 客户端失败: {ex.Message}");
                  
                }
                
              
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 诊断过程中发生错误: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
            }
        }

        public static async Task Main(string[] args)
        {
         

            try
            {
                // 首先诊断配置加载问题
                DiagnoseConfigurationLoading(args);

                var hostBuilder = Host.CreateDefaultBuilder(args);
        
                // 配置服务
                hostBuilder.ConfigureServices((context,services)=>
                {
                   
                    
                    // 使用集中式配置管理
                    var appSettings = context.Configuration.GetAppSettings();
                    
                    // 配置字符串详细诊断
                
                    var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
         
                    
                    // 尝试直接从配置中读取
                    var directConnectionString = context.Configuration["ConnectionStrings:DefaultConnection"];
                    
                    
                    // 检查所有配置键
                 
         
                    
                    // 添加配置服务 - 修正 JWT 配置绑定
                    services.AddConfigurationServices(context.Configuration);

                    // 添加数据库服务
                    services.AddDatabaseServices(context.Configuration);
                    
                    // 添加 SqlSugar 配置绑定
                    services.Configure<SqlSugarConfig.SqlSugarOptions>(context.Configuration.GetSection("SqlSugar"));
                    
                    // 添加连接字符串配置绑定
                    services.Configure<ConnectionStringsOptions>(context.Configuration.GetSection("ConnectionStrings"));
                    
                    // 添加 Orleans 数据库初始化服务
                    services.AddTransient<OrleansDatabaseInitializer>();

                    // 暂时注释掉数据库初始化服务，专注于测试Orleans持久化状态配置
                    services.AddDatabaseInitializer(context.Configuration);

                    // 添加代码生成器服务
                    services.AddCodeGenerator(context.Configuration);
                    
                    // 注册文件存储服务
                    services.AddFileStorage(context.Configuration);

                    // 添加Grain服务依赖注入
                    services.AddGrainServices();
                    // SqlSugar 配置诊断
                  
                    DiagnoseSqlSugarConfiguration(context.Configuration, services);
                });

                // 配置Orleans - 使用统一配置方式
                hostBuilder.ConfigureOrleansSilo();

                var host = hostBuilder.Build();

                
                try
                {
                    // 首先初始化 Orleans 数据库表结构
                    using (var scope = host.Services.CreateScope())
                    {
                        var dbInitializer = scope.ServiceProvider.GetService<OrleansDatabaseInitializer>();
                        if (dbInitializer != null)
                        {
                           
                            await dbInitializer.StartAsync(default);
                           
                        }
                        else
                        {
                            Console.WriteLine("❌ 无法获取 Orleans 数据库初始化器");
                        }
                    }

                    await host.StartAsync();
                
                    await Task.Delay(-1);
                }
                catch (Exception ex)
                {
                   
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"内部错误: {ex.InnerException.Message}");
                    }
                 

                    // 重新构建配置，确保使用PostgreSQL持久化存储
                    var fallbackHostBuilder = Host.CreateDefaultBuilder(args);
                    fallbackHostBuilder.ConfigureServices((context, services) =>
                    {
                        services.AddConfigurationServices(context.Configuration);
                        services.AddDatabaseServices(context.Configuration);
                    });

                    fallbackHostBuilder.UseOrleans((context, siloBuilder) =>
                    {
                        // 使用统一配置方式
                        var appSettings = context.Configuration.GetAppSettings();
                        var fallbackConnectionString = appSettings.Database.GetConnectionString();

                        // 配置Orleans
                        siloBuilder.UseLocalhostClustering(
                            clusterId: appSettings.Orleans.ClusterId ?? "FakeMicroCluster",
                            serviceId: appSettings.Orleans.ServiceId ?? "FakeMicroService");

                        // 配置持久化存储
                        if (!string.IsNullOrEmpty(fallbackConnectionString))
                        {
                            siloBuilder.AddAdoNetGrainStorage("Default", options =>
                            {
                                options.Invariant = "Npgsql";
                                options.ConnectionString = fallbackConnectionString;
                            });

                            // 暂时注释掉提醒服务配置，后续修复
                            // siloBuilder.UseAdoNetReminderService(options =>
                            // {
                            //     options.Invariant = "Npgsql";
                            //     options.ConnectionString = fallbackConnectionString;
                            // });

                            siloBuilder.UseAdoNetClustering(options =>
                            {
                                options.Invariant = "Npgsql";
                                options.ConnectionString = fallbackConnectionString;
                            });
                        }
                    });

                    var fallbackHost = fallbackHostBuilder.Build();
                  
                    await fallbackHost.StartAsync();
                
                    await Task.Delay(-1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动错误: {ex.Message}");
                throw;
            }
        }
    }
}