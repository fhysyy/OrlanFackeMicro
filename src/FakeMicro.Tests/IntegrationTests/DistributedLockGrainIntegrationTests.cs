using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
using FakeMicro.Entities;
using FakeMicro.Entities.Enums;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FakeMicro.Tests.IntegrationTests
{
    public class DistributedLockGrainIntegrationTests : IClassFixture<TestClusterFixture>
    {
        private readonly TestClusterFixture _fixture;

        public DistributedLockGrainIntegrationTests(TestClusterFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task DistributedLock_TryAcquire_Success()
        {
            var lockGrain = _fixture.Cluster.GrainFactory.GetGrain<IDistributedLockGrain>("test-lock-1");
            var ownerId = "owner-1";

            var acquired = await lockGrain.TryAcquireAsync(ownerId);

            Assert.True(acquired);

            var isLocked = await lockGrain.IsLockedAsync();
            Assert.True(isLocked);

            var currentOwner = await lockGrain.GetOwnerAsync();
            Assert.Equal(ownerId, currentOwner);
        }

        [Fact]
        public async Task DistributedLock_TryAcquire_WhenLocked_ReturnsFalse()
        {
            var lockGrain = _fixture.Cluster.GrainFactory.GetGrain<IDistributedLockGrain>("test-lock-2");
            var owner1 = "owner-1";
            var owner2 = "owner-2";

            var acquired1 = await lockGrain.TryAcquireAsync(owner1);
            Assert.True(acquired1);

            var acquired2 = await lockGrain.TryAcquireAsync(owner2);
            Assert.False(acquired2);

            var currentOwner = await lockGrain.GetOwnerAsync();
            Assert.Equal(owner1, currentOwner);
        }

        [Fact]
        public async Task DistributedLock_Release_Success()
        {
            var lockGrain = _fixture.Cluster.GrainFactory.GetGrain<IDistributedLockGrain>("test-lock-3");
            var ownerId = "owner-1";

            await lockGrain.TryAcquireAsync(ownerId);

            var released = await lockGrain.ReleaseAsync(ownerId);
            Assert.True(released);

            var isLocked = await lockGrain.IsLockedAsync();
            Assert.False(isLocked);

            var currentOwner = await lockGrain.GetOwnerAsync();
            Assert.Null(currentOwner);
        }

        [Fact]
        public async Task DistributedLock_Release_WithWrongOwner_ReturnsFalse()
        {
            var lockGrain = _fixture.Cluster.GrainFactory.GetGrain<IDistributedLockGrain>("test-lock-4");
            var owner1 = "owner-1";
            var owner2 = "owner-2";

            await lockGrain.TryAcquireAsync(owner1);

            var released = await lockGrain.ReleaseAsync(owner2);
            Assert.False(released);

            var isLocked = await lockGrain.IsLockedAsync();
            Assert.True(isLocked);

            var currentOwner = await lockGrain.GetOwnerAsync();
            Assert.Equal(owner1, currentOwner);
        }

        [Fact]
        public async Task DistributedLock_Extend_Success()
        {
            var lockGrain = _fixture.Cluster.GrainFactory.GetGrain<IDistributedLockGrain>("test-lock-5");
            var ownerId = "owner-1";
            var timeoutMs = 3000;

            await lockGrain.TryAcquireAsync(ownerId, timeoutMs);

            var extended = await lockGrain.ExtendAsync(ownerId, 2000);
            Assert.True(extended);

            var isLocked = await lockGrain.IsLockedAsync();
            Assert.True(isLocked);
        }

        [Fact]
        public async Task DistributedLock_Extend_WithWrongOwner_ReturnsFalse()
        {
            var lockGrain = _fixture.Cluster.GrainFactory.GetGrain<IDistributedLockGrain>("test-lock-6");
            var owner1 = "owner-1";
            var owner2 = "owner-2";

            await lockGrain.TryAcquireAsync(owner1);

            var extended = await lockGrain.ExtendAsync(owner2, 2000);
            Assert.False(extended);
        }

        [Fact]
        public async Task DistributedLock_ForceRelease_Success()
        {
            var lockGrain = _fixture.Cluster.GrainFactory.GetGrain<IDistributedLockGrain>("test-lock-7");
            var ownerId = "owner-1";

            await lockGrain.TryAcquireAsync(ownerId);

            var forceReleased = await lockGrain.ForceReleaseAsync();
            Assert.True(forceReleased);

            var isLocked = await lockGrain.IsLockedAsync();
            Assert.False(isLocked);

            var currentOwner = await lockGrain.GetOwnerAsync();
            Assert.Null(currentOwner);
        }

        [Fact]
        public async Task DistributedLock_ForceRelease_WhenNotLocked_ReturnsFalse()
        {
            var lockGrain = _fixture.Cluster.GrainFactory.GetGrain<IDistributedLockGrain>("test-lock-8");

            var forceReleased = await lockGrain.ForceReleaseAsync();
            Assert.False(forceReleased);
        }

        [Fact]
        public async Task DistributedLock_ExpireAfterTimeout()
        {
            var lockGrain = _fixture.Cluster.GrainFactory.GetGrain<IDistributedLockGrain>("test-lock-9");
            var ownerId = "owner-1";
            var timeoutMs = 2000;

            var acquired = await lockGrain.TryAcquireAsync(ownerId, timeoutMs);
            Assert.True(acquired);

            var isLocked = await lockGrain.IsLockedAsync();
            Assert.True(isLocked);

            await Task.Delay(timeoutMs + 500);

            isLocked = await lockGrain.IsLockedAsync();
            Assert.False(isLocked);

            var currentOwner = await lockGrain.GetOwnerAsync();
            Assert.Null(currentOwner);
        }

        [Fact]
        public async Task DistributedLock_AcquireAfterExpiration_Success()
        {
            var lockGrain = _fixture.Cluster.GrainFactory.GetGrain<IDistributedLockGrain>("test-lock-10");
            var owner1 = "owner-1";
            var owner2 = "owner-2";
            var timeoutMs = 2000;

            var acquired1 = await lockGrain.TryAcquireAsync(owner1, timeoutMs);
            Assert.True(acquired1);

            await Task.Delay(timeoutMs + 500);

            var acquired2 = await lockGrain.TryAcquireAsync(owner2, timeoutMs);
            Assert.True(acquired2);

            var currentOwner = await lockGrain.GetOwnerAsync();
            Assert.Equal(owner2, currentOwner);
        }

        [Fact]
        public async Task DistributedLock_ConcurrentAccess_PreventsRaceCondition()
        {
            var lockGrain = _fixture.Cluster.GrainFactory.GetGrain<IDistributedLockGrain>("test-lock-11");
            var tasks = new List<Task<bool>>();

            for (int i = 0; i < 10; i++)
            {
                var ownerId = $"owner-{i}";
                tasks.Add(lockGrain.TryAcquireAsync(ownerId));
            }

            var results = await Task.WhenAll(tasks);

            var successCount = results.Count(r => r);
            Assert.Equal(1, successCount);

            var currentOwner = await lockGrain.GetOwnerAsync();
            Assert.NotNull(currentOwner);
        }

        [Fact]
        public async Task DistributedLock_ReleaseAndReacquire_Success()
        {
            var lockGrain = _fixture.Cluster.GrainFactory.GetGrain<IDistributedLockGrain>("test-lock-12");
            var owner1 = "owner-1";
            var owner2 = "owner-2";

            await lockGrain.TryAcquireAsync(owner1);
            await lockGrain.ReleaseAsync(owner1);

            var acquired2 = await lockGrain.TryAcquireAsync(owner2);
            Assert.True(acquired2);

            var currentOwner = await lockGrain.GetOwnerAsync();
            Assert.Equal(owner2, currentOwner);
        }

        [Fact]
        public async Task DistributedLock_MultipleExtensions_Success()
        {
            var lockGrain = _fixture.Cluster.GrainFactory.GetGrain<IDistributedLockGrain>("test-lock-13");
            var ownerId = "owner-1";
            var timeoutMs = 2000;

            await lockGrain.TryAcquireAsync(ownerId, timeoutMs);

            for (int i = 0; i < 3; i++)
            {
                await Task.Delay(1000);
                var extended = await lockGrain.ExtendAsync(ownerId, 2000);
                Assert.True(extended);

                var isLocked = await lockGrain.IsLockedAsync();
                Assert.True(isLocked);
            }
        }

        [Fact]
        public async Task DistributedLock_DefaultTimeout_30Seconds()
        {
            var lockGrain = _fixture.Cluster.GrainFactory.GetGrain<IDistributedLockGrain>("test-lock-14");
            var ownerId = "owner-1";

            await lockGrain.TryAcquireAsync(ownerId);

            var isLocked = await lockGrain.IsLockedAsync();
            Assert.True(isLocked);

            await Task.Delay(1000);

            isLocked = await lockGrain.IsLockedAsync();
            Assert.True(isLocked);
        }

        [Fact]
        public async Task DistributedLock_EmptyOwnerId_ReturnsFalse()
        {
            var lockGrain = _fixture.Cluster.GrainFactory.GetGrain<IDistributedLockGrain>("test-lock-15");

            var acquired = await lockGrain.TryAcquireAsync("");
            Assert.False(acquired);

            acquired = await lockGrain.TryAcquireAsync(null);
            Assert.False(acquired);
        }
    }
}
