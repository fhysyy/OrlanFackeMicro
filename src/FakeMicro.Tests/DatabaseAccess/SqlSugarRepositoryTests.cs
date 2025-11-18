using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using FakeMicro.DatabaseAccess;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.DatabaseAccess.Exceptions;
using FakeMicro.Models;

/// <summary>
/// SqlSugar仓储层单元测试
/// 验证仓储功能的正确性和性能
/// </summary>
namespace FakeMicro.Tests.DatabaseAccess
{
    /// <summary>
    /// SqlSugar仓储基础测试
    /// </summary>
    public class SqlSugarRepositoryTests : IDisposable
    {
        private readonly Mock<SqlSugarScope> _mockDb;
        private readonly Mock<ILogger<SqlSugarRepository<TestEntity, Guid>>> _mockLogger;
        private readonly SqlSugarRepository<TestEntity, Guid> _repository;
        private readonly IServiceProvider _serviceProvider;

        public SqlSugarRepositoryTests()
        {
            // 初始化测试数据
            _mockDb = new Mock<SqlSugarScope>();
            _mockLogger = new Mock<ILogger<SqlSugarRepository<TestEntity, Guid>>>();
            
            // 配置依赖注入
            var services = new ServiceCollection();
            services.AddSingleton(_mockDb.Object);
            services.AddSingleton(_mockLogger.Object);
            
            _serviceProvider = services.BuildServiceProvider();
            _repository = new SqlSugarRepository<TestEntity, Guid>(_mockDb.Object, _mockLogger.Object);
        }

        public void Dispose()
        {
            // 清理资源
            _mockDb.Reset();
            _mockLogger.Reset();
        }

