using FakeMicro.DatabaseAccess;
using FakeMicro.DatabaseAccess.Repositories;
using FakeMicro.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;

// 程序入口点
await MainAsync();

// 异步主方法
async Task MainAsync()
{
    // 配置依赖注入容器
    var serviceProvider = new ServiceCollection()
        .AddLogging(config =>
        {
            config.AddConsole();
            config.SetMinimumLevel(LogLevel.Information);
        })
        // 加载配置
        .AddSingleton<IConfiguration>(_ => new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build())
        // 添加数据库服务
        .AddDatabaseServices(new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build())
        .BuildServiceProvider();

    // 获取日志记录器
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("开始测试审计日志功能...");

    // 初始化数据库表结构
     using (var scope = serviceProvider.CreateScope())
     {
         var sqlSugarClient = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
         
         // 检查并创建审计日志表
         logger.LogInformation("开始初始化数据库表结构...");
         try
         {
             // 强制重新创建表，确保结构正确
             sqlSugarClient.DbMaintenance.DropTable("audit_logs");
             logger.LogInformation("已删除旧的audit_logs表");
             
             // 创建新表
             sqlSugarClient.CodeFirst.InitTables<AuditLog>();
             logger.LogInformation("已创建新的audit_logs表");
         }
         catch (Exception ex)
         {
             logger.LogError(ex, "初始化数据库表结构时发生错误");
             throw;
         }
     }

    try
    {
        // 获取审计日志仓储实例
        var auditLogRepository = serviceProvider.GetRequiredService<IAuditLogRepository>();
        
        logger.LogInformation("成功获取AuditLogRepository实例");
        
        // 创建一个测试审计日志
        var auditLog = new AuditLog
        {
            user_id = 1,
            username = "admin",
            action = "TEST_ACTION",
            resource = "test_resource",
            resource_id = "test_123",
            ip_address = "127.0.0.1",
            user_agent = "Test Agent",
            Details = "测试审计日志记录",
            Result = "success",
            tenant_id = "default", // 添加默认租户ID
            ErrorMessage = "", // 提供空字符串而不是null
            execution_time = 0 // 提供默认执行时间
        };
        
        logger.LogInformation("创建测试审计日志: {AuditLog}", auditLog);
        
        // 添加审计日志到数据库
        await auditLogRepository.AddAsync(auditLog);
        
        logger.LogInformation("成功添加审计日志记录，ID: {Id}", auditLog.id);
        
        // 测试获取审计日志
        var retrievedLog = await auditLogRepository.GetByIdAsync(auditLog.id);
        
        if (retrievedLog != null)
        {
            logger.LogInformation("成功获取审计日志记录: {Id} - {Action}", retrievedLog.id, retrievedLog.action);
        }
        else
        {
            logger.LogError("无法获取审计日志记录");
        }
        
        // 测试按用户ID获取审计日志
        var userLogs = await auditLogRepository.GetAuditLogsByUserAsync(1);
        
        logger.LogInformation("按用户ID获取的审计日志数量: {Count}", userLogs.Count());
        
        // 测试获取审计日志统计信息
        var statistics = await auditLogRepository.GetAuditLogStatisticsAsync();
        
        logger.LogInformation("审计日志统计信息: 总数={TotalCount}, 唯一用户数={UniqueUserCount}, 唯一操作数={UniqueActionCount}", statistics.TotalCount, statistics.UniqueUserCount, statistics.UniqueActionCount);
        
        logger.LogInformation("审计日志功能测试完成");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "审计日志功能测试失败");
    }
    finally
    {
        await serviceProvider.DisposeAsync();
    }
}
