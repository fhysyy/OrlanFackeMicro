using Orleans;
using Orleans.Storage;
using Orleans.Serialization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Security.Cryptography;
using System.Text;
using FakeMicro.Shared.Exceptions;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;

namespace FakeMicro.Grains
{
    public interface IUserGrain : IGrainWithStringKey
    {
        Task SetNicknameAsync(string nickname);
        Task<string> GetNicknameAsync();
        Task SetAvatarAsync(string avatarUrl);
        Task<string> GetAvatarAsync();
        Task<Dictionary<string, object>> GetProfileAsync();
        Task UpdateProfileAsync(Dictionary<string, object> profile);
        Task<List<UserSession>> GetSessionsAsync(CancellationToken cancellationToken = default);
        Task CreateSessionAsync(UserSession session);
        Task TerminateSessionAsync(string sessionId);
        Task<List<Permission>> GetPermissionsAsync(CancellationToken cancellationToken = default);
        Task AddPermissionAsync(Permission permission);
        Task RemovePermissionAsync(string resource, string permissionType);
        Task<bool> HasPermissionAsync(string resource, string permissionType, CancellationToken cancellationToken = default);
        Task DeleteAsync();
        Task SetStatusAsync(UserStatus status);
        Task<UserStatus> GetStatusAsync();
        Task<string> GetEmailAsync();
        Task SetEmailAsync(string email);
        Task SetPasswordAsync(string passwordHash);
        Task<string> GetPasswordAsync();
        Task<DateTime> GetLastLoginAsync();
        Task SetLastLoginAsync(DateTime lastLogin);
        Task UpdateOnlineStatusAsync(bool isOnline);
        Task<bool> IsOnlineAsync();
        Task<List<string>> GetFriendsAsync();
        Task AddFriendAsync(string friendUserId);
        Task RemoveFriendAsync(string friendUserId);
        Task<bool> IsFriendAsync(string friendUserId);
        Task<List<string>> GetBlockedUsersAsync();
        Task BlockUserAsync(string userId);
        Task UnblockUserAsync(string userId);
        Task<bool> IsBlockedAsync(string userId);
        Task<Dictionary<string, string>> GetSettingsAsync();
        Task UpdateSettingAsync(string key, string value);
        Task DeleteSettingAsync(string key);
    }

    public class UserGrain : OrleansGrainBase, IUserGrain
    {
        #region 状态管理

        /// <summary>
        /// 用户状态存储
        /// </summary>
        private readonly IPersistentState<UserState> _userState;

        /// <summary>
        /// 当前用户状态
        /// </summary>
        private UserState State => _userState.State;

        /// <summary>
        /// 状态是否有效
        /// </summary>
        private bool IsStateValid => State != null;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="userState">用户状态存储</param>
        /// <param name="logger">日志记录器</param>
        public UserGrain(
            [PersistentState("UserState", "UserStateStore")] IPersistentState<UserState> userState,
            ILogger<UserGrain> logger)
            : base(logger)
        {
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
        }

        #endregion

        #region 生命周期方法

        /// <summary>
        /// Grain 激活时的初始化逻辑
        /// </summary>
        public override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await base.OnActivateAsync(cancellationToken);
            await LoadUserDataAsync();
        }

