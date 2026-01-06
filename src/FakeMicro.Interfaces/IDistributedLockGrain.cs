using Orleans;
using Orleans.Concurrency;
using System.Threading.Tasks;

namespace FakeMicro.Interfaces
{
    /// <summary>
    /// 分布式锁Grain接口
    /// </summary>
    public interface IDistributedLockGrain : IGrainWithStringKey
    {
        /// <summary>
        /// 尝试获取锁
        /// </summary>
        /// <param name="ownerId">锁所有者标识</param>
        /// <param name="timeoutMs">超时时间（毫秒），0表示不等待</param>
        /// <returns>是否成功获取锁</returns>
        Task<bool> TryAcquireAsync(string ownerId, int timeoutMs = 0);

        /// <summary>
        /// 释放锁
        /// </summary>
        /// <param name="ownerId">锁所有者标识</param>
        /// <returns>是否成功释放锁</returns>
        Task<bool> ReleaseAsync(string ownerId);

        /// <summary>
        /// 检查锁是否被持有
        /// </summary>
        /// <returns>锁是否被持有</returns>
        [ReadOnly]
        [AlwaysInterleave]
        Task<bool> IsLockedAsync();

        /// <summary>
        /// 获取锁的所有者信息
        /// </summary>
        /// <returns>锁所有者标识，如果未被持有则返回null</returns>
        [ReadOnly]
        [AlwaysInterleave]
        Task<string?> GetOwnerAsync();

        /// <summary>
        /// 延长锁的持有时间
        /// </summary>
        /// <param name="ownerId">锁所有者标识</param>
        /// <param name="additionalMs">延长的毫秒数</param>
        /// <returns>是否成功延长</returns>
        Task<bool> ExtendAsync(string ownerId, int additionalMs);

        /// <summary>
        /// 强制释放锁（仅限管理员使用）
        /// </summary>
        /// <returns>是否成功强制释放</returns>
        Task<bool> ForceReleaseAsync();
    }
}
