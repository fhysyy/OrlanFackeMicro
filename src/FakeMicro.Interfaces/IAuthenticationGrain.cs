using FakeMicro.Interfaces.Models;
using Orleans;
using System.Threading;
using System.Threading.Tasks;

namespace FakeMicro.Interfaces
{
    /// <summary>
    /// 用户认证Grain接口 - 负责身份验证和令牌管理
    /// </summary>
    public interface IAuthenticationGrain : IGrainWithGuidKey
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
    }
}
