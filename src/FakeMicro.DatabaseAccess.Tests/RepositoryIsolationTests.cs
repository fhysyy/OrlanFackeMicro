using FakeMicro.DatabaseAccess;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Entities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FakeMicro.DatabaseAccess.Tests;

public class RepositoryIsolationTests
{
    private readonly ServiceProvider _serviceProvider;

    public RepositoryIsolationTests()
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
    public void Should_Get_SqlRepository_For_PostgreSql()
    {
        // 验证可以获取ISqlRepository接口的实例
        using var scope = _serviceProvider.CreateScope();
        var sqlRepository = scope.ServiceProvider.GetRequiredService<ISqlRepository<User, long>>();
        
        Assert.NotNull(sqlRepository);
        Assert.IsAssignableFrom<SqlSugarRepository<User, long>>(sqlRepository);
    }

    [Fact]
    public void Should_Get_MongoRepository_For_MongoDb()
    {
        // 验证可以获取IMongoRepository接口的实例
        using var scope = _serviceProvider.CreateScope();
        var mongoRepository = scope.ServiceProvider.GetRequiredService<IMongoRepository<User, long>>();
        
        Assert.NotNull(mongoRepository);
        Assert.IsAssignableFrom<MongoRepository<User, long>>(mongoRepository);
    }

    [Fact]
    public void Should_Not_Be_Able_To_Convert_SqlRepository_To_MongoRepository()
    {
        // 验证两种仓储不能互换使用
        using var scope = _serviceProvider.CreateScope();
        var sqlRepository = scope.ServiceProvider.GetRequiredService<ISqlRepository<User, long>>();
        
        // 尝试将SqlRepository转换为MongoRepository应该失败
        var mongoRepository = sqlRepository as IMongoRepository<User, long>;
        
        Assert.Null(mongoRepository);
    }

    [Fact]
    public void Should_Not_Be_Able_To_Convert_MongoRepository_To_SqlRepository()
    {
        // 验证两种仓储不能互换使用
        using var scope = _serviceProvider.CreateScope();
        var mongoRepository = scope.ServiceProvider.GetRequiredService<IMongoRepository<User, long>>();
        
        // 尝试将MongoRepository转换为SqlRepository应该失败
        var sqlRepository = mongoRepository as ISqlRepository<User, long>;
        
        Assert.Null(sqlRepository);
    }

    [Fact]
    public void RepositoryFactory_Should_Create_SqlRepository()
    {
        // 验证仓储工厂可以创建SQL仓储
        using var scope = _serviceProvider.CreateScope();
        var repositoryFactory = scope.ServiceProvider.GetRequiredService<IRepositoryFactory>();
        
        var sqlRepository = repositoryFactory.CreateSqlRepository<User, long>();
        
        Assert.NotNull(sqlRepository);
        Assert.IsAssignableFrom<ISqlRepository<User, long>>(sqlRepository);
    }

    [Fact]
    public void RepositoryFactory_Should_Create_MongoRepository()
    {
        // 验证仓储工厂可以创建MongoDB仓储
        using var scope = _serviceProvider.CreateScope();
        var repositoryFactory = scope.ServiceProvider.GetRequiredService<IRepositoryFactory>();
        
        var mongoRepository = repositoryFactory.CreateMongoRepository<User, long>();
        
        Assert.NotNull(mongoRepository);
        Assert.IsAssignableFrom<IMongoRepository<User, long>>(mongoRepository);
    }

    [Fact]
    public void RepositoryFactory_Created_SqlRepository_Should_Not_Be_MongoRepository()
    {
        // 验证工厂创建的SQL仓储不是MongoDB仓储
        using var scope = _serviceProvider.CreateScope();
        var repositoryFactory = scope.ServiceProvider.GetRequiredService<IRepositoryFactory>();
        
        var sqlRepository = repositoryFactory.CreateSqlRepository<User, long>();
        var mongoRepository = sqlRepository as IMongoRepository<User, long>;
        
        Assert.Null(mongoRepository);
    }

    [Fact]
    public void RepositoryFactory_Created_MongoRepository_Should_Not_Be_SqlRepository()
    {
        // 验证工厂创建的MongoDB仓储不是SQL仓储
        using var scope = _serviceProvider.CreateScope();
        var repositoryFactory = scope.ServiceProvider.GetRequiredService<IRepositoryFactory>();
        
        var mongoRepository = repositoryFactory.CreateMongoRepository<User, long>();
        var sqlRepository = mongoRepository as ISqlRepository<User, long>;
        
        Assert.Null(sqlRepository);
    }

    [Fact]
    public void Should_Be_Able_To_Use_Both_Repositories_In_The_Same_Scope()
    {
        // 验证在同一个作用域中可以同时使用两种仓储
        using var scope = _serviceProvider.CreateScope();
        
        var sqlRepository = scope.ServiceProvider.GetRequiredService<ISqlRepository<User, long>>();
        var mongoRepository = scope.ServiceProvider.GetRequiredService<IMongoRepository<User, long>>();
        
        Assert.NotNull(sqlRepository);
        Assert.NotNull(mongoRepository);
        
        // 验证它们是不同的实例
        Assert.NotSame(sqlRepository, mongoRepository);
    }
}
