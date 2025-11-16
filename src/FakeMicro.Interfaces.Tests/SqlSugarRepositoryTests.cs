using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeMicro.DatabaseAccess;
using FakeMicro.DatabaseAccess.Exception;
using FakeMicro.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using SqlSugar;
using Xunit;

namespace FakeMicro.Interfaces.Tests
{
    /// <summary>
    /// SqlSugarRepository测试类
    /// 测试连接管理、查询性能优化、缓存功能和批量操作
    /// </summary>
    public class SqlSugarRepositoryTests
    {
        private readonly Mock<ISqlSugarClient> _mockDb;
        private readonly Mock<ILogger<SqlSugarRepository<UserEntity, int>>> _mockLogger;
        private readonly Mock<IQueryCacheManager> _mockQueryCacheManager;
        private readonly SqlSugarRepository<UserEntity, int> _repository;

        public SqlSugarRepositoryTests()
        {
            _mockDb = new Mock<ISqlSugarClient>();
            _mockLogger = new Mock<ILogger<SqlSugarRepository<UserEntity, int>>>();
            _mockQueryCacheManager = new Mock<IQueryCacheManager>();
            
            // 设置SqlSugarConfig模拟
            var mockConfig = new Mock<ISqlSugarConfig>();
            mockConfig.Setup(c => c.GetSqlSugarClient()).Returns(_mockDb.Object);
            
            // 创建仓储实例
            _repository = new SqlSugarRepository<UserEntity, int>(
                mockConfig.Object,
                _mockLogger.Object,
                _mockQueryCacheManager.Object);
            /// <summary>
        /// 测试GetByIdAsync的缓存功能
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldUseCache_WhenQueryCacheManagerIsAvailable()
        {
            // 准备测试数据
            var testUser = new UserEntity { Id = 1, Username = "testuser", Email = "test@example.com" };
            
            // 设置缓存管理器模拟行为
            _mockQueryCacheManager.Setup(cache => cache.GenerateCacheKey<UserEntity>("1"))
                .Returns("CacheKey_UserEntity_1");
            
            _mockQueryCacheManager.Setup(cache => cache.GetOrCreateAsync(
                "CacheKey_UserEntity_1",
                It.IsAny<Func<Task<UserEntity?>>>,
                5))
                .ReturnsAsync(testUser);
            
            // 执行查询
            var result = await _repository.GetByIdAsync(1);
            
            // 验证结果
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            
            // 验证缓存方法被调用
            _mockQueryCacheManager.Verify(cache => cache.GenerateCacheKey<UserEntity>("1"), Times.Once);
            _mockQueryCacheManager.Verify(cache => cache.GetOrCreateAsync(
                "CacheKey_UserEntity_1",
                It.IsAny<Func<Task<UserEntity?>>>,
                5), Times.Once);
        }
        
        /// <summary>
        /// 测试AddAsync的缓存清除功能
        /// </summary>
        [Fact]
        public async Task AddAsync_ShouldClearEntityCache_WhenOperationSucceeds()
        {
            // 准备测试数据
            var testUser = new UserEntity { Id = 1, Username = "newuser", Email = "new@example.com" };
            
            // 设置数据库模拟行为
            var mockInsertable = new Mock<IInsertable<UserEntity>>();
            mockInsertable.Setup(i => i.ExecuteReturnEntityAsync()).ReturnsAsync(testUser);
            
            _mockDb.Setup(db => db.Insertable(testUser)).Returns(mockInsertable.Object);
            
            // 执行添加操作
            await _repository.AddAsync(testUser);
            
            // 验证缓存清除方法被调用
            _mockQueryCacheManager.Verify(cache => cache.RemoveEntityCacheAsync(typeof(UserEntity)), Times.Once);
        }
        
        /// <summary>
        /// 测试UpdateAsync的缓存清除功能
        /// </summary>
        [Fact]
        public async Task UpdateAsync_ShouldClearEntityCache_WhenOperationSucceeds()
        {
            // 准备测试数据
            var testUser = new UserEntity { Id = 1, Username = "updateduser", Email = "updated@example.com" };
            
            // 设置数据库模拟行为
            var mockUpdateable = new Mock<IUpdateable<UserEntity>>();
            mockUpdateable.Setup(u => u.ExecuteCommandAsync()).ReturnsAsync(1);
            
            _mockDb.Setup(db => db.Updateable(testUser)).Returns(mockUpdateable.Object);
            
            // 设置缓存管理器模拟行为
            _mockQueryCacheManager.Setup(cache => cache.GenerateCacheKey<UserEntity>("1"))
                .Returns("CacheKey_UserEntity_1");
            
            // 执行更新操作
            await _repository.UpdateAsync(testUser);
            
            // 验证缓存清除方法被调用
            _mockQueryCacheManager.Verify(cache => cache.RemoveAsync("CacheKey_UserEntity_1"), Times.Once);
            _mockQueryCacheManager.Verify(cache => cache.RemoveEntityCacheAsync(typeof(UserEntity)), Times.Once);
        }
        
        /// <summary>
        /// 测试DeleteByIdAsync的缓存清除功能
        /// </summary>
        [Fact]
        public async Task DeleteByIdAsync_ShouldClearEntityCache_WhenOperationSucceeds()
        {
            // 设置数据库模拟行为
            _mockDb.Setup(db => db.Deleteable<UserEntity>().In(1).ExecuteCommandAsync())
                .ReturnsAsync(1);
            
            // 设置缓存管理器模拟行为
            _mockQueryCacheManager.Setup(cache => cache.GenerateCacheKey<UserEntity>("1"))
                .Returns("CacheKey_UserEntity_1");
            
            // 执行删除操作
            await _repository.DeleteByIdAsync(1);
            
            // 验证缓存清除方法被调用
            _mockQueryCacheManager.Verify(cache => cache.RemoveAsync("CacheKey_UserEntity_1"), Times.Once);
            _mockQueryCacheManager.Verify(cache => cache.RemoveEntityCacheAsync(typeof(UserEntity)), Times.Once);
        }
        
        /// <summary>
        /// 测试性能对比 - 缓存命中 vs 缓存未命中
        /// <summary>
        [Fact]
        public async Task PerformanceComparison_TestCacheHitVsMiss()
        {
            // 准备测试数据
            var testUser = new UserEntity { Id = 1, Username = "performanceuser", Email = "performance@example.com" };
            
            // 设置缓存管理器模拟行为
            _mockQueryCacheManager.Setup(cache => cache.GenerateCacheKey<UserEntity>("1"))
                .Returns("CacheKey_UserEntity_1");
            
            // 第一次调用 - 模拟缓存未命中，执行数据库查询
            var firstCallComplete = false;
            _mockQueryCacheManager.SetupSequence(cache => cache.GetOrCreateAsync(
                "CacheKey_UserEntity_1",
                It.IsAny<Func<Task<UserEntity?>>>,
                5))
                .Callback((string key, Func<Task<UserEntity?>> factory, int minutes) => {
                    // 模拟数据库查询延迟
                    Task.Delay(100).Wait();
                    firstCallComplete = true;
                })
                .ReturnsAsync(testUser)
                // 第二次调用 - 模拟缓存命中，直接返回缓存结果
                .ReturnsAsync(testUser);
            
            // 执行两次查询
            var stopwatch1 = System.Diagnostics.Stopwatch.StartNew();
            await _repository.GetByIdAsync(1);
            stopwatch1.Stop();
            
            var stopwatch2 = System.Diagnostics.Stopwatch.StartNew();
            await _repository.GetByIdAsync(1);
            stopwatch2.Stop();
            
            // 验证第二次调用（缓存命中）应该比第一次调用（缓存未命中）快
            Assert.True(stopwatch2.ElapsedMilliseconds < stopwatch1.ElapsedMilliseconds * 0.5, 
                "缓存命中应该比缓存未命中快至少50%");
            
            // 验证两次查询都成功返回结果
            _mockQueryCacheManager.Verify(cache => cache.GetOrCreateAsync(
                "CacheKey_UserEntity_1",
                It.IsAny<Func<Task<UserEntity?>>>,
                5), Times.Exactly(2));
        }
    }

