using Orleans;
using Orleans.Concurrency;
using System.Threading;
using System.Threading.Tasks;

namespace FakeMicro.Interfaces
{
    /// <summary>
    /// 用户管理Grain接口 - 负责用户权限和状态管理
    /// </summary>
    public interface IUserManagementGrain : IGrainWithGuidKey
    {
        /// <summary>
        /// 用户登出
        /// </summary>
        Task<bool> LogoutAsync(long userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 验证用户权限
        /// </summary>
        [ReadOnly]
        [AlwaysInterleave]
        Task<bool> ValidateUserPermissionAsync(long userId, string resource, string permissionType, CancellationToken cancellationToken = default);
    }
}
