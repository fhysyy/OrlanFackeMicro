using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeMicro.Grains.Bases;
using Microsoft.Extensions.Logging;
using Moq;
using Orleans;
using Orleans.Runtime;
using Xunit;

namespace FakeMicro.Interfaces.Tests
{
    /// <summary>
    /// Orleans状态管理测试类
    /// 测试批量状态更新和冲突解决策略
    /// </summary>
    public class OrleansStateManagementTests
    {
        private readonly Mock<IGrainFactory> _mockGrainFactory;
        private readonly Mock<ILogger<OrleansStatefulGrainBase<TestState>>> _mockLogger;
        private readonly Mock<IStorage<TestState>> _mockStorage;
        private readonly TestGrain _testGrain;

        public OrleansStateManagementTests()
        {
            _mockGrainFactory = new Mock<IGrainFactory>();
            _mockLogger = new Mock<ILogger<OrleansStatefulGrainBase<TestState>>>();
            _mockStorage = new Mock<IStorage<TestState>>();
            
            // 设置初始状态
            var initialState = new TestState { Version = 1, Data = "Initial Data" };
            _mockStorage.SetupGet(s => s.State).Returns(initialState);
            
            // 创建测试Grain
            _testGrain = new TestGrain(
                _mockGrainFactory.Object,
                _mockLogger.Object,
                _mockStorage.Object);
        }

        /// <summary>
        /// 测试批量状态更新功能
        /// </summary>
        [Fact]
        public async Task ExecuteBatchStateUpdateAsync_ShouldBatchMultipleUpdates()
        {
            // 模拟存储操作
            _mockStorage.Setup(s => s.WriteStateAsync())
                .Returns(Task.CompletedTask);

            // 执行多次状态更新操作
            await _testGrain.UpdateDataAsync("Update 1");
            await _testGrain.UpdateDataAsync("Update 2");
            await _testGrain.UpdateDataAsync("Update 3");
            
            // 触发批量更新执行
            await _testGrain.FlushBatchUpdatesAsync();
            
            // 验证最终状态
            Assert.Equal("Update 3", _testGrain.State.Data);
            Assert.Equal(4, _testGrain.State.Version); // 初始版本1 + 3次更新 = 4
            
            // 验证存储只被调用一次
            _mockStorage.Verify(s => s.WriteStateAsync(), Times.Once);
        }

        /// <summary>
        /// 测试冲突解决策略
        /// </summary>
        [Fact]
        public async Task PersistStateAsync_ShouldRetryOnInconsistentStateException()
        {
            // 模拟第一次调用时的并发冲突
            int callCount = 0;
            _mockStorage.Setup(s => s.WriteStateAsync())
                .Callback(() =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        // 第一次调用抛出并发冲突异常
                        throw new InconsistentStateException(
                            "Concurrency conflict",
                            new TestState { Version = 1, Data = "Initial Data" },
                            new TestState { Version = 2, Data = "Updated by another process" });
                    }
                })
                .Returns(Task.CompletedTask);

            // 模拟读取最新状态
            _mockStorage.Setup(s => s.ReadStateAsync())
                .Callback(() =>
                {
                    // 更新状态为最新版本
                    _mockStorage.SetupGet(st => st.State).Returns(
                        new TestState { Version = 2, Data = "Updated by another process" });
                })
                .Returns(Task.CompletedTask);

            // 更新数据
            _testGrain.State.Data = "New Update";
            _testGrain.State.Version = 2; // 尝试在版本2上更新
            
            // 执行持久化并验证重试成功
            await _testGrain.PersistStateAsync();
            
            // 验证存储被调用了两次（第一次失败，第二次成功）
            _mockStorage.Verify(s => s.WriteStateAsync(), Times.Exactly(2));
            _mockStorage.Verify(s => s.ReadStateAsync(), Times.Once);
        }

        /// <summary>
        /// 测试停用自动持久化
        /// </summary>
        [Fact]
        public async Task OnDeactivateAsync_ShouldPersistState()
        {
            // 模拟存储操作
            _mockStorage.Setup(s => s.WriteStateAsync())
                .Returns(Task.CompletedTask);

            // 模拟停用
            await _testGrain.OnDeactivateAsync();
            
            // 验证状态被持久化
            _mockStorage.Verify(s => s.WriteStateAsync(), Times.Once);
        }

        /// <summary>
        /// 用于测试的状态类
        /// </summary>
        public class TestState
        {
            public int Version { get; set; }
            public string Data { get; set; }
            
            public TestState DeepCopy()
            {
                return new TestState { Version = this.Version, Data = this.Data };
            }
        }

        /// <summary>
        /// 用于测试的Grain类
        /// </summary>
        private class TestGrain : OrleansStatefulGrainBase<TestState>
        {
            public TestGrain(
                IGrainFactory grainFactory,
                ILogger<OrleansStatefulGrainBase<TestState>> logger,
                IStorage<TestState> storage)
                : base(grainFactory, logger, storage)
            { }

            public async Task UpdateDataAsync(string newData)
            {
                // 使用批量更新机制
                await ExecuteBatchStateUpdateAsync(() =>
                {
                    State.Data = newData;
                    State.Version++;
                });
            }

            public async Task FlushBatchUpdatesAsync()
            {
                // 强制刷新批量更新队列
                await ForcePersistStateAsync();
            }
        }
    }
}
