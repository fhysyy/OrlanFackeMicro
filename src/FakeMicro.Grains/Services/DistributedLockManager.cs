using FakeMicro.Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using System;
using System.Threading.Tasks;

namespace FakeMicro.Grains.Services
{
    /// <summary>
    /// 分布式锁管理器
    /// </summary>
    public interface IDistributedLockManager
    {
        /// <summary>
        /// 尝试获取锁
        /// </summary>
        /// <param name="lockKey">锁键</param>
        /// <param name="ownerId">所有者标识</param>
        /// <param name="timeoutMs">超时时间（毫秒）</param>
        /// <returns>锁令牌，如果获取失败则返回null</returns>
        Task<IDistributedLockToken?> TryAcquireAsync(string lockKey, string ownerId, int timeoutMs = 0);

        /// <summary>
        /// 释放锁
        /// </summary>
        /// <param name="lockKey">锁键</param>
        /// <param name="ownerId">所有者标识</param>
        /// <returns>是否成功释放</returns>
        Task<bool> ReleaseAsync(string lockKey, string ownerId);

        /// <summary>
        /// 检查锁是否被持有
        /// </summary>
        /// <param name="lockKey">锁键</param>
        /// <returns>是否被持有</returns>
        Task<bool> IsLockedAsync(string lockKey);

        /// <summary>
        /// 获取锁的所有者
        /// </summary>
        /// <param name="lockKey">锁键</param>
        /// <returns>所有者标识</returns>
        Task<string?> GetOwnerAsync(string lockKey);
    }

    /// <summary>
    /// 分布式锁令牌接口
    /// </summary>
    public interface IDistributedLockToken : IAsyncDisposable
    {
        string LockKey { get; }
        string OwnerId { get; }
        bool IsValid { get; }
        Task<bool> ReleaseAsync();
    }

    /// <summary>
    /// 分布式锁管理器实现
    /// </summary>
    public class DistributedLockManager : IDistributedLockManager
    {
        private readonly IGrainFactory _grainFactory;
        private readonly ILogger<DistributedLockManager> _logger;

        public DistributedLockManager(
            IGrainFactory grainFactory,
            ILogger<DistributedLockManager> logger)
        {
            _grainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IDistributedLockToken?> TryAcquireAsync(string lockKey, string ownerId, int timeoutMs = 0)
        {
            if (string.IsNullOrEmpty(lockKey))
            {
                throw new ArgumentException("锁键不能为空", nameof(lockKey));
            }

            if (string.IsNullOrEmpty(ownerId))
            {
                throw new ArgumentException("所有者标识不能为空", nameof(ownerId));
            }

            var lockGrain = _grainFactory.GetGrain<IDistributedLockGrain>(lockKey);
            var acquired = await lockGrain.TryAcquireAsync(ownerId, timeoutMs);

            if (acquired)
            {
                _logger.LogInformation("成功获取分布式锁: {LockKey}, 所有者: {OwnerId}", lockKey, ownerId);
                return new DistributedLockToken(lockGrain, lockKey, ownerId, _logger);
            }

            _logger.LogWarning("获取分布式锁失败: {LockKey}, 所有者: {OwnerId}", lockKey, ownerId);
            return null;
        }

        public async Task<bool> ReleaseAsync(string lockKey, string ownerId)
        {
            if (string.IsNullOrEmpty(lockKey))
            {
                throw new ArgumentException("锁键不能为空", nameof(lockKey));
            }

            var lockGrain = _grainFactory.GetGrain<IDistributedLockGrain>(lockKey);
            var released = await lockGrain.ReleaseAsync(ownerId);

            if (released)
            {
                _logger.LogInformation("成功释放分布式锁: {LockKey}, 所有者: {OwnerId}", lockKey, ownerId);
            }
            else
            {
                _logger.LogWarning("释放分布式锁失败: {LockKey}, 所有者: {OwnerId}", lockKey, ownerId);
            }

            return released;
        }

        public async Task<bool> IsLockedAsync(string lockKey)
        {
            if (string.IsNullOrEmpty(lockKey))
            {
                throw new ArgumentException("锁键不能为空", nameof(lockKey));
            }

            var lockGrain = _grainFactory.GetGrain<IDistributedLockGrain>(lockKey);
            return await lockGrain.IsLockedAsync();
        }

        public async Task<string?> GetOwnerAsync(string lockKey)
        {
            if (string.IsNullOrEmpty(lockKey))
            {
                throw new ArgumentException("锁键不能为空", nameof(lockKey));
            }

            var lockGrain = _grainFactory.GetGrain<IDistributedLockGrain>(lockKey);
            return await lockGrain.GetOwnerAsync();
        }
    }

    /// <summary>
    /// 分布式锁令牌实现
    /// </summary>
    public class DistributedLockToken : IDistributedLockToken
    {
        private readonly IDistributedLockGrain _lockGrain;
        private readonly ILogger _logger;
        private bool _disposed;

        public string LockKey { get; }
        public string OwnerId { get; }
        public bool IsValid { get; private set; }

        public DistributedLockToken(
            IDistributedLockGrain lockGrain,
            string lockKey,
            string ownerId,
            ILogger logger)
        {
            _lockGrain = lockGrain ?? throw new ArgumentNullException(nameof(lockGrain));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            LockKey = lockKey;
            OwnerId = ownerId;
            IsValid = true;
        }

        public async Task<bool> ReleaseAsync()
        {
            if (_disposed || !IsValid)
            {
                return false;
            }

            var released = await _lockGrain.ReleaseAsync(OwnerId);
            if (released)
            {
                IsValid = false;
            }
            return released;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (IsValid)
            {
                await ReleaseAsync();
            }
        }
    }

    /// <summary>
    /// 分布式锁扩展方法
    /// </summary>
    public static class DistributedLockExtensions
    {
        /// <summary>
        /// 在锁保护下执行操作
        /// </summary>
        public static async Task<T> ExecuteWithLockAsync<T>(
            this IDistributedLockManager lockManager,
            string lockKey,
            string ownerId,
            Func<Task<T>> action,
            int timeoutMs = 30000)
        {
            var lockToken = await lockManager.TryAcquireAsync(lockKey, ownerId, timeoutMs);
            if (lockToken == null)
            {
                throw new TimeoutException($"无法获取锁: {lockKey}");
            }

            try
            {
                return await action();
            }
            finally
            {
                await lockToken.DisposeAsync();
            }
        }

        /// <summary>
        /// 在锁保护下执行操作
        /// </summary>
        public static async Task ExecuteWithLockAsync(
            this IDistributedLockManager lockManager,
            string lockKey,
            string ownerId,
            Func<Task> action,
            int timeoutMs = 30000)
        {
            var lockToken = await lockManager.TryAcquireAsync(lockKey, ownerId, timeoutMs);
            if (lockToken == null)
            {
                throw new TimeoutException($"无法获取锁: {lockKey}");
            }

            try
            {
                await action();
            }
            finally
            {
                await lockToken.DisposeAsync();
            }
        }
    }
}