        /// <summary>
        /// 测试连接健康检查功能
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_ShouldMarkConnectionUnhealthy_WhenConnectionExceptionOccurs()
        {
            // 模拟连接异常
            _mockDb.Setup(db => db.Queryable<UserEntity>())
                .Throws(new SqlSugarException("Connection timeout occurred"));

            // 执行操作并验证异常
            var exception = await Assert.ThrowsAsync<DataAccessException>(
                () => _repository.GetByIdAsync(1));
            
            // 验证异常消息
            Assert.Contains("数据库连接异常", exception.Message);
            
            // 验证日志记录
            _mockLogger.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("数据库连接异常")),
                It.IsAny<SqlSugarException>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// 测试GetAllAsync分页强制约束
        /// </summary>
        [Fact]
        public async Task GetAllAsync_ShouldEnforcePaginationConstraints()
        {
            // 模拟查询结果
            var mockQueryable = new Mock<ISugarQueryable<UserEntity>>();
            _mockDb.Setup(db => db.Queryable<UserEntity>())
                .Returns(mockQueryable.Object);
            
            mockQueryable.Setup(q => q.With(SqlWith.NoLock))
                .Returns(mockQueryable.Object);
            
            mockQueryable.Setup(q => q.CountAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1000);
            
            mockQueryable.Setup(q => q.ToPageListAsync(1, 100))
                .ReturnsAsync(new List<UserEntity>());

            // 测试默认参数
            var result1 = await _repository.GetAllAsync();
            Assert.Equal(1, result1.PageIndex);
            Assert.Equal(100, result1.PageSize);

            // 测试边界值 - 最小值
            var result2 = await _repository.GetAllAsync(-1, 0);
            Assert.Equal(1, result2.PageIndex);
            Assert.Equal(1, result2.PageSize);

            // 测试边界值 - 最大值
            var result3 = await _repository.GetAllAsync(10, 2000);
            Assert.Equal(10, result3.PageIndex);
            Assert.Equal(1000, result3.PageSize);
        }

