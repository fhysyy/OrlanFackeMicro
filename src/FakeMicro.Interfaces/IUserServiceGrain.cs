using FakeMicro.Interfaces.Models;
using Orleans;
using Orleans.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FakeMicro.Interfaces
{
    /// <summary>
    /// 用户服务Grain接口
    /// </summary>
    public interface IUserServiceGrain : IGrainWithGuidKey
    {
        /// <summary>
        /// 用户注册
        /// </summary>
        Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 用户登录
        /// </summary>
        Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 刷新令牌
        /// </summary>
        Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 用户登出
        /// </summary>
        Task<bool> LogoutAsync(long userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据用户名或邮箱查找用户
        /// </summary>
        [ReadOnly]
        [AlwaysInterleave]
        Task<long?> FindUserByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default);

        /// <summary>
        /// 验证用户权限
        /// </summary>
        [ReadOnly]
        [AlwaysInterleave]
        Task<bool> ValidateUserPermissionAsync(long userId, string resource, string permissionType, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户统计信息
        /// </summary>
        [ReadOnly]
        [AlwaysInterleave]
        Task<UserStatistics> GetUserStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取用户列表
        /// </summary>
        [ReadOnly]
        [AlwaysInterleave]
        Task<List<UserDto>> GetUsersAsync(string? username = null, string? email = null, string? status = null, CancellationToken cancellationToken = default);
    }
}