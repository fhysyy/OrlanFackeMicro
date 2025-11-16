using FakeMicro.Interfaces.Attributes;
using Orleans;

namespace FakeMicro.Interfaces
{
    [Version(1, 1)]
    public interface IAuthGrain : IGrainWithStringKey
    {
        /// <summary>
        /// 用户注册
        /// </summary>
        Task<UserAuthResult> RegisterAsync(string username, string email, string password, string? displayName = null);
        
        /// <summary>
        /// 用户登录
        /// </summary>
        Task<UserAuthResult> LoginAsync(string username, string password);
        
        /// <summary>
        /// 验证Token
        /// </summary>
        Task<UserAuthResult> ValidateTokenAsync(string token);
        
        /// <summary>
        /// 刷新Token
        /// </summary>
        Task<UserAuthResult> RefreshTokenAsync(string refreshToken);
        
        /// <summary>
        /// 用户登出
        /// </summary>
        Task LogoutAsync();
        
        /// <summary>
        /// 修改密码
        /// </summary>
        Task<bool> ChangePasswordAsync(string oldPassword, string newPassword);
    }
    
    [GenerateSerializer]
    public class UserAuthResult
    {
        [Id(0)]
        public bool Success { get; set; }
        
        [Id(1)]
        public string Message { get; set; } = string.Empty;
        
        [Id(2)]
        public string Token { get; set; } = string.Empty;
        
        [Id(3)]
        public string RefreshToken { get; set; } = string.Empty;
        
        [Id(4)]
        public DateTime? ExpiresAt { get; set; }
        
        [Id(5)]
        public UserInfo? User { get; set; }
    }
    
    [GenerateSerializer]
    public class UserInfo
    {
        [Id(0)]
        public long Id { get; set; }
        
        [Id(1)]
        public string Username { get; set; } = string.Empty;
        
        [Id(2)]
        public string Email { get; set; } = string.Empty;
        
        [Id(3)]
        public string DisplayName { get; set; } = string.Empty;
        
        [Id(4)]
        public string Role { get; set; } = "User";
        
        [Id(5)]
        public DateTime CreatedAt { get; set; }
    }
}