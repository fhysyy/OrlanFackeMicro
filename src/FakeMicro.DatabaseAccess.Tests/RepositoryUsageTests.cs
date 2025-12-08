using FakeMicro.DatabaseAccess;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Entities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FakeMicro.DatabaseAccess.Tests;

public class RepositoryUsageTests
{
    private readonly ServiceProvider _serviceProvider;

    public RepositoryUsageTests()
    {
        // 配置依赖注入
        var services = new ServiceCollection();
        
        // 添加数据库服务，默认启用所有功能
        services.AddDatabaseServices(options =>
        {
            options.UsePostgreSql = true;
            options.UseMongoDb = true;
            options.RegisterDefaultRepositories = true;
            options.UseRepositoryFactory = true;
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void Business_Scenario_Should_Use_Correct_Repository_For_Entity()
    {
        // 模拟业务场景：用户管理服务
        using var scope = _serviceProvider.CreateScope();
        
        // 对于需要存储在PostgreSQL中的用户实体，使用ISqlRepository
        var userSqlRepository = scope.ServiceProvider.GetRequiredService<ISqlRepository<User, long>>();
        
        // 对于需要存储在MongoDB中的消息实体，使用IMongoRepository
        var messageMongoRepository = scope.ServiceProvider.GetRequiredService<IMongoRepository<Message, long>>();
        
        Assert.NotNull(userSqlRepository);
        Assert.NotNull(messageMongoRepository);
        
        // 验证获取的是正确类型的仓储
        Assert.IsType<SqlSugarRepository<User, long>>(userSqlRepository);
        Assert.IsType<MongoRepository<Message, long>>(messageMongoRepository);
    }

    [Fact]
    public void RepositoryFactory_Should_Provide_Type_Safe_Repository_Creation()
    {
        // 验证仓储工厂提供类型安全的仓储创建
        using var scope = _serviceProvider.CreateScope();
        var repositoryFactory = scope.ServiceProvider.GetRequiredService<IRepositoryFactory>();
        
        // 创建SQL仓储（PostgreSQL）
        var userSqlRepository = repositoryFactory.CreateSqlRepository<User, long>();
        var roleSqlRepository = repositoryFactory.CreateSqlRepository<Role, long>();
        
        // 创建MongoDB仓储
        var activityMongoRepository = repositoryFactory.CreateMongoRepository<Message, long>();
        var messageMongoRepository = repositoryFactory.CreateMongoRepository<Message, long>();
        
        Assert.NotNull(userSqlRepository);
        Assert.NotNull(roleSqlRepository);
        Assert.NotNull(activityMongoRepository);
        Assert.NotNull(messageMongoRepository);
        
        // 验证类型安全
        Assert.IsAssignableFrom<ISqlRepository<User, long>>(userSqlRepository);
        Assert.IsAssignableFrom<ISqlRepository<Role, long>>(roleSqlRepository);
        Assert.IsAssignableFrom<IMongoRepository<Act, long>>(activityMongoRepository);
        Assert.IsAssignableFrom<IMongoRepository<Message, long>>(messageMongoRepository);
    }

    [Fact]
    public void Should_Be_Able_To_Register_And_Resolve_Custom_Repositories()
    {
        // 配置带有自定义仓储的依赖注入
        var services = new ServiceCollection();
        
        services.AddDatabaseServices(options =>
        {
            options.UsePostgreSql = true;
            options.UseMongoDb = true;
            options.RegisterDefaultRepositories = false;
            options.UseRepositoryFactory = true;
        });
        
        // 注册自定义仓储
        services.AddScoped<UserRepository>();
        services.AddScoped<MongoActRepository>();
        
        var serviceProvider = services.BuildServiceProvider();
        
        using var scope = serviceProvider.CreateScope();
        
        // 验证可以解析自定义仓储
        var userRepository = scope.ServiceProvider.GetRequiredService<UserRepository>();
        var mongoActRepository = scope.ServiceProvider.GetRequiredService<MongoActRepository>();
        
        Assert.NotNull(userRepository);
        Assert.NotNull(mongoActRepository);
        
        // 验证自定义仓储继承自正确的基类
        Assert.IsAssignableFrom<SqlSugarRepository<User, long>>(userRepository);
        Assert.IsAssignableFrom<MongoRepository<Act, long>>(mongoActRepository);
    }
}
