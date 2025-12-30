using FakeMicro.Entities.Enums;
using Orleans;

namespace FakeMicro.Interfaces.Models
{
    /// <summary>
    /// 用户注册请求
    /// </summary>
    [GenerateSerializer]
    public class RegisterRequest
    {
        [Id(0)]
        public string Username { get; set; } = string.Empty;
        [Id(1)]
        public string Email { get; set; } = string.Empty;
        [Id(2)]
        public string Password { get; set; } = string.Empty;
        [Id(3)]
        public string? Phone { get; set; }
        [Id(4)]
        public string? DisplayName { get; set; }

    }

    /// <summary>
    /// 用户登录请求
    /// </summary>
    [GenerateSerializer]
    public class LoginRequest
    {
        [Id(0)]
        public string UsernameOrEmail { get; set; } = string.Empty;
        [Id(1)]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// 认证响应
    /// </summary>
    [GenerateSerializer]
    public class AuthResponse
    {
        [Id(0)]
        public bool Success { get; set; }
        [Id(1)]
        public string? Token { get; set; }
        [Id(2)]
        public string? RefreshToken { get; set; }
        [Id(3)]
        public DateTime? ExpiresAt { get; set; }
        [Id(4)]
        public UserDto? User { get; set; }
        [Id(5)]
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 用户信息DTO
    /// </summary>
    [GenerateSerializer]
    public class UserDto
    {
        [Id(0)]
        public long Id { get; set; }
        [Id(1)]
        public string Username { get; set; } = string.Empty;
        [Id(2)]
        public string Email { get; set; } = string.Empty;
        [Id(3)]
        public string? Phone { get; set; }
        [Id(4)]
        public string? DisplayName { get; set; }
        [Id(5)]
        public UserRole Role { get; set; }
        [Id(6)]
        public UserStatus Status { get; set; }
        [Id(7)]
        public DateTime CreatedAt { get; set; }
        [Id(8)]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 刷新令牌请求
    /// </summary>
    [GenerateSerializer]
    public class RefreshTokenRequest
    {
        [Id(0)]
        public string Token { get; set; } = string.Empty;
        [Id(1)]
        public string RefreshToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// 修改密码请求
    /// </summary>
    [GenerateSerializer]
    public class ChangePasswordRequest
    {
        [Id(0)]
        public string CurrentPassword { get; set; } = string.Empty;
        [Id(1)]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// 权限信息
    /// </summary>
    [GenerateSerializer]
    public class Permission
    {
        [Id(0)]
        public string Resource { get; set; } = string.Empty;
        [Id(1)]
        public PermissionType Type { get; set; }
    }

    /// <summary>
    /// 角色权限
    /// </summary>
    [GenerateSerializer]
    public class RolePermission
    {
        [Id(0)]
        public UserRole Role { get; set; }
        [Id(1)]
        public List<Permission> Permissions { get; set; } = new();
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
        public Dictionary<string, int> StatusDistribution { get; set; } = new();
        [Id(4)]
        public Dictionary<string, int> RoleDistribution { get; set; } = new();
    }
}