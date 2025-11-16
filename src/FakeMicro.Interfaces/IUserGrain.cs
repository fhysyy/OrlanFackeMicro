using FakeMicro.Entities;
using FakeMicro.Interfaces.Models;
using Orleans;
using Orleans.Concurrency;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FakeMicro.Interfaces
{
    /// <summary>
    /// 用户Grain接口
    /// </summary>
    public interface IUserGrain : IGrainWithStringKey
    {
        /// <summary>
        /// 初始化Grain
        /// </summary>
        [AlwaysInterleave] // 允许在激活期间被中断
        Task InitializeAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 获取用户信息（从内存状态）
        /// </summary>
        [ReadOnly] // 标记为只读操作
        Task<UserDto?> GetUserAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 从数据库直接获取用户信息
        /// </summary>
        [ReadOnly]
        Task<UserDto?> GetUserFromDatabaseAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 更新用户信息
        /// </summary>
        Task<UpdateUserResult> UpdateUserAsync(UserDto userDto, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 验证密码
        /// </summary>
        [ReadOnly]
        Task<bool> ValidatePasswordAsync(string password, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 修改密码
        /// </summary>
        Task<ChangePasswordResult> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 更新登录信息
        /// </summary>
        Task UpdateLoginInfoAsync(bool success, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 生成刷新令牌
        /// </summary>
        Task<string?> GenerateRefreshTokenAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 验证刷新令牌
        /// </summary>
        [ReadOnly]
        Task<bool> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 获取用户权限
        /// </summary>
        [ReadOnly]
        Task<List<Permission>> GetPermissionsAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 检查用户是否有权限
        /// </summary>
        [ReadOnly]
        Task<bool> HasPermissionAsync(string resource, string permissionType, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 发送邮箱验证邮件
        /// </summary>
        Task<SendVerificationResult> SendEmailVerificationAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 验证邮箱
        /// </summary>
        Task<VerifyEmailResult> VerifyEmailAsync(string token, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 发送手机验证码
        /// </summary>
        Task<SendVerificationResult> SendPhoneVerificationAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 验证手机
        /// </summary>
        Task<VerifyPhoneResult> VerifyPhoneAsync(string code, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 请求密码重置
        /// </summary>
        Task<RequestPasswordResetResult> RequestPasswordResetAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 重置密码
        /// </summary>
        Task<ResetPasswordResult> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 更新用户会话信息
        /// </summary>
        Task UpdateSessionAsync(string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 获取用户会话列表
        /// </summary>
        [ReadOnly]
        Task<List<UserSession>> GetSessionsAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 终止指定会话
        /// </summary>
        Task<TerminateSessionResult> TerminateSessionAsync(string sessionId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 删除用户
        /// </summary>
        Task<DeleteUserResult> DeleteUserAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 终止所有会话（除当前会话外）
        /// </summary>
        Task<TerminateSessionResult> TerminateOtherSessionsAsync(CancellationToken cancellationToken = default);
    }
    
    /// <summary>
    /// 更新用户结果
    /// </summary>
    [GenerateSerializer]
    public class UpdateUserResult
    {
        [Id(0)]
        public bool Success { get; set; }
        [Id(1)]
        public string? ErrorMessage { get; set; }
        [Id(2)]
        public UserDto? UpdatedUser { get; set; }
        
        public static UpdateUserResult CreateSuccess(UserDto user)
            => new() { Success = true, UpdatedUser = user };
        
        public static UpdateUserResult CreateFailed(string errorMessage)
            => new() { Success = false, ErrorMessage = errorMessage };
    }
    
    /// <summary>
    /// 修改密码结果
    /// </summary>
    [GenerateSerializer]
    public class ChangePasswordResult
    {
        [Id(0)]
        public bool Success { get; set; }
        [Id(1)]
        public string? ErrorMessage { get; set; }
        
        public static ChangePasswordResult CreateSuccess()
            => new() { Success = true };
        
        public static ChangePasswordResult CreateFailed(string errorMessage)
            => new() { Success = false, ErrorMessage = errorMessage };
    }
    
    /// <summary>
    /// 发送验证结果
    /// </summary>
    [GenerateSerializer]
    public class SendVerificationResult
    {
        [Id(0)]
        public bool Success { get; set; }
        [Id(1)]
        public string? ErrorMessage { get; set; }
        [Id(2)]
        public bool IsRateLimited { get; set; }
        [Id(3)]
        public int? RetryAfterSeconds { get; set; }
        
        public static SendVerificationResult CreateSuccess()
            => new() { Success = true };
        
        public static SendVerificationResult CreateFailed(string errorMessage)
            => new() { Success = false, ErrorMessage = errorMessage };
        
        public static SendVerificationResult CreateRateLimited(int retryAfterSeconds)
            => new() { Success = false, IsRateLimited = true, RetryAfterSeconds = retryAfterSeconds, ErrorMessage = "请求过于频繁，请稍后再试" };
    }
    
    /// <summary>
    /// 验证邮箱结果
    /// </summary>
    [GenerateSerializer]
    public class VerifyEmailResult
    {
        [Id(0)]
        public bool Success { get; set; }
        [Id(1)]
        public string? ErrorMessage { get; set; }
        [Id(2)]
        public bool IsExpired { get; set; }
        
        public static VerifyEmailResult CreateSuccess()
            => new() { Success = true };
        
        public static VerifyEmailResult CreateFailed(string errorMessage)
            => new() { Success = false, ErrorMessage = errorMessage };
        
        public static VerifyEmailResult CreateExpired()
            => new() { Success = false, IsExpired = true, ErrorMessage = "验证码已过期" };
    }
    
    /// <summary>
    /// 验证手机结果
    /// </summary>
    [GenerateSerializer]
    public class VerifyPhoneResult
    {
        [Id(0)]
        public bool Success { get; set; }
        [Id(1)]
        public string? ErrorMessage { get; set; }
        [Id(2)]
        public bool IsExpired { get; set; }
        [Id(3)]
        public bool IsInvalid { get; set; }
        
        public static VerifyPhoneResult CreateSuccess()
            => new() { Success = true };
        
        public static VerifyPhoneResult CreateFailed(string errorMessage)
            => new() { Success = false, ErrorMessage = errorMessage };
        
        public static VerifyPhoneResult CreateExpired()
            => new() { Success = false, IsExpired = true, ErrorMessage = "验证码已过期" };
        
        public static VerifyPhoneResult CreateInvalid()
            => new() { Success = false, IsInvalid = true, ErrorMessage = "验证码无效" };
    }
    
    /// <summary>
    /// 请求密码重置结果
    /// </summary>
    [GenerateSerializer]
    public class RequestPasswordResetResult
    {
        [Id(0)]
        public bool Success { get; set; }
        [Id(1)]
        public string? ErrorMessage { get; set; }
        [Id(2)]
        public bool IsRateLimited { get; set; }
        
        public static RequestPasswordResetResult CreateSuccess()
            => new() { Success = true };
        
        public static RequestPasswordResetResult CreateFailed(string errorMessage)
            => new() { Success = false, ErrorMessage = errorMessage };
        
        public static RequestPasswordResetResult CreateRateLimited()
            => new() { Success = false, IsRateLimited = true, ErrorMessage = "请求过于频繁，请稍后再试" };
    }
    
    /// <summary>
    /// 重置密码结果
    /// </summary>
    [GenerateSerializer]
    public class ResetPasswordResult
    {
        [Id(0)]
        public bool Success { get; set; }
        [Id(1)]
        public string? ErrorMessage { get; set; }
        [Id(2)]
        public bool IsTokenExpired { get; set; }
        [Id(3)]
        public bool IsTokenInvalid { get; set; }
        
        public static ResetPasswordResult CreateSuccess()
            => new() { Success = true };
        
        public static ResetPasswordResult CreateFailed(string errorMessage)
            => new() { Success = false, ErrorMessage = errorMessage };
        
        public static ResetPasswordResult CreateTokenExpired()
            => new() { Success = false, IsTokenExpired = true, ErrorMessage = "重置令牌已过期" };
        
        public static ResetPasswordResult CreateTokenInvalid()
            => new() { Success = false, IsTokenInvalid = true, ErrorMessage = "重置令牌无效" };
    }
    
    /// <summary>
    /// 终止会话结果
    /// </summary>
    [GenerateSerializer]
    public class TerminateSessionResult
    {
        [Id(0)]
        public bool Success { get; set; }
        [Id(1)]
        public string? ErrorMessage { get; set; }
        [Id(2)]
        public int TerminatedCount { get; set; }
        
        public static TerminateSessionResult CreateSuccess(int terminatedCount = 1)
            => new() { Success = true, TerminatedCount = terminatedCount };
        
        public static TerminateSessionResult CreateFailed(string errorMessage)
            => new() { Success = false, ErrorMessage = errorMessage };
    }
    
    /// <summary>
    /// 删除用户结果
    /// </summary>
    [GenerateSerializer]
    public class DeleteUserResult
    {
        [Id(0)]
        public bool Success { get; set; }
        [Id(1)]
        public string? ErrorMessage { get; set; }
        [Id(2)]
        public bool IsSoftDeleted { get; set; }
        
        public static DeleteUserResult CreateSuccess(bool isSoftDeleted = true)
            => new() { Success = true, IsSoftDeleted = isSoftDeleted };
        
        public static DeleteUserResult CreateFailed(string errorMessage)
            => new() { Success = false, ErrorMessage = errorMessage };
    }

    /// <summary>
    /// 用户服务Grain接口
    /// </summary>
    public interface IUserServiceGrain : IGrainWithIntegerKey
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
        /// 根据用户名或邮箱查找用户
        /// </summary>
        Task<long?> FindUserByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 验证用户权限
        /// </summary>
        Task<bool> ValidateUserPermissionAsync(long userId, string resource, string permissionType, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 获取用户统计信息
        /// </summary>
        Task<UserStatistics> GetUserStatisticsAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 获取用户列表（支持搜索）
        /// </summary>
        Task<List<UserDto>> GetUsersAsync(string? username = null, string? email = null, string? status = null, CancellationToken cancellationToken = default);

    }

    /// <summary>
    /// 用户统计信息
    /// </summary>
    [GenerateSerializer]
    public class UserStatistics
    {
        [Id(0)]
        public int TotalUsers { get; set; }
        [Id(1)]
        public int ActiveUsers { get; set; }
        [Id(2)]
        public int NewUsersToday { get; set; }
        [Id(3)]
        public Dictionary<string, int> RoleDistribution { get; set; } = new();
        [Id(4)]
        public Dictionary<string, int> StatusDistribution { get; set; } = new();
    }

    /// <summary>
    /// 用户会话信息
    /// </summary>
    [GenerateSerializer]
    public class UserSession
    {
        [Id(0)]
        public string SessionId { get; set; } = string.Empty;
        [Id(1)]
        public DateTime LoginTime { get; set; }
        [Id(2)]
        public DateTime? LastActivity { get; set; }
        [Id(3)]
        public string? IpAddress { get; set; }
        [Id(4)]
        public string? UserAgent { get; set; }
        [Id(5)]
        public bool IsCurrent { get; set; }
    }

    /// <summary>
    /// 用户资料更新请求
    /// </summary>
    [GenerateSerializer]
    public class UserProfileUpdateRequest
    {
        [Id(0)]
        public string? DisplayName { get; set; }
        [Id(1)]
        public string? Phone { get; set; }
        [Id(2)]
        public string? AvatarUrl { get; set; }
        [Id(3)]
        public string? Bio { get; set; }
        [Id(4)]
        public string PhoneNumber { get; set; }
    }
}