        /// <summary>
        /// 测试仓储构造函数参数验证
        /// </summary>
        [Fact]
        public void Constructor_WithNullDbContext_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new SqlSugarRepository<TestEntity, Guid>(null!, _mockLogger.Object));
        }

        /// <summary>
        /// 测试仓储构造函数参数验证
        /// </summary>
        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new SqlSugarRepository<TestEntity, Guid>(_mockDb.Object, null!));
        }

        /// <summary>
        /// 测试连接健康检查方法
        /// </summary>
        [Fact]
        public async Task CanAttemptConnectionRecovery_WithHealthyConnection_ReturnsTrue()
        {
            // Arrange
            _mockDb.Setup(db => db.Ado.IsValidConnection()).Returns(true);
            _mockDb.Setup(db => db.Ado.Connection.State).Returns(System.Data.ConnectionState.Open);

            // Act
            var result = await _repository.CanAttemptConnectionRecovery();

            // Assert
            Assert.True(result);
            _mockDb.Verify(db => db.Ado.IsValidConnection(), Times.Once);
        }

        /// <summary>
        /// 测试连接健康检查方法
        /// </summary>
        [Fact]
        public async Task CanAttemptConnectionRecovery_WithUnhealthyConnection_ReturnsFalse()
        {
            // Arrange
            _mockDb.Setup(db => db.Ado.IsValidConnection()).Returns(false);
            _mockDb.Setup(db => db.Ado.Connection.State).Returns(System.Data.ConnectionState.Closed);

            // Act
            var result = await _repository.CanAttemptConnectionRecovery();

            // Assert
            Assert.False(result);
            _mockDb.Verify(db => db.Ado.IsValidConnection(), Times.Once);
        }

        /// <summary>
        /// 测试GetByIdAsync方法成功场景
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsEntity()
        {
            // Arrange
            var testId = Guid.NewGuid();
            var testEntity = new TestEntity { Id = testId, Name = "Test Entity" };
            
            _mockDb.Setup(db => db.Queryable<TestEntity>()
                .InSingle(testId))
                .Returns(testEntity);

            // Act
            var result = await _repository.GetByIdAsync(testId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(testId, result.Id);
            Assert.Equal("Test Entity", result.Name);
        }

        /// <summary>
        /// 测试GetByIdAsync方法实体不存在场景
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
        {
            // Arrange
            var testId = Guid.NewGuid();
            _mockDb.Setup(db => db.Queryable<TestEntity>()
                .InSingle(testId))
                .Returns((TestEntity?)null);

            // Act
            var result = await _repository.GetByIdAsync(testId);

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// 测试AddAsync方法成功场景
        /// </summary>
        [Fact]
        public async Task AddAsync_WithValidEntity_ReturnsEntity()
        {
            // Arrange
            var testEntity = new TestEntity 
            { 
                Id = Guid.NewGuid(), 
                Name = "Test Entity",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockDb.Setup(db => db.Insertable(testEntity).ExecuteCommandAsync()).ReturnsAsync(1);
            _mockDb.Setup(db => db.Ado.BeginTran()).Returns(new Mock<IDbTransaction>().Object);

            // Act
            await _repository.AddAsync(testEntity);

            // Assert
            _mockDb.Verify(db => db.Insertable(testEntity).ExecuteCommandAsync(), Times.Once);
        }

        /// <summary>
        /// 测试UpdateAsync方法成功场景
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WithValidEntity_UpdatesEntity()
        {
            // Arrange
            var testEntity = new TestEntity 
            { 
                Id = Guid.NewGuid(), 
                Name = "Updated Entity",
                UpdatedAt = DateTime.UtcNow
            };

            _mockDb.Setup(db => db.Updateable(testEntity).ExecuteCommandAsync()).ReturnsAsync(1);
            _mockDb.Setup(db => db.Ado.BeginTran()).Returns(new Mock<IDbTransaction>().Object);

            // Act
            await _repository.UpdateAsync(testEntity);

            // Assert
            _mockDb.Verify(db => db.Updateable(testEntity).ExecuteCommandAsync(), Times.Once);
        }

        /// <summary>
        /// 测试DeleteAsync方法成功场景
        /// </summary>
        [Fact]
        public async Task DeleteAsync_WithValidEntity_DeletesEntity()
        {
            // Arrange
            var testEntity = new TestEntity 
            { 
                Id = Guid.NewGuid(), 
                Name = "Test Entity"
            };

            _mockDb.Setup(db => db.Deleteable(testEntity).ExecuteCommandAsync()).ReturnsAsync(1);
            _mockDb.Setup(db => db.Ado.BeginTran()).Returns(new Mock<IDbTransaction>().Object);

            // Act
            await _repository.DeleteAsync(testEntity);

            // Assert
            _mockDb.Verify(db => db.Deleteable(testEntity).ExecuteCommandAsync(), Times.Once);
        }

        /// <summary>
        /// 测试ExistsAsync方法成功场景
        /// </summary>
        [Fact]
        public async Task ExistsAsync_WithExistingEntity_ReturnsTrue()
        {
            // Arrange
            var testId = Guid.NewGuid();
            _mockDb.Setup(db => db.Queryable<TestEntity>()
                .Where(It.IsAny<Expression<Func<TestEntity, bool>>>())
                .Any())
                .Returns(true);

            // Act
            var result = await _repository.ExistsAsync(e => e.Id == testId);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试CountAsync方法成功场景
        /// </summary>
        [Fact]
        public async Task CountAsync_WithEntities_ReturnsCount()
        {
            // Arrange
            var expectedCount = 5;
            _mockDb.Setup(db => db.Queryable<TestEntity>()
                .Count())
                .Returns(expectedCount);

            // Act
            var result = await _repository.CountAsync();

            // Assert
            Assert.Equal(expectedCount, result);
        }

        /// <summary>
        /// 测试GetPagedAsync方法成功场景
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_WithValidParameters_ReturnsPagedResult()
        {
            // Arrange
            var testEntities = new List<TestEntity>
            {
                new TestEntity { Id = Guid.NewGuid(), Name = "Entity 1" },
                new TestEntity { Id = Guid.NewGuid(), Name = "Entity 2" }
            };

            _mockDb.Setup(db => db.Queryable<TestEntity>()
                .ToPageListAsync(1, 10, 0))
                .ReturnsAsync((testEntities, 2));

            // Act
            var result = await _repository.GetPagedAsync(1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
            Assert.Equal(1, result.PageIndex);
            Assert.Equal(10, result.PageSize);
        }

        /// <summary>
        /// 测试SaveChangesAsync方法成功场景
        /// </summary>
        [Fact]
        public async Task SaveChangesAsync_WithPendingChanges_SavesChanges()
        {
            // Arrange
            _mockDb.Setup(db => db.Ado.CommitTran()).Returns(true);
            _mockDb.Setup(db => db.Ado.BeginTran()).Returns(new Mock<IDbTransaction>().Object);

            // Act
            await _repository.SaveChangesAsync();

            // Assert
            _mockDb.Verify(db => db.Ado.CommitTran(), Times.Once);
        }

        /// <summary>
        /// 测试异常处理
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_WithSqlSugarException_ThrowsDataAccessException()
        {
            // Arrange
            var testId = Guid.NewGuid();
            _mockDb.Setup(db => db.Queryable<TestEntity>()
                .InSingle(testId))
                .Throws(new SqlSugarException("数据库错误", new Exception("底层错误")));

            // Act & Assert
            await Assert.ThrowsAsync<DataAccessException>(async () => 
                await _repository.GetByIdAsync(testId));
        }

        /// <summary>
        /// 测试性能监控
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_LogsPerformanceMetrics()
        {
            // Arrange
            var testId = Guid.NewGuid();
            var testEntity = new TestEntity { Id = testId, Name = "Test Entity" };
            
            _mockDb.Setup(db => db.Queryable<TestEntity>()
                .InSingle(testId))
                .Returns(testEntity);

            // Act
            await _repository.GetByIdAsync(testId);

            // Assert
            _mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("耗时")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }

    /// <summary>
    /// 测试实体类
    /// </summary>
    public class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    /// <summary>
    /// 仓储性能测试
    /// </summary>
    public class SqlSugarRepositoryPerformanceTests : IDisposable
    {
        private readonly Mock<SqlSugarScope> _mockDb;
        private readonly Mock<ILogger<SqlSugarRepository<TestEntity, Guid>>> _mockLogger;
        private readonly SqlSugarRepository<TestEntity, Guid> _repository;

        public SqlSugarRepositoryPerformanceTests()
        {
            _mockDb = new Mock<SqlSugarScope>();
            _mockLogger = new Mock<ILogger<SqlSugarRepository<TestEntity, Guid>>>();
            _repository = new SqlSugarRepository<TestEntity, Guid>(_mockDb.Object, _mockLogger.Object);
        }

        public void Dispose()
        {
            _mockDb.Reset();
            _mockLogger.Reset();
        }

        /// <summary>
        /// 测试批量插入性能
        /// </summary>
        [Fact]
        public async Task AddRangeAsync_WithLargeBatch_ExecutesEfficiently()
        {
            // Arrange
            var entities = Enumerable.Range(1, 1000)
                .Select(i => new TestEntity 
                { 
                    Id = Guid.NewGuid(), 
                    Name = $"Entity {i}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                })
                .ToList();

            _mockDb.Setup(db => db.Insertable(entities).ExecuteCommandAsync()).ReturnsAsync(1000);
            _mockDb.Setup(db => db.Ado.BeginTran()).Returns(new Mock<IDbTransaction>().Object);

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await _repository.AddRangeAsync(entities);
            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 1000); // 应该在1秒内完成
            _mockDb.Verify(db => db.Insertable(entities).ExecuteCommandAsync(), Times.Once);
        }

        /// <summary>
        /// 测试分页查询性能
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_WithLargeDataSet_ExecutesEfficiently()
        {
            // Arrange
            var totalCount = 10000;
            var pageEntities = Enumerable.Range(1, 50)
                .Select(i => new TestEntity 
                { 
                    Id = Guid.NewGuid(), 
                    Name = $"Entity {i}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                })
                .ToList();

            _mockDb.Setup(db => db.Queryable<TestEntity>()
                .ToPageListAsync(1, 50, totalCount))
                .ReturnsAsync((pageEntities, totalCount));

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await _repository.GetPagedAsync(1, 50);
            stopwatch.Stop();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(totalCount, result.TotalCount);
            Assert.True(stopwatch.ElapsedMilliseconds < 500); // 应该在500毫秒内完成
        }

        /// <summary>
        /// 测试条件查询性能
        /// </summary>
        [Fact]
        public async Task GetByConditionAsync_WithComplexCondition_ExecutesEfficiently()
        {
            // Arrange
            var testEntities = Enumerable.Range(1, 100)
                .Select(i => new TestEntity 
                { 
                    Id = Guid.NewGuid(), 
                    Name = $"Entity {i}",
                    CreatedAt = DateTime.UtcNow.AddDays(-i),
                    UpdatedAt = DateTime.UtcNow
                })
                .ToList();

            _mockDb.Setup(db => db.Queryable<TestEntity>()
                .Where(It.IsAny<Expression<Func<TestEntity, bool>>>())
                .ToListAsync())
                .ReturnsAsync(testEntities.Where(e => e.CreatedAt > DateTime.UtcNow.AddDays(-50)).ToList());

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await _repository.GetByConditionAsync(e => e.CreatedAt > DateTime.UtcNow.AddDays(-50));
            stopwatch.Stop();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count() <= 50);
            Assert.True(stopwatch.ElapsedMilliseconds < 300); // 应该在300毫秒内完成
        }

        /// <summary>
        /// 测试重试机制
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_WithTransientError_RetriesSuccessfully()
        {
            // Arrange
            var testId = Guid.NewGuid();
            var testEntity = new TestEntity { Id = testId, Name = "Test Entity" };
            var retryCount = 0;

            _mockDb.Setup(db => db.Queryable<TestEntity>()
                .InSingle(testId))
                .Returns(() =>
                {
                    retryCount++;
                    if (retryCount < 3)
                    {
                        throw new SqlSugarException("临时错误", new Exception("连接超时"));
                    }
                    return testEntity;
                });

            // Act
            var result = await _repository.GetByIdAsync(testId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(testId, result.Id);
            Assert.True(retryCount >= 3);
        }

        /// <summary>
        /// 测试并发操作
        /// </summary>
        [Fact]
        public async Task MultipleOperations_ConcurrentAccess_HandlesCorrectly()
        {
            // Arrange
            var testIds = Enumerable.Range(1, 10).Select(_ => Guid.NewGuid()).ToList();
            var tasks = new List<Task<TestEntity?>>();

            _mockDb.Setup(db => db.Queryable<TestEntity>()
                .InSingle(It.IsAny<Guid>()))
                .Returns<Guid>(id => new TestEntity { Id = id, Name = $"Entity {id}" });

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            foreach (var id in testIds)
            {
                tasks.Add(_repository.GetByIdAsync(id));
            }

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            Assert.Equal(10, results.Length);
            Assert.All(results, result => Assert.NotNull(result));
            Assert.True(stopwatch.ElapsedMilliseconds < 1000); // 应该在1秒内完成
        }
    }

    /// <summary>
    /// 仓储集成测试
    /// </summary>
    public class SqlSugarRepositoryIntegrationTests : IDisposable
    {
        private readonly string _connectionString;
        private readonly SqlSugarScope _db;
        private readonly SqlSugarRepository<TestEntity, Guid> _repository;
        private readonly ILogger<SqlSugarRepository<TestEntity, Guid>> _logger;

        public SqlSugarRepositoryIntegrationTests()
        {
            _connectionString = "Data Source=(local);Initial Catalog=TestDB;Integrated Security=True;TrustServerCertificate=True";
            _db = new SqlSugarScope(new ConnectionConfig
            {
                ConnectionString = _connectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true
            });

            _logger = new Mock<ILogger<SqlSugarRepository<TestEntity, Guid>>>().Object;
            _repository = new SqlSugarRepository<TestEntity, Guid>(_db, _logger);

            // 初始化测试数据库
            InitializeTestDatabase();
        }

        public void Dispose()
        {
            // 清理测试数据
            CleanupTestDatabase();
            _db.Dispose();
        }

        private void InitializeTestDatabase()
        {
            // 创建测试表
            _db.DbMaintenance.CreateTable<TestEntity>();
        }

        private void CleanupTestDatabase()
        {
            // 删除测试表
            _db.DbMaintenance.DropTable<TestEntity>();
        }

        /// <summary>
        /// 测试完整的CRUD操作
        /// </summary>
        [Fact]
        public async Task CompleteCRUD_WithValidEntity_ExecutesSuccessfully()
        {
            // Arrange
            var testEntity = new TestEntity 
            { 
                Id = Guid.NewGuid(), 
                Name = "Integration Test Entity",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Act & Assert
            // 1. 插入
            await _repository.AddAsync(testEntity);
            await _repository.SaveChangesAsync();

            // 2. 查询
            var retrievedEntity = await _repository.GetByIdAsync(testEntity.Id);
            Assert.NotNull(retrievedEntity);
            Assert.Equal(testEntity.Name, retrievedEntity.Name);

            // 3. 更新
            retrievedEntity.Name = "Updated Integration Test Entity";
            retrievedEntity.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(retrievedEntity);
            await _repository.SaveChangesAsync();

            // 4. 验证更新
            var updatedEntity = await _repository.GetByIdAsync(testEntity.Id);
            Assert.Equal("Updated Integration Test Entity", updatedEntity.Name);

            // 5. 删除
            await _repository.DeleteAsync(updatedEntity);
            await _repository.SaveChangesAsync();

            // 6. 验证删除
            var deletedEntity = await _repository.GetByIdAsync(testEntity.Id);
            Assert.Null(deletedEntity);
        }

        /// <summary>
        /// 测试事务管理
        /// </summary>
        [Fact]
        public async Task TransactionOperations_WithMultipleEntities_HandlesCorrectly()
        {
            // Arrange
            var entities = Enumerable.Range(1, 5)
                .Select(i => new TestEntity 
                { 
                    Id = Guid.NewGuid(), 
                    Name = $"Transaction Entity {i}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                })
                .ToList();

            // Act
            await _db.Ado.BeginTranAsync();
            
            try
            {
                await _repository.AddRangeAsync(entities);
                await _repository.SaveChangesAsync();
                
                // 模拟业务逻辑错误
                throw new InvalidOperationException("业务逻辑错误");
                
                await _db.Ado.CommitTranAsync();
            }
            catch
            {
                await _db.Ado.RollbackTranAsync();
            }

            // Assert
            var count = await _repository.CountAsync();
            Assert.Equal(0, count); // 事务应该回滚，没有实体被插入
        }

        /// <summary>
        /// 测试性能基准
        /// </summary>
        [Fact]
        public async Task PerformanceBenchmark_WithLargeDataset_MeetsRequirements()
        {
            // Arrange
            var entities = Enumerable.Range(1, 1000)
                .Select(i => new TestEntity 
                { 
                    Id = Guid.NewGuid(), 
                    Name = $"Performance Entity {i}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                })
                .ToList();

            // Act - 批量插入
            var insertStopwatch = System.Diagnostics.Stopwatch.StartNew();
            await _repository.AddRangeAsync(entities);
            await _repository.SaveChangesAsync();
            insertStopwatch.Stop();

            // Act - 分页查询
            var queryStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var pagedResult = await _repository.GetPagedAsync(1, 50);
            queryStopwatch.Stop();

            // Act - 条件查询
            var conditionStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var filteredResults = await _repository.GetByConditionAsync(e => e.Name.Contains("Performance"));
            conditionStopwatch.Stop();

            // Assert
            Assert.True(insertStopwatch.ElapsedMilliseconds < 2000, "批量插入应在2秒内完成");
            Assert.True(queryStopwatch.ElapsedMilliseconds < 500, "分页查询应在500毫秒内完成");
            Assert.True(conditionStopwatch.ElapsedMilliseconds < 1000, "条件查询应在1秒内完成");
            Assert.Equal(1000, pagedResult.TotalCount);
            Assert.Equal(50, pagedResult.Items.Count());
            Assert.Equal(1000, filteredResults.Count());
        }
    }
}