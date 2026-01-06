using FakeMicro.Interfaces;
using Orleans;
using Orleans.Runtime;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FakeMicro.Grains
{
    /// <summary>
    /// 分布式锁Grain实现
    /// </summary>
    public class DistributedLockGrain : OrleansGrainBase, IDistributedLockGrain
    {
        private string? _ownerId;
        private DateTime? _expirationTime;
        private IDisposable? _timer;

        public DistributedLockGrain(ILogger<DistributedLockGrain> logger) : base(logger)
        {
        }

        /// <summary>
        /// 尝试获取锁
        /// </summary>
        public async Task<bool> TryAcquireAsync(string ownerId, int timeoutMs = 0)
        {
            if (string.IsNullOrEmpty(ownerId))
            {
                _logger.LogWarning("尝试获取锁失败: ownerId为空");
                return false;
            }

            var now = DateTime.UtcNow;

            if (_ownerId == null)
            {
                _ownerId = ownerId;
                _expirationTime = now.AddMilliseconds(timeoutMs > 0 ? timeoutMs : 30000);
                _logger.LogInformation("锁 {LockKey} 被获取，所有者: {OwnerId}, 过期时间: {ExpirationTime}",
                    this.GetPrimaryKeyString(), ownerId, _expirationTime);

                RegisterExpirationTimer();
                return true;
            }

            if (_expirationTime.HasValue && _expirationTime.Value < now)
            {
                _logger.LogWarning("锁 {LockKey} 已过期，强制释放", this.GetPrimaryKeyString());
                await ReleaseInternalAsync();
                return await TryAcquireAsync(ownerId, timeoutMs);
            }

            _logger.LogDebug("锁 {LockKey} 已被持有，所有者: {CurrentOwner}, 请求者: {Requester}",
                this.GetPrimaryKeyString(), _ownerId, ownerId);
            return false;
        }

        /// <summary>
        /// 释放锁
        /// </summary>
        public async Task<bool> ReleaseAsync(string ownerId)
        {
            if (_ownerId == null)
            {
                _logger.LogWarning("尝试释放锁 {LockKey} 失败: 锁未被持有", this.GetPrimaryKeyString());
                return false;
            }

            if (_ownerId != ownerId)
            {
                _logger.LogWarning("尝试释放锁 {LockKey} 失败: 所有者不匹配，当前所有者: {CurrentOwner}, 请求者: {Requester}",
                    this.GetPrimaryKeyString(), _ownerId, ownerId);
                return false;
            }

            await ReleaseInternalAsync();
            _logger.LogInformation("锁 {LockKey} 已释放，所有者: {OwnerId}",
                this.GetPrimaryKeyString(), ownerId);
            return true;
        }

        /// <summary>
        /// 检查锁是否被持有
        /// </summary>
        public Task<bool> IsLockedAsync()
        {
            var now = DateTime.UtcNow;
            var isLocked = _ownerId != null && (!_expirationTime.HasValue || _expirationTime.Value >= now);
            return Task.FromResult(isLocked);
        }

        /// <summary>
        /// 获取锁的所有者信息
        /// </summary>
        public Task<string?> GetOwnerAsync()
        {
            var now = DateTime.UtcNow;
            if (_ownerId != null && _expirationTime.HasValue && _expirationTime.Value < now)
            {
                return Task.FromResult<string?>(null);
            }
            return Task.FromResult(_ownerId);
        }

        /// <summary>
        /// 延长锁的持有时间
        /// </summary>
        public async Task<bool> ExtendAsync(string ownerId, int additionalMs)
        {
            if (_ownerId == null || _ownerId != ownerId)
            {
                _logger.LogWarning("延长锁 {LockKey} 失败: 所有者不匹配", this.GetPrimaryKeyString());
                return false;
            }

            if (_expirationTime.HasValue)
            {
                _expirationTime = _expirationTime.Value.AddMilliseconds(additionalMs);
                RegisterExpirationTimer();
                _logger.LogInformation("锁 {LockKey} 已延长 {AdditionalMs}ms，新的过期时间: {ExpirationTime}",
                    this.GetPrimaryKeyString(), additionalMs, _expirationTime);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 强制释放锁
        /// </summary>
        public async Task<bool> ForceReleaseAsync()
        {
            if (_ownerId == null)
            {
                return false;
            }

            var previousOwner = _ownerId;
            await ReleaseInternalAsync();
            _logger.LogWarning("锁 {LockKey} 已被强制释放，原所有者: {PreviousOwner}",
                this.GetPrimaryKeyString(), previousOwner);
            return true;
        }

        private async Task ReleaseInternalAsync()
        {
            _ownerId = null;
            _expirationTime = null;

            try
            {
                _timer?.Dispose();
                _timer = null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "取消锁过期定时器失败: {LockKey}", this.GetPrimaryKeyString());
            }
        }

        private void RegisterExpirationTimer()
        {
            _timer?.Dispose();
            
            if (_expirationTime.HasValue)
            {
                var timeUntilExpiration = _expirationTime.Value - DateTime.UtcNow;
                if (timeUntilExpiration.TotalMilliseconds > 0)
                {
                    _timer = RegisterTimer(
                        async state => await ReleaseInternalAsync(),
                        null,
                        timeUntilExpiration,
                        TimeSpan.FromMilliseconds(-1));
                }
            }
        }

        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("分布式锁Grain {LockKey} 已激活", this.GetPrimaryKeyString());
            return base.OnActivateAsync(cancellationToken);
        }

        public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
        {
            _timer?.Dispose();
            _logger.LogInformation("分布式锁Grain {LockKey} 已停用", this.GetPrimaryKeyString());
            return base.OnDeactivateAsync(reason, cancellationToken);
        }
    }
}