        /// <summary>
        /// 测试查询缓存功能
        /// </summary>
        [Fact]
        public async Task GetAllAsync_ShouldUseCache_WhenEnabled()
        {
            // 模拟查询结果
            var mockQueryable = new Mock<ISugarQueryable<UserEntity>>();
            _mockDb.Setup(db => db.Queryable<UserEntity>())
                .Returns(mockQueryable.Object);
            
            mockQueryable.Setup(q => q.With(SqlWith.NoLock))
                .Returns(mockQueryable.Object);
            
            // 模拟缓存设置
            mockQueryable.Setup(q => q.WithCacheIF(true, "UserEntity_List_1_100", 300))
                .Returns(mockQueryable.Object);
            
            mockQueryable.Setup(q => q.CountAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(100);
            
            mockQueryable.Setup(q => q.ToPageListAsync(1, 100))
                .ReturnsAsync(new List<UserEntity>());

            // 启用缓存执行查询
            await _repository.GetAllAsync(1, 100, true, 5);
            
            // 验证缓存设置被调用
            mockQueryable.Verify(q => q.WithCacheIF(true, "UserEntity_List_1_100", 300), Times.Once);
            
            // 验证日志记录
            _mockLogger.Verify(logger => logger.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("使用查询缓存")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// 测试GetPagedAsync分页和缓存功能
        /// </summary>
        [Fact]
        public async Task GetPagedAsync_ShouldReturnCorrectPagedResult()
        {
            // 模拟查询结果
            var mockQueryable = new Mock<ISugarQueryable<UserEntity>>();
            _mockDb.Setup(db => db.Queryable<UserEntity>())
                .Returns(mockQueryable.Object);
            
            mockQueryable.Setup(q => q.With(SqlWith.NoLock))
                .Returns(mockQueryable.Object);
            
            mockQueryable.Setup(q => q.CountAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(500);
            
            mockQueryable.Setup(q => q.ToPageListAsync(2, 100))
                .ReturnsAsync(new List<UserEntity>());

            // 执行分页查询
            var result = await _repository.GetPagedAsync(2, 100);
            
            // 验证分页结果
            Assert.Equal(2, result.PageIndex);
            Assert.Equal(100, result.PageSize);
            Assert.Equal(500, result.TotalCount);
            Assert.Equal(5, result.TotalPages);
            Assert.True(result.HasPrevious);
            Assert.True(result.HasNext);
        }
    }

    // 用于测试的用户实体类
    public class UserEntity
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
