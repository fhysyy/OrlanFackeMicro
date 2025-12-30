using FakeMicro.Interfaces;
using FakeMicro.Entities;
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FakeMicro.Grains.States
{
    /// <summary>
    /// 用户Grain状态 - Orleans状态管理的最佳实践
    /// </summary>
    public class UserState
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        [JsonPropertyOrder(0)]
        public long UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        [JsonPropertyOrder(1)]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 邮箱
        /// </summary>
        [JsonPropertyOrder(2)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 手机号
        /// </summary>
        [JsonPropertyOrder(3)]
        public string? Phone { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        [JsonPropertyOrder(4)]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 用户头像URL
        /// </summary>
        [JsonPropertyOrder(5)]
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// 用户角色
        /// </summary>
        [JsonPropertyOrder(6)]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// 用户状态
        /// </summary>
        [JsonPropertyOrder(6)]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// 最后登录时间
        /// </summary>
        [JsonPropertyOrder(7)]
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// 是否已激活
        /// </summary>
        [JsonPropertyOrder(8)]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 邮箱是否已验证
        /// </summary>
        [JsonPropertyOrder(9)]
        public bool EmailVerified { get; set; }

        /// <summary>
        /// 邮箱是否已验证（用于兼容UserGrain中的使用）
        /// </summary>
        [JsonIgnore]
        public bool IsEmailVerified { get => EmailVerified; set => EmailVerified = value; }

        /// <summary>
        /// 手机是否已验证
        /// </summary>
        [JsonPropertyOrder(10)]
        public bool PhoneVerified { get; set; }

        /// <summary>
        /// 手机是否已验证（用于兼容UserGrain中的使用）
        /// </summary>
        [JsonIgnore]
        public bool IsPhoneVerified { get => PhoneVerified; set => PhoneVerified = value; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [JsonPropertyOrder(11)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 更新时间
        /// </summary>
        [JsonPropertyOrder(12)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 用户会话列表
        /// </summary>
        [JsonPropertyOrder(13)]
        public List<UserSession> Sessions { get; set; } = new();

        /// <summary>
        /// 用户权限列表
        /// </summary>
        [JsonPropertyOrder(14)]
        public List<UserPermission> Permissions { get; set; } = new();

        /// <summary>
        /// 用户好友列表
        /// </summary>
        [JsonPropertyOrder(15)]
        public Dictionary<long, DateTime> Friends { get; set; } = new();

        /// <summary>
        /// 被阻止的用户列表
        /// </summary>
        [JsonPropertyOrder(16)]
        public Dictionary<long, DateTime> BlockedUsers { get; set; } = new();

        /// <summary>
        /// 用户设置
        /// </summary>
        [JsonPropertyOrder(17)]
        public UserSettings Settings { get; set; } = new();

        /// <summary>
        /// 当前有效的刷新令牌
        /// </summary>
        [JsonPropertyOrder(15)]
        public string? CurrentRefreshToken { get; set; }

        /// <summary>
        /// 刷新令牌（用于兼容UserGrain中的使用）
        /// </summary>
        [JsonIgnore]
        public string? RefreshToken { get => CurrentRefreshToken; set => CurrentRefreshToken = value; }

        /// <summary>
        /// 刷新令牌过期时间
        /// </summary>
        [JsonPropertyOrder(16)]
        public DateTime? RefreshTokenExpiresAt { get; set; }

        /// <summary>
        /// 刷新令牌过期时间（用于兼容UserGrain中的使用）
        /// </summary>
        [JsonIgnore]
        public DateTime? RefreshTokenExpiry { get => RefreshTokenExpiresAt; set => RefreshTokenExpiresAt = value; }

        /// <summary>
        /// 状态最后修改时间
        /// </summary>
        [JsonIgnore]
        [JsonPropertyOrder(19)]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        [JsonIgnore]
        [JsonPropertyOrder(17)]
        public string PasswordHash { get; set; } = string.Empty;
        [JsonIgnore]
        [JsonPropertyOrder(18)]
        public string PasswordSalt { get; set; } = string.Empty;

        /// <summary>
        /// 状态版本（用于乐观并发控制）
        /// </summary>
        [JsonIgnore]
        public int Version { get; set; } = 0;

        /// <summary>
        /// 状态是否已加载
        /// </summary>
        [JsonIgnore]
        [JsonPropertyOrder(15)]
        public bool IsLoaded { get; set; } = false;



        /// <summary>
        /// 检查状态是否有效
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Username) && 
                   !string.IsNullOrEmpty(Email) &&
                   UserId > 0;
        }

        /// <summary>
        /// 创建状态的深拷贝
        /// </summary>
        public UserState DeepCopy()
        {
            var copy = new UserState
            {
                UserId = this.UserId,
                Username = this.Username,
                Email = this.Email,
                Phone = this.Phone,
                DisplayName = this.DisplayName,
                Role = this.Role,
                Status = this.Status,
                LastLoginAt = this.LastLoginAt,
                IsActive = this.IsActive,
                EmailVerified = this.EmailVerified,
                PhoneVerified = this.PhoneVerified,
                CreatedAt = this.CreatedAt,
                UpdatedAt = this.UpdatedAt,
                Version = this.Version,
                IsLoaded = this.IsLoaded,
                LastModified = this.LastModified,
                // 深拷贝列表和字典
                Sessions = this.Sessions.Select(s => new UserSession
                {
                    SessionId = s.SessionId,
                    LoginTime = s.LoginTime,
                    LastActivity = s.LastActivity,
                    IpAddress = s.IpAddress,
                    UserAgent = s.UserAgent,
                    IsCurrent = s.IsCurrent
                }).ToList(),
                Permissions = this.Permissions.Select(p => new UserPermission
                {
                    Resource = p.Resource,
                    Type = p.Type,
                    GrantedAt = p.GrantedAt
                }).ToList(),
                // 深拷贝字典
                Friends = new Dictionary<long, DateTime>(this.Friends),
                BlockedUsers = new Dictionary<long, DateTime>(this.BlockedUsers),
                // 深拷贝设置
                Settings = new UserSettings
                {
                    Notifications = new NotificationSettings
                    {
                        EmailEnabled = this.Settings.Notifications.EmailEnabled,
                        SmsEnabled = this.Settings.Notifications.SmsEnabled,
                        PushEnabled = this.Settings.Notifications.PushEnabled
                    },
                    Privacy = new PrivacySettings
                    {
                        ShowEmail = this.Settings.Privacy.ShowEmail,
                        ShowPhone = this.Settings.Privacy.ShowPhone,
                        AllowFriendRequests = this.Settings.Privacy.AllowFriendRequests
                    },
                    Theme = new ThemeSettings
                    {
                        ThemeName = this.Settings.Theme.ThemeName,
                        AutoTheme = this.Settings.Theme.AutoTheme
                    }
                }
            };
            return copy;
        }

        /// <summary>
        /// 更新状态版本
        /// </summary>
        public void IncrementVersion()
        {
            Version++;
            LastModified = DateTime.UtcNow;
        }

        /// <summary>
        /// 添加用户会话
        /// </summary>
        public void AddSession(UserSession session)
        {
            Sessions.Add(session);
            
            // 限制会话数量
            if (Sessions.Count > 10)
            {
                Sessions.RemoveAt(0);
            }
            
            IncrementVersion();
        }

        /// <summary>
        /// 移除用户会话
        /// </summary>
        public bool RemoveSession(string sessionId)
        {
            var session = Sessions.FirstOrDefault(s => s.SessionId == sessionId);
            if (session != null)
            {
                Sessions.Remove(session);
                IncrementVersion();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 更新用户信息
        /// </summary>
        public void UpdateUserInfo(string username, string email, string displayName, string role)
        {
            Username = username;
            Email = email;
            DisplayName = displayName;
            Role = role;
            UpdatedAt = DateTime.UtcNow;
            IncrementVersion();
        }

        /// <summary>
        /// 更新登录信息
        /// </summary>
        public void UpdateLoginInfo(bool success)
        {
            if (success)
            {
                LastLoginAt = DateTime.UtcNow;
                UpdatedAt = DateTime.UtcNow;
                IncrementVersion();
            }
        }

        /// <summary>
        /// 重置状态（清除所有用户数据）
        /// </summary>
        public void Reset()
        {
            UserId = 0;
            Username = string.Empty;
            Email = string.Empty;
            Phone = null;
            DisplayName = string.Empty;
            Role = string.Empty;
            Status = string.Empty;
            LastLoginAt = null;
            IsActive = true;
            EmailVerified = false;
            PhoneVerified = false;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            Sessions.Clear();
            Permissions.Clear();
            Friends.Clear();
            BlockedUsers.Clear();
            Settings = new UserSettings();
            // 重置刷新令牌
            CurrentRefreshToken = null;
            RefreshTokenExpiresAt = null;
            IsLoaded = false;
            LastModified = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 用户权限信息
    /// </summary>
    public class UserPermission
    {
        public string Resource { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 用户设置
    /// </summary>
    public class UserSettings
    {
        /// <summary>
        /// 通知设置
        /// </summary>
        public NotificationSettings Notifications { get; set; } = new();

        /// <summary>
        /// 隐私设置
        /// </summary>
        public PrivacySettings Privacy { get; set; } = new();

        /// <summary>
        /// 主题设置
        /// </summary>
        public ThemeSettings Theme { get; set; } = new();
    }

    /// <summary>
    /// 通知设置
    /// </summary>
    public class NotificationSettings
    {
        public bool EmailEnabled { get; set; } = true;
        public bool SmsEnabled { get; set; } = false;
        public bool PushEnabled { get; set; } = true;
    }

    /// <summary>
    /// 隐私设置
    /// </summary>
    public class PrivacySettings
    {
        public bool ShowEmail { get; set; } = false;
        public bool ShowPhone { get; set; } = false;
        public bool AllowFriendRequests { get; set; } = true;
    }

    /// <summary>
    /// 主题设置
    /// </summary>
    public class ThemeSettings
    {
        public string ThemeName { get; set; } = "light";
        public bool AutoTheme { get; set; } = true;
    }
}