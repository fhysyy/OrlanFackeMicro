using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeMicro.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Xunit;

namespace FakeMicro.DatabaseAccess.Tests;

/// <summary>
/// MongoDB仓储测试类
/// 验证MongoDB仓储的基本功能
/// </summary>
public class MongoRepositoryTests
{
    private readonly IServiceProvider _serviceProvider;

    public MongoRepositoryTests()
    {
        // 配置测试服务
        var services = new ServiceCollection();
        
        // 配置日志
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // 配置MongoDB
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>{
                { "MongoDB:ConnectionString", "mongodb://localhost:27017" },
                { "MongoDB:DatabaseName", "FakeMicroTest" },
                { "MongoDB:ConnectionTimeout", "30" },
                { "MongoDB:ConnectionPoolSize", "100" }
            })
            .Build();
        
        // 添加MongoDB服务
        services.AddMongoDB(configuration);
        
        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// 测试MongoDB连接
    /// </summary>
    [Fact]
    public async Task TestMongoDBConnection()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IMongoClient>();
        
        // Act
        var databases = await client.ListDatabaseNamesAsync();
        var databaseList = await databases.ToListAsync();
        
        // Assert
        Assert.NotNull(databases);
        Assert.True(databaseList.Count > 0);
    }

    /// <summary>
    /// 测试创建和获取MongoDB仓储
    /// </summary>
    [Fact]
    public void TestCreateMongoRepository()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IMongoRepositoryFactory>();
        
        // Act
        var repository = factory.CreateRepository<User, long>();
        
        // Assert
        Assert.NotNull(repository);
        Assert.IsType<MongoRepository<User, long>>(repository);
    }

    /// <summary>
    /// 测试MongoRepository的AddAsync和GetByIdAsync方法
    /// </summary>
    [Fact]
    public async Task TestAddAndGetByIdAsync()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMongoRepository<User, long>>();
        
        // 创建测试用户
        var user = new User
        {
            username = "testuser",
            display_name = "Test User",
            email = "test@example.com",
            phone = "1234567890",
            avatar = "",
            gender = "male",
            birthday = DateTime.Now.AddYears(-30),
            address = "Test Address",
            country = "China",
            province = "Guangdong",
            city = "Shenzhen",
            zip_code = "518000",
            website = "",
            bio = "Test bio",
            status = "active",
            last_login_at = DateTime.Now,
            email_verified_at = DateTime.Now,
            phone_verified_at = DateTime.Now,
            created_at = DateTime.Now,
            updated_at = DateTime.Now,
            created_by = 1,
            updated_by = 1,
            is_deleted = false
        };
        
        // Act
        await repository.AddAsync(user);
        
        // 由于MongoDB会自动生成ObjectId，这里需要先获取生成的id
        // 注意：需要确保User实体的id字段类型支持MongoDB的ObjectId
        // 目前User实体的id是long类型，可能需要调整为string或ObjectId
        // 这里我们使用username来查询，而不是id
        var result = await repository.GetByConditionAsync(u => u.username == "testuser");
        
        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Equal("testuser", result.First().username);
        
        // 清理测试数据
        await repository.DeleteByConditionAsync(u => u.username == "testuser");
    }

    /// <summary>
    /// 测试MongoRepository的DeleteByConditionAsync方法
    /// </summary>
    [Fact]
    public async Task TestDeleteByConditionAsync()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMongoRepository<User, long>>();
        
        // 创建测试用户
        var user = new User
        {
            username = "testuser_delete",
            display_name = "Test User Delete",
            email = "test_delete@example.com",
            phone = "1234567890",
            avatar = "",
            gender = "male",
            birthday = DateTime.Now.AddYears(-30),
            address = "Test Address",
            country = "China",
            province = "Guangdong",
            city = "Shenzhen",
            zip_code = "518000",
            website = "",
            bio = "Test bio",
            status = "active",
            last_login_at = DateTime.Now,
            email_verified_at = DateTime.Now,
            phone_verified_at = DateTime.Now,
            created_at = DateTime.Now,
            updated_at = DateTime.Now,
            created_by = 1,
            updated_by = 1,
            is_deleted = false
        };
        
        await repository.AddAsync(user);
        
        // Act
        var deleteCount = await repository.DeleteByConditionAsync(u => u.username == "testuser_delete");
        
        // Assert
        Assert.Equal(1, deleteCount);
        
        // 验证用户已被删除
        var result = await repository.GetByConditionAsync(u => u.username == "testuser_delete");
        Assert.Empty(result);
    }
}