        /// <summary>
        /// 初始化Grain
        /// </summary>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            await LoadUserDataAsync();
        }

        #endregion

        #region 数据加载与初始化

        /// <summary>
        /// 从持久化存储加载用户数据
        /// </summary>
        private async Task LoadUserDataAsync()
        {
            try
            {
                await _userState.ReadStateAsync();

                if (!IsStateValid)
                {
                    // 初始化默认状态
                    LogInformation("初始化新用户状态: {UserId}", this.GetPrimaryKeyString());
                    _userState.State = new UserState
                    {
                        UserId = this.GetPrimaryKeyString(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Status = UserStatus.Offline,
                        Profile = new Dictionary<string, object>(),
                        Sessions = new List<UserSession>(),
                        Permissions = new List<Permission>(),
                        Settings = new Dictionary<string, string>(),
                        Friends = new List<string>(),
                        BlockedUsers = new List<string>()
                    };
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "加载用户数据失败: {UserId}", this.GetPrimaryKeyString());
                throw new PersistenceException($"加载用户数据失败: {this.GetPrimaryKeyString()}", ex);
            }
        }

        /// <summary>
        /// 保存用户状态
        /// </summary>
        private async Task SaveUserStateAsync()
        {
            try
            {
                if (IsStateValid)
                {
                    State.UpdatedAt = DateTime.UtcNow;
                    await _userState.WriteStateAsync();
                    LogInformation("保存用户状态成功: {UserId}", this.GetPrimaryKeyString());
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "保存用户状态失败: {UserId}", this.GetPrimaryKeyString());
                throw new PersistenceException($"保存用户状态失败: {this.GetPrimaryKeyString()}", ex);
            }
        }

        #endregion

        #region 个人资料相关方法

        /// <summary>
        /// 设置用户昵称
        /// </summary>
        public async Task SetNicknameAsync(string nickname)
        {
            await SafeExecuteAsync("SetNicknameAsync", async () =>
            {
                if (string.IsNullOrWhiteSpace(nickname))
                {
                    throw new ArgumentException("昵称不能为空", nameof(nickname));
                }

                State.Profile["nickname"] = nickname;
                await SaveUserStateAsync();
            }, nickname);
        }

        /// <summary>
        /// 获取用户昵称
        /// </summary>
        public async Task<string> GetNicknameAsync()
        {
            return await SafeExecuteAsync("GetNicknameAsync", async () =>
            {
                return State.Profile.TryGetValue("nickname", out var nickname) ? nickname?.ToString() : null;
            });
        }

        /// <summary>
        /// 设置用户头像
        /// </summary>
        public async Task SetAvatarAsync(string avatarUrl)
        {
            await SafeExecuteAsync("SetAvatarAsync", async () =>
            {
                State.Profile["avatar"] = avatarUrl;
                await SaveUserStateAsync();
            }, avatarUrl);
        }

        /// <summary>
        /// 获取用户头像
        /// </summary>
        public async Task<string> GetAvatarAsync()
        {
            return await SafeExecuteAsync("GetAvatarAsync", async () =>
            {
                return State.Profile.TryGetValue("avatar", out var avatar) ? avatar?.ToString() : null;
            });
        }

        /// <summary>
        /// 获取用户资料
        /// </summary>
        public async Task<Dictionary<string, object>> GetProfileAsync()
        {
            return await SafeExecuteAsync("GetProfileAsync", async () =>
            {
                return new Dictionary<string, object>(State.Profile);
            });
        }

        /// <summary>
        /// 更新用户资料
        /// </summary>
        public async Task UpdateProfileAsync(Dictionary<string, object> profile)
        {
            await SafeExecuteAsync("UpdateProfileAsync", async () =>
            {
                if (profile == null)
                {
                    throw new ArgumentNullException(nameof(profile));
                }

                foreach (var kvp in profile)
                {
                    State.Profile[kvp.Key] = kvp.Value;
                }
                await SaveUserStateAsync();
            }, profile.Keys.FirstOrDefault());
        }

        #endregion

        #region 会话管理

        /// <summary>
        /// 获取所有会话
        /// </summary>
        public async Task<List<UserSession>> GetSessionsAsync(CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync("GetSessionsAsync", async () =>
            {
                return new List<UserSession>(State.Sessions);
            });
        }

        /// <summary>
        /// 创建新会话
        /// </summary>
        public async Task CreateSessionAsync(UserSession session)
        {
            await SafeExecuteAsync("CreateSessionAsync", async () =>
            {
                if (session == null)
                {
                    throw new ArgumentNullException(nameof(session));
                }

                if (string.IsNullOrWhiteSpace(session.SessionId))
                {
                    session.SessionId = Guid.NewGuid().ToString();
                }

                session.LoginTime = DateTime.UtcNow;
                session.LastActivity = DateTime.UtcNow;
                State.Sessions.Add(session);
                await SaveUserStateAsync();
            }, session.SessionId);
        }

        /// <summary>
        /// 终止会话
        /// </summary>
        public async Task TerminateSessionAsync(string sessionId)
        {
            await SafeExecuteAsync("TerminateSessionAsync", async () =>
            {
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    throw new ArgumentException("会话ID不能为空", nameof(sessionId));
                }

                var session = State.Sessions.FirstOrDefault(s => s.SessionId == sessionId);
                if (session != null)
                {
                    State.Sessions.Remove(session);
                    await SaveUserStateAsync();
                }
            }, sessionId);
        }

        #endregion

        #region 权限管理

        /// <summary>
        /// 获取所有权限
        /// </summary>
        public async Task<List<Permission>> GetPermissionsAsync(CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync("GetPermissionsAsync", async () =>
            {
                return new List<Permission>(State.Permissions);
            });
        }

        /// <summary>
        /// 添加权限
        /// </summary>
        public async Task AddPermissionAsync(Permission permission)
        {
            await SafeExecuteAsync("AddPermissionAsync", async () =>
            {
                if (permission == null)
                {
                    throw new ArgumentNullException(nameof(permission));
                }

                if (!State.Permissions.Any(p => p.Resource == permission.Resource && p.Type == permission.Type))
                {
                    State.Permissions.Add(permission);
                    await SaveUserStateAsync();
                }
            }, permission.Resource, permission.Type.ToString());
        }

        /// <summary>
        /// 移除权限
        /// </summary>
        public async Task RemovePermissionAsync(string resource, string permissionType)
        {
            await SafeExecuteAsync("RemovePermissionAsync", async () =>
            {
                if (string.IsNullOrWhiteSpace(resource) || string.IsNullOrWhiteSpace(permissionType))
                {
                    throw new ArgumentException("资源或权限类型不能为空", nameof(resource));
                }

                var permission = State.Permissions.FirstOrDefault(p => p.Resource == resource && p.Type.ToString() == permissionType);
                if (permission != null)
                {
                    State.Permissions.Remove(permission);
                    await SaveUserStateAsync();
                }
            }, resource, permissionType);
        }

        /// <summary>
        /// 检查是否有权限
        /// </summary>
        public async Task<bool> HasPermissionAsync(string resource, string permissionType, CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync("HasPermissionAsync", async () =>
            {
                if (string.IsNullOrWhiteSpace(resource) || string.IsNullOrWhiteSpace(permissionType))
                {
                    return false;
                }
                return State.Permissions.Any(p => p.Resource == resource && p.Type.ToString() == permissionType);
            }, resource, permissionType);
        }

        #endregion

        #region 用户管理

        /// <summary>
        /// 删除用户
        /// </summary>
        public async Task DeleteAsync()
        {
            await SafeExecuteAsync("DeleteAsync", async () =>
            {
                try
                {
                    await _userState.ClearStateAsync();
                    LogInformation("用户已删除: {UserId}", this.GetPrimaryKeyString());
                }
                catch (Exception ex)
                {
                    LogError(ex, "删除用户失败: {UserId}", this.GetPrimaryKeyString());
                    throw new PersistenceException($"删除用户失败: {this.GetPrimaryKeyString()}", ex);
                }
            });
        }

        #endregion

        #region 状态管理

        /// <summary>
        /// 设置用户状态
        /// </summary>
        public async Task SetStatusAsync(UserStatus status)
        {
            await SafeExecuteAsync("SetStatusAsync", async () =>
            {
                State.Status = status;
                await SaveUserStateAsync();
            }, status.ToString());
        }

        /// <summary>
        /// 获取用户状态
        /// </summary>
        public async Task<UserStatus> GetStatusAsync()
        {
            return await SafeExecuteAsync("GetStatusAsync", async () =>
            {
                return State.Status;
            });
        }

        /// <summary>
        /// 获取用户邮箱
        /// </summary>
        public async Task<string> GetEmailAsync()
        {
            return await SafeExecuteAsync("GetEmailAsync", async () =>
            {
                return State.Profile.TryGetValue("email", out var email) ? email?.ToString() : null;
            });
        }

        /// <summary>
        /// 设置用户邮箱
        /// </summary>
        public async Task SetEmailAsync(string email)
        {
            await SafeExecuteAsync("SetEmailAsync", async () =>
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    throw new ArgumentException("邮箱不能为空", nameof(email));
                }

                State.Profile["email"] = email;
                await SaveUserStateAsync();
            }, email);
        }

        /// <summary>
        /// 设置用户密码
        /// </summary>
        /// <summary>
        /// 设置用户密码（内部使用，不推荐直接调用）
        /// </summary>
        public async Task SetPasswordAsync(string passwordHash)
        {
            await SafeExecuteAsync("SetPasswordAsync", async () =>
            {
                if (string.IsNullOrWhiteSpace(passwordHash))
                {
                    throw new ArgumentException("密码哈希不能为空", nameof(passwordHash));
                }

                State.PasswordHash = passwordHash;
                await SaveUserStateAsync();
            }, "[密码哈希]");
        }

        /// <summary>
        /// 设置用户密码哈希和盐值
        /// </summary>
        public async Task SetPasswordHashAndSaltAsync(string passwordHash, string passwordSalt)
        {
            await SafeExecuteAsync("SetPasswordHashAndSaltAsync", async () =>
            {
                if (string.IsNullOrWhiteSpace(passwordHash))
                {
                    throw new ArgumentException("密码哈希不能为空", nameof(passwordHash));
                }

                if (string.IsNullOrWhiteSpace(passwordSalt))
                {
                    throw new ArgumentException("密码盐值不能为空", nameof(passwordSalt));
                }

                State.PasswordHash = passwordHash;
                State.PasswordSalt = passwordSalt;
                await SaveUserStateAsync();
            }, "[密码哈希和盐值]");
        }

        /// <summary>
        /// 获取用户密码哈希
        /// </summary>
        public async Task<string> GetPasswordAsync()
        {
            return await SafeExecuteAsync("GetPasswordAsync", async () =>
            {
                return State.PasswordHash;
            });
        }

        /// <summary>
        /// 验证密码
        /// </summary>
        public async Task<bool> ValidatePasswordAsync(string password, CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync("ValidatePasswordAsync", async () =>
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(State.PasswordHash) || string.IsNullOrWhiteSpace(State.PasswordSalt))
                {
                    return false;
                }

                try
                {
                    using var hmac = new HMACSHA512(Convert.FromBase64String(State.PasswordSalt));
                    var computedHash = Convert.ToBase64String(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
                    return SecureCompareHash(computedHash, State.PasswordHash);
                }
                catch
                {
                    return false;
                }
            }, "[密码验证]");
        }

        /// <summary>
        /// 修改密码
        /// </summary>
        public async Task<ChangePasswordResult> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync("ChangePasswordAsync", async () =>
            {
                if (string.IsNullOrWhiteSpace(currentPassword))
                {
                    return ChangePasswordResult.CreateFailed("当前密码不能为空");
                }

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    return ChangePasswordResult.CreateFailed("新密码不能为空");
                }

                // 验证当前密码
                if (!await ValidatePasswordAsync(currentPassword))
                {
                    return ChangePasswordResult.CreateFailed("当前密码不正确");
                }

                // 生成新密码哈希和盐值
                GeneratePasswordHash(newPassword, out string newPasswordHash, out string newPasswordSalt);

                // 更新密码
                State.PasswordHash = newPasswordHash;
                State.PasswordSalt = newPasswordSalt;
                await SaveUserStateAsync();

                return ChangePasswordResult.CreateSuccess();
            }, "[修改密码]");
        }

        /// <summary>
        /// 生成密码哈希
        /// </summary>
        private void GeneratePasswordHash(string password, out string hash, out string salt)
        {
            using var hmac = new HMACSHA512();
            salt = Convert.ToBase64String(hmac.Key);
            hash = Convert.ToBase64String(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
        }

        /// <summary>
        /// 安全比较哈希值，防止计时攻击
        /// </summary>
        private bool SecureCompareHash(string hash1, string hash2)
        {
            if (hash1.Length != hash2.Length)
            {
                return false;
            }

            bool result = true;
            for (int i = 0; i < hash1.Length; i++)
            {
                result &= (hash1[i] == hash2[i]);
            }

            return result;
        }

        /// <summary>
        /// 获取最后登录时间
        /// </summary>
        public async Task<DateTime> GetLastLoginAsync()
        {
            return await SafeExecuteAsync("GetLastLoginAsync", async () =>
            {
                return State.LastLogin;
            });
        }

        /// <summary>
        /// 设置最后登录时间
        /// </summary>
        public async Task SetLastLoginAsync(DateTime lastLogin)
        {
            await SafeExecuteAsync("SetLastLoginAsync", async () =>
            {
                State.LastLogin = lastLogin;
                await SaveUserStateAsync();
            }, lastLogin.ToString());
        }

        /// <summary>
        /// 更新在线状态
        /// </summary>
        public async Task UpdateOnlineStatusAsync(bool isOnline)
        {
            await SafeExecuteAsync("UpdateOnlineStatusAsync", async () =>
            {
                State.Status = isOnline ? UserStatus.Online : UserStatus.Offline;
                await SaveUserStateAsync();
            }, isOnline.ToString());
        }

        /// <summary>
        /// 检查用户是否在线
        /// </summary>
        public async Task<bool> IsOnlineAsync()
        {
            return await SafeExecuteAsync("IsOnlineAsync", async () =>
            {
                return State.Status == UserStatus.Online;
            });
        }

        #endregion

        #region 社交关系

        /// <summary>
        /// 获取好友列表
        /// </summary>
        public async Task<List<string>> GetFriendsAsync()
        {
            return await SafeExecuteAsync("GetFriendsAsync", async () =>
            {
                return new List<string>(State.Friends);
            });
        }

        /// <summary>
        /// 添加好友
        /// </summary>
        public async Task AddFriendAsync(string friendUserId)
        {
            await SafeExecuteAsync("AddFriendAsync", async () =>
            {
                if (string.IsNullOrWhiteSpace(friendUserId))
                {
                    throw new ArgumentException("好友用户ID不能为空", nameof(friendUserId));
                }

                if (!State.Friends.Contains(friendUserId))
                {
                    State.Friends.Add(friendUserId);
                    await SaveUserStateAsync();
                }
            }, friendUserId);
        }

        /// <summary>
        /// 移除好友
        /// </summary>
        public async Task RemoveFriendAsync(string friendUserId)
        {
            await SafeExecuteAsync("RemoveFriendAsync", async () =>
            {
                if (string.IsNullOrWhiteSpace(friendUserId))
                {
                    throw new ArgumentException("好友用户ID不能为空", nameof(friendUserId));
                }

                if (State.Friends.Contains(friendUserId))
                {
                    State.Friends.Remove(friendUserId);
                    await SaveUserStateAsync();
                }
            }, friendUserId);
        }

        /// <summary>
        /// 检查是否是好友
        /// </summary>
        public async Task<bool> IsFriendAsync(string friendUserId)
        {
            return await SafeExecuteAsync("IsFriendAsync", async () =>
            {
                if (string.IsNullOrWhiteSpace(friendUserId))
                {
                    return false;
                }
                return State.Friends.Contains(friendUserId);
            }, friendUserId);
        }

        /// <summary>
        /// 获取黑名单
        /// </summary>
        public async Task<List<string>> GetBlockedUsersAsync()
        {
            return await SafeExecuteAsync("GetBlockedUsersAsync", async () =>
            {
                return new List<string>(State.BlockedUsers);
            });
        }

        /// <summary>
        /// 拉黑用户
        /// </summary>
        public async Task BlockUserAsync(string userId)
        {
            await SafeExecuteAsync("BlockUserAsync", async () =>
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    throw new ArgumentException("用户ID不能为空", nameof(userId));
                }

                if (!State.BlockedUsers.Contains(userId))
                {
                    State.BlockedUsers.Add(userId);
                    await SaveUserStateAsync();
                }
            }, userId);
        }

        /// <summary>
        /// 取消拉黑
        /// </summary>
        public async Task UnblockUserAsync(string userId)
        {
            await SafeExecuteAsync("UnblockUserAsync", async () =>
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    throw new ArgumentException("用户ID不能为空", nameof(userId));
                }

                if (State.BlockedUsers.Contains(userId))
                {
                    State.BlockedUsers.Remove(userId);
                    await SaveUserStateAsync();
                }
            }, userId);
        }

        /// <summary>
        /// 检查是否被拉黑
        /// </summary>
        public async Task<bool> IsBlockedAsync(string userId)
        {
            return await SafeExecuteAsync("IsBlockedAsync", async () =>
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return false;
                }
                return State.BlockedUsers.Contains(userId);
            }, userId);
        }

        #endregion

        #region 设置管理

        /// <summary>
        /// 获取用户设置
        /// </summary>
        public async Task<Dictionary<string, string>> GetSettingsAsync()
        {
            return await SafeExecuteAsync("GetSettingsAsync", async () =>
            {
                return new Dictionary<string, string>(State.Settings);
            });
        }

        /// <summary>
        /// 更新用户设置
        /// </summary>
        public async Task UpdateSettingAsync(string key, string value)
        {
            await SafeExecuteAsync("UpdateSettingAsync", async () =>
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new ArgumentException("设置键不能为空", nameof(key));
                }

                State.Settings[key] = value;
                await SaveUserStateAsync();
            }, key);
        }

        /// <summary>
        /// 删除用户设置
        /// </summary>
        public async Task DeleteSettingAsync(string key)
        {
            await SafeExecuteAsync("DeleteSettingAsync", async () =>
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new ArgumentException("设置键不能为空", nameof(key));
                }

                if (State.Settings.ContainsKey(key))
                {
                    State.Settings.Remove(key);
                    await SaveUserStateAsync();
                }
            }, key);
        }

        #endregion

        #region 安全执行

        /// <summary>
        /// 安全执行异步操作，包含日志记录和重试机制
        /// </summary>
        private async Task SafeExecuteAsync(string operationName, Func<Task> operation, params string[] sensitiveParameters)
        {
            await ExecuteWithRetryAsync(operationName, async () =>
            {
                await TrackPerformanceAsync(operationName, async () =>
                {
                    await operation();
                    return true;
                }, sensitiveParameters);
                return true;
            }, 3);
        }

        /// <summary>
        /// 安全执行异步操作并返回结果，包含日志记录和重试机制
        /// </summary>
        private async Task<T> SafeExecuteAsync<T>(string operationName, Func<Task<T>> operation, params string[] sensitiveParameters)
        {
            return await ExecuteWithRetryAsync(operationName, async () =>
            {
                return await TrackPerformanceAsync(operationName, operation, sensitiveParameters);
            }, 3);
        }

        #endregion
    }

    #region 数据模型

    /// <summary>
    /// 用户状态
    /// </summary>
    [Serializable]
    public enum UserStatus
    {
        Online,
        Offline,
        Busy,
        Away,
        Invisible
    }

    

    /// <summary>
    /// 用户状态数据
    /// </summary>
    [GenerateSerializer]
    public class UserState
    {
        [Id(0)]
        public string UserId { get; set; }
        [Id(1)]
        public DateTime CreatedAt { get; set; }
        [Id(2)]
        public DateTime UpdatedAt { get; set; }
        [Id(3)]
        public UserStatus Status { get; set; }
        [Id(4)]
        public DateTime LastLogin { get; set; }
        [Id(5)]
        public string PasswordHash { get; set; }
        [Id(6)]
        public string PasswordSalt { get; set; }
        [Id(7)]
        public Dictionary<string, object> Profile { get; set; }
        [Id(8)]
        public List<UserSession> Sessions { get; set; }
        [Id(9)]
        public List<Permission> Permissions { get; set; }
        [Id(10)]
        public Dictionary<string, string> Settings { get; set; }
        [Id(11)]
        public List<string> Friends { get; set; }
        [Id(12)]
        public List<string> BlockedUsers { get; set; }
    }

    #endregion
}