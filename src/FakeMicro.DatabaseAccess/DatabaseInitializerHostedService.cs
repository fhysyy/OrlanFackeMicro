using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// 数据库初始化后台服务
/// 在应用启动时自动初始化所有数据库表
/// </summary>
public class DatabaseInitializerHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializerHostedService> _logger;

    public DatabaseInitializerHostedService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseInitializerHostedService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始数据库初始化...");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            
            // 获取所有需要初始化的实体类型
            var entityTypes = new[]
            {
                typeof(FakeMicro.Entities.User),
                typeof(FakeMicro.Entities.Role),
                typeof(FakeMicro.Entities.UserRole),
                typeof(FakeMicro.Entities.Subject),
                typeof(FakeMicro.Entities.Message),
                typeof(FakeMicro.Entities.DictionaryType),
                typeof(FakeMicro.Entities.DictionaryItem),
               // typeof(FakeMicro.Entities.AuditLog),
              
            };

            // 创建数据库（如果不存在）
            db.DbMaintenance.CreateDatabase();
            
            // 初始化所有表
            db.CodeFirst.InitTables(entityTypes);
            
            _logger.LogInformation("数据库初始化完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库初始化失败");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}