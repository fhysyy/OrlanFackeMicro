using FakeMicro.Entities;
using FakeMicro.Entities.Enums;
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
        /// 设置昵称
        /// </summary>
        Task SetNicknameAsync(string nickname);
        
        /// <summary>
        /// 获取昵称
        /// </summary>
        [ReadOnly]
        Task<string> GetNicknameAsync();
        
        /// <summary>
        /// 设置头像
        /// </summary>
        Task SetAvatarAsync(string avatarUrl);
        
        /// <summary>
        /// 获取头像
        /// </summary>
        [ReadOnly]
        Task<string> GetAvatarAsync();
        
        /// <summary>
        /// 获取个人资料
        /// </summary>
        [ReadOnly]
        Task<Dictionary<string, object>> GetProfileAsync();
        
        /// <summary>
        /// 更新个人资料
        /// </summary>
        Task UpdateProfileAsync(Dictionary<string, object> profile);
        
        /// <summary>
        /// 创建用户会话
        /// </summary>
        Task CreateSessionAsync(UserSession session);
        
        /// <summary>
        /// 获取用户权限
        /// </summary>
        [ReadOnly]
        Task<List<Permission>> GetPermissionsAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 添加用户权限
        /// </summary>
        Task AddPermissionAsync(Permission permission);
        
        /// <summary>
        /// 移除用户权限
        /// </summary>
        Task RemovePermissionAsync(string resource, string permissionType);
        
        /// <summary>
        /// 检查用户是否有权限
        /// </summary>
        [ReadOnly]
        Task<bool> HasPermissionAsync(string resource, string permissionType, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 删除用户
        /// </summary>
        Task DeleteAsync();
        
        /// <summary>
        /// 设置用户状态
        /// </summary>
        Task SetStatusAsync(UserStatus status);
        
        /// <summary>
        /// 获取用户状态
        /// </summary>
        [ReadOnly]
        Task<UserStatus> GetStatusAsync();
        
        /// <summary>
        /// 获取用户邮箱
        /// </summary>
        [ReadOnly]
        Task<string> GetEmailAsync();
        
        /// <summary>
        /// 设置用户邮箱
        /// </summary>
        Task SetEmailAsync(string email);
        
        /// <summary>
        /// 设置密码哈希
        /// </summary>
        Task SetPasswordAsync(string passwordHash);
        
        /// <summary>
        /// 获取密码哈希
        /// </summary>
        [ReadOnly]
        Task<string> GetPasswordAsync();
        
        /// <summary>
        /// 获取最后登录时间
        /// </summary>
        [ReadOnly]
        Task<DateTime> GetLastLoginAsync();
        
        /// <summary>
        /// 设置最后登录时间
        /// </summary>
        Task SetLastLoginAsync(DateTime lastLogin);
        
        /// <summary>
        /// 更新在线状态
        /// </summary>
        Task UpdateOnlineStatusAsync(bool isOnline);
        
        /// <summary>
        /// 检查用户是否在线
        /// </summary>
        [ReadOnly]
        Task<bool> IsOnlineAsync();
        
        /// <summary>
        /// 获取用户好友列表
        /// </summary>
        [ReadOnly]
        Task<List<long>> GetFriendsAsync();
        
        /// <summary>
        /// 添加好友
        /// </summary>
        Task AddFriendAsync(long friendUserId);
        
        /// <summary>
        /// 移除好友
        /// </summary>
        Task RemoveFriendAsync(long friendUserId);
        
        /// <summary>
        /// 检查是否为好友
        /// </summary>
        [ReadOnly]
        Task<bool> IsFriendAsync(long friendUserId);
        
        /// <summary>
        /// 获取被屏蔽的用户列表
        /// </summary>
        [ReadOnly]
        Task<List<long>> GetBlockedUsersAsync();
        
        /// <summary>
        /// 屏蔽用户
        /// </summary>
        Task BlockUserAsync(long userId);
        
        /// <summary>
        /// 取消屏蔽用户
        /// </summary>
        Task UnblockUserAsync(long userId);
        
        /// <summary>
        /// 检查用户是否被屏蔽
        /// </summary>
        [ReadOnly]
        Task<bool> IsBlockedAsync(long userId);
        
        /// <summary>
        /// 获取用户设置
        /// </summary>
        [ReadOnly]
        Task<Dictionary<string, string>> GetSettingsAsync();
        
        /// <summary>
        /// 更新用户设置
        /// </summary>
        Task UpdateSettingAsync(string key, string value);
        
        /// <summary>
        /// 删除用户设置
        /// </summary>
        Task DeleteSettingAsync(string key);
        
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
        
        public static DeleteUserResult CreateSuccess()
            => new() { Success = true };
        
        public static DeleteUserResult CreateFailed(string errorMessage)
            => new() { Success = false, ErrorMessage = errorMessage };
    }
}