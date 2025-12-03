using FakeMicro.DatabaseAccess;using FakeMicro.DatabaseAccess.Interfaces;using FakeMicro.Entities;using Microsoft.Extensions.Configuration;using Microsoft.Extensions.DependencyInjection;using Microsoft.Extensions.Logging;using System;using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // 配置依赖注入
        var services = new ServiceCollection();
        
        // 添加日志
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
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
        
        Console.WriteLine("测试审计日志功能...");
        
        try
        {
            // 创建测试审计日志
            var auditLog = new AuditLog
            {
                user_id = 1,
                username = "test_user",
                action = "TestAction",
                resource = "TestResource",
                resource_id = "123",
                Details = "测试审计日志",
                ip_address = "127.0.0.1",
                user_agent = "Test Agent",
                created_at = DateTime.UtcNow,
                tenant_id = 1,
                Result = "Success",
                execution_time = 100
            };
            
            // 添加审计日志
            await auditLogRepository.AddAsync(auditLog);
            await auditLogRepository.SaveChangesAsync();
            
            Console.WriteLine("审计日志添加成功！");
            
            // 查询审计日志
            var logs = await auditLogRepository.GetAuditLogsAsync();
            Console.WriteLine($"查询到 {logs.Count} 条审计日志");
            
            foreach (var log in logs)
            {
                Console.WriteLine($"ID: {log.id}, 用户: {log.username}, 操作: {log.action}, 时间: {log.created_at}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}