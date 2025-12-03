using FakeMicro.DatabaseAccess;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AuditLogTestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("审计日志功能测试");
            Console.WriteLine("=" * 50);
            
            // 配置依赖注入
            var services = new ServiceCollection();
            
            // 添加日志
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            
            // 读取配置
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
            
            // 添加数据库服务
            services.AddDatabaseServices(configuration);
            
            // 构建服务提供器
            var serviceProvider = services.BuildServiceProvider();
            
            // 获取审计日志仓库
            var auditLogRepository = serviceProvider.GetRequiredService<IAuditLogRepository>();
            
            try
            {
                // 测试添加审计日志
                Console.WriteLine("\n1. 测试添加审计日志...");
                
                var auditLog = new AuditLog
                {
                    user_id = 1,
                    username = "test_user",
                    action = "TestAction",
                    resource = "TestResource",
                    resource_id = "123",
                    Details = "测试审计日志记录功能",
                    ip_address = "127.0.0.1",
                    user_agent = "Test Client",
                    created_at = DateTime.UtcNow,
                    tenant_id = "1",
                    Result = "Success",
                    execution_time = 100
                };
                
                await auditLogRepository.AddAsync(auditLog);
                await auditLogRepository.SaveChangesAsync();
                
                Console.WriteLine("✓ 审计日志添加成功！");
                
                // 测试查询审计日志
                Console.WriteLine("\n2. 测试查询审计日志...");
                
                var logs = await auditLogRepository.GetAuditLogsAsync();
                Console.WriteLine($"✓ 查询到 {logs.Count} 条审计日志");
                
                if (logs.Count > 0)
                {
                    var latestLog = logs[0];
                    Console.WriteLine($"   最新日志: {latestLog.action} - {latestLog.resource} (用户: {latestLog.username})");
                }
                
                // 测试按用户查询审计日志
                Console.WriteLine("\n3. 测试按用户查询审计日志...");
                
                var userLogs = await auditLogRepository.GetAuditLogsByUserAsync(1);
                Console.WriteLine($"✓ 查询到用户ID为1的 {userLogs.Count} 条审计日志");
                
                // 测试获取审计日志统计信息
                Console.WriteLine("\n4. 测试获取审计日志统计信息...");
                
                var statistics = await auditLogRepository.GetAuditLogStatisticsAsync();
                Console.WriteLine($"✓ 总日志数: {statistics.TotalCount}");
                Console.WriteLine($"  唯一用户数: {statistics.UniqueUserCount}");
                Console.WriteLine($"  唯一操作数: {statistics.UniqueActionCount}");
                
                Console.WriteLine("\n" + "=" * 50);
                Console.WriteLine("所有测试完成！审计日志功能正常工作。");
                
            } catch (Exception ex)
            {
                Console.WriteLine($"\n✗ 测试失败: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}