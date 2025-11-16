using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace FakeMicro.Diagnostics
{
    public class ConfigurationDiagnostic
    {
        public static void DiagnoseConfigurationLoading(string[] args)
        {
            Console.WriteLine("=== 配置加载诊断工具 ===");
            
            try
            {
                // 1. 检查当前工作目录
                Console.WriteLine($"1. 当前工作目录: {Environment.CurrentDirectory}");
                
                // 2. 检查是否存在 appsettings.json 文件
                var appsettingsPath = Path.Combine(Environment.CurrentDirectory, "appsettings.json");
                Console.WriteLine($"2. 查找 appsettings.json 路径: {appsettingsPath}");
                Console.WriteLine($"   文件是否存在: {File.Exists(appsettingsPath)}");
                
                // 3. 检查是否存在 appsettings.{Environment}.json 文件
                var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
                var envAppsettingsPath = Path.Combine(Environment.CurrentDirectory, $"appsettings.{environmentName}.json");
                Console.WriteLine($"3. 环境配置文件路径: {envAppsettingsPath}");
                Console.WriteLine($"   文件是否存在: {File.Exists(envAppsettingsPath)}");
                
                // 4. 构建配置并测试
                Console.WriteLine("4. 构建主机配置...");
                var hostBuilder = Host.CreateDefaultBuilder(args);
                
                hostBuilder.ConfigureAppConfiguration((context, config) =>
                {
                    Console.WriteLine($"   主机环境: {context.HostingEnvironment.EnvironmentName}");
                    Console.WriteLine($"   主机内容根路径: {context.HostingEnvironment.ContentRootPath}");
                    Console.WriteLine($"   主机应用名称: {context.HostingEnvironment.ApplicationName}");
                    
                    // 添加自定义配置源
                    config.AddJsonFile("custom-config.json", optional: true, reloadOnChange: true);
                });
                
                var host = hostBuilder.Build();
                var configuration = host.Services.GetRequiredService<IConfiguration>();
                
                // 5. 测试配置读取
                Console.WriteLine("5. 测试配置读取:");
                
                // 测试基本配置
                var loggingSection = configuration.GetSection("Logging");
                Console.WriteLine($"   Logging配置存在: {loggingSection.Exists()}");
                
                // 测试连接字符串
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                Console.WriteLine($"   DefaultConnection: {connectionString ?? "未找到"}");
                
                // 测试SqlSugar配置
                var sqlSugarSection = configuration.GetSection("SqlSugar");
                Console.WriteLine($"   SqlSugar配置存在: {sqlSugarSection.Exists()}");
                if (sqlSugarSection.Exists())
                {
                    var sqlSugarConnection = sqlSugarSection["ConnectionString"];
                    Console.WriteLine($"   SqlSugar连接字符串: {sqlSugarConnection ?? "未找到"}");
                }
                
                // 测试Orleans配置
                var orleansSection = configuration.GetSection("Orleans");
                Console.WriteLine($"   Orleans配置存在: {orleansSection.Exists()}");
                if (orleansSection.Exists())
                {
                    var clusterId = orleansSection["ClusterId"];
                    Console.WriteLine($"   Orleans ClusterId: {clusterId ?? "未找到"}");
                }
                
                // 6. 列出所有配置键
                Console.WriteLine("6. 所有配置键:");
                var allKeys = configuration.GetChildren().Select(c => c.Key).ToList();
                foreach (var key in allKeys.OrderBy(k => k))
                {
                    Console.WriteLine($"   {key}");
                }
                
                Console.WriteLine("\n=== 诊断完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 诊断过程中发生错误: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
            }
        }
    }
}