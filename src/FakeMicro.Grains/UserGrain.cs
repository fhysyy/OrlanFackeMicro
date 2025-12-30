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
using FakeMicro.Utilities;
using FakeMicro.Entities;
using FakeMicro.Entities.Enums;
using FakeMicro.Grains.States;

namespace FakeMicro.Grains
{
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
                        UserId = long.Parse(this.GetPrimaryKeyString()),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Status = UserStatus.Offline.ToString(),
                        Sessions = new List<UserSession>(),
                        Permissions = new List<UserPermission>(),
                        Settings = new UserSettings(),
                        Friends = new Dictionary<long, DateTime>(),
                        BlockedUsers = new Dictionary<long, DateTime>()
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

                State.DisplayName = nickname;
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
                return State.DisplayName;
            });
        }

        /// <summary>
        /// 设置用户头像
        /// </summary>
        public async Task SetAvatarAsync(string avatarUrl)
        {
            await SafeExecuteAsync("SetAvatarAsync", async () =>
            {
                State.AvatarUrl = avatarUrl;
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
                return State.AvatarUrl;
            });
        }

        /// <summary>
        /// 获取用户资料
        /// </summary>
        public async Task<Dictionary<string, object>> GetProfileAsync()
        {
            return await SafeExecuteAsync("GetProfileAsync", async () =>
            {
                var profile = new Dictionary<string, object>
                {
                    { "userId", State.UserId },
                    { "username", State.Username },
                    { "email", State.Email },
                    { "phone", State.Phone },
                    { "displayName", State.DisplayName },
                    { "role", State.Role },
                    { "status", State.Status },
                    { "isActive", State.IsActive },
                    { "emailVerified", State.EmailVerified },
                    { "phoneVerified", State.PhoneVerified },
                    { "createdAt", State.CreatedAt },
                    { "updatedAt", State.UpdatedAt }
                };
                return profile;
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

                if (profile.TryGetValue("username", out var username) && username is string usernameStr)
                {
                    State.Username = usernameStr;
                }
                if (profile.TryGetValue("email", out var email) && email is string emailStr)
                {
                    State.Email = emailStr;
                }
                if (profile.TryGetValue("phone", out var phone) && phone is string phoneStr)
                {
                    State.Phone = phoneStr;
                }
                if (profile.TryGetValue("displayName", out var displayName) && displayName is string displayNameStr)
                {
                    State.DisplayName = displayNameStr;
                }
                if (profile.TryGetValue("role", out var role) && role is string roleStr)
                {
                    State.Role = roleStr;
                }
                if (profile.TryGetValue("status", out var status) && status is string statusStr)
                {
                    State.Status = statusStr;
                }
                if (profile.TryGetValue("isActive", out var isActive) && isActive is bool isActiveBool)
                {
                    State.IsActive = isActiveBool;
                }
                if (profile.TryGetValue("emailVerified", out var emailVerified) && emailVerified is bool emailVerifiedBool)
                {
                    State.EmailVerified = emailVerifiedBool;
                }
                if (profile.TryGetValue("phoneVerified", out var phoneVerified) && phoneVerified is bool phoneVerifiedBool)
                {
                    State.PhoneVerified = phoneVerifiedBool;
                }

                State.UpdatedAt = DateTime.UtcNow;
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
        /// 获取用户信息（从内存状态）
        /// </summary>
        public async Task<UserDto?> GetUserAsync(CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync("GetUserAsync", async () =>
            {
                if (!IsStateValid)
                {
                    return null;
                }

                return new UserDto
                {
                    Id = State.UserId,
                    Username = State.Username,
                    Email = State.Email,
                    DisplayName = State.DisplayName,
                    Status = (UserStatus)Enum.Parse(typeof(UserStatus), State.Status)
                };
            });
        }

        /// <summary>
        /// 从数据库直接获取用户信息
        /// </summary>
        public async Task<UserDto?> GetUserFromDatabaseAsync(CancellationToken cancellationToken = default)
        {
            // 实际项目中应该实现从数据库获取数据的逻辑
            // 这里暂时返回与内存状态相同的结果
            return await GetUserAsync(cancellationToken);
        }

        /// <summary>
        /// 更新用户信息
        /// </summary>
        public async Task<UpdateUserResult> UpdateUserAsync(UserDto userDto, CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync("UpdateUserAsync", async () =>
            {
                if (userDto == null)
                {
                    throw new ArgumentNullException(nameof(userDto));
                }

                State.Username = userDto.Username;
                State.Email = userDto.Email;
                State.DisplayName = userDto.DisplayName;
                State.Status = userDto.Status.ToString();
                await SaveUserStateAsync();

                return UpdateUserResult.CreateSuccess(userDto);
            });
        }

        /// <summary>
        /// 更新登录信息
        /// </summary>
        public async Task UpdateLoginInfoAsync(bool success, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
        {
            await SafeExecuteAsync("UpdateLoginInfoAsync", async () =>
            {
                State.LastLoginAt = DateTime.UtcNow;
                await SaveUserStateAsync();
            });
        }

        /// <summary>
        /// 生成刷新令牌
        /// </summary>
        public async Task<string?> GenerateRefreshTokenAsync(CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync("GenerateRefreshTokenAsync", async () =>
            {
                var token = Guid.NewGuid().ToString();
                State.RefreshToken = token;
                State.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
                await SaveUserStateAsync();
                return token;
            });
        }

        /// <summary>
        /// 验证刷新令牌
        /// </summary>
        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync("ValidateRefreshTokenAsync", async () =>
            {
                if (string.IsNullOrWhiteSpace(State.RefreshToken))
                {
                    return false;
                }

                if (State.RefreshTokenExpiry < DateTime.UtcNow)
                {
                    return false;
                }

                return State.RefreshToken == refreshToken;
            });
        }

        /// <summary>
        /// 发送邮箱验证邮件
        /// </summary>
        public async Task<SendVerificationResult> SendEmailVerificationAsync(CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync("SendEmailVerificationAsync", async () =>
            {
                // 实际项目中应该实现发送邮件的逻辑
                return SendVerificationResult.CreateSuccess();
            });
        }

        /// <summary>
        /// 验证邮箱
        /// </summary>
        public async Task<VerifyEmailResult> VerifyEmailAsync(string token, CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync("VerifyEmailAsync", async () =>
            {
                // 实际项目中应该实现验证邮箱的逻辑
                State.IsEmailVerified = true;
                await SaveUserStateAsync();
                return VerifyEmailResult.CreateSuccess();
            });
        }

        /// <summary>
        /// 发送手机验证码
        /// </summary>
        public async Task<SendVerificationResult> SendPhoneVerificationAsync(CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync("SendPhoneVerificationAsync", async () =>
            {
                // 实际项目中应该实现发送手机验证码的逻辑
                return SendVerificationResult.CreateSuccess();
            });
        }

        /// <summary>
        /// 验证手机
        /// </summary>
        public async Task<VerifyPhoneResult> VerifyPhoneAsync(string code, CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync("VerifyPhoneAsync", async () =>
            {
                // 实际项目中应该实现验证手机的逻辑
                State.IsPhoneVerified = true;
                await SaveUserStateAsync();
                return VerifyPhoneResult.CreateSuccess();
            });
        }

        /// <summary>
        /// 请求密码重置
        /// </summary>
        public async Task<RequestPasswordResetResult> RequestPasswordResetAsync(CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync("RequestPasswordResetAsync", async () =>
            {
                // 实际项目中应该实现请求密码重置的逻辑
                return RequestPasswordResetResult.CreateSuccess();
            });
        }

        /// <summary>
        /// 重置密码
        /// </summary>
        public async Task<ResetPasswordResult> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync("ResetPasswordAsync", async () =>
            {
                // 实际项目中应该实现重置密码的逻辑
                var passwordHash = HashPassword(newPassword);
                State.PasswordHash = passwordHash;
                await SaveUserStateAsync();
                return ResetPasswordResult.CreateSuccess();
            });
        }

        /// <summary>
        /// 更新用户会话信息
        /// </summary>
        public async Task UpdateSessionAsync(string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
        {
            await SafeExecuteAsync("UpdateSessionAsync", async () =>
            {
                if (State.Sessions.Any(s => s.IsCurrent))
                {
                    var currentSession = State.Sessions.First(s => s.IsCurrent);
                    currentSession.LastActivity = DateTime.UtcNow;
                    if (ipAddress != null)
                    {
                        currentSession.IpAddress = ipAddress;
                    }
                    if (userAgent != null)
                    {
                        currentSession.UserAgent = userAgent;
                    }
                    await SaveUserStateAsync();
                }
            });
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        public async Task<DeleteUserResult> DeleteUserAsync(CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync("DeleteUserAsync", async () =>
            {
                // 实际项目中应该实现删除用户的逻辑
                await _userState.ClearStateAsync();
                return DeleteUserResult.CreateSuccess();
            });
        }

        /// <summary>
        /// 终止所有会话（除当前会话外）
        /// </summary>
        public async Task<TerminateSessionResult> TerminateOtherSessionsAsync(CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync("TerminateOtherSessionsAsync", async () =>
            {
                var currentSessions = State.Sessions.Where(s => !s.IsCurrent).ToList();
                var count = currentSessions.Count;
                
                foreach (var session in currentSessions)
                {
                    State.Sessions.Remove(session);
                }
                
                await SaveUserStateAsync();
                return TerminateSessionResult.CreateSuccess(count);
            });
        }

        /// <summary>
        /// 终止会话
        /// </summary>
        public async Task<TerminateSessionResult> TerminateSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync("TerminateSessionAsync", async () =>
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
                    return TerminateSessionResult.CreateSuccess(1);
                }
                return TerminateSessionResult.CreateFailed("会话不存在");
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
                return State.Permissions
                    .Select(p => new Permission
                    {
                        Resource = p.Resource,
                        Type = Enum.TryParse<PermissionType>(p.Type, out var permissionType) ? permissionType : default
                    })
                    .ToList();
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

                if (!State.Permissions.Any(p => p.Resource == permission.Resource && p.Type == permission.Type.ToString()))
                {
                    State.Permissions.Add(new UserPermission
                    {
                        Resource = permission.Resource,
                        Type = permission.Type.ToString(),
                        GrantedAt = DateTime.UtcNow
                    });
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
                State.Status = status.ToString();
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
                return Enum.TryParse<UserStatus>(State.Status, out var status) ? status : UserStatus.Pending;
            });
        }

        /// <summary>
        /// 获取用户邮箱
        /// </summary>
        public async Task<string> GetEmailAsync()
        {
            return await SafeExecuteAsync("GetEmailAsync", async () =>
            {
                return State.Email;
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

                State.Email = email;
                await SaveUserStateAsync();
            }, email);
        }

        /// <summary>
        /// 设置用户密码
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
                    // 首先尝试使用PBKDF2验证（新格式）
                    // 组合盐和哈希以匹配CryptoHelper的格式
                    var combinedHashBytes = new byte[16 + Convert.FromBase64String(State.PasswordHash).Length];
                    Array.Copy(Convert.FromBase64String(State.PasswordSalt), 0, combinedHashBytes, 0, 16);
                    Array.Copy(Convert.FromBase64String(State.PasswordHash), 0, combinedHashBytes, 16, Convert.FromBase64String(State.PasswordHash).Length);
                    var combinedHash = Convert.ToBase64String(combinedHashBytes);
                    
                    if (CryptoHelper.VerifyPasswordHash(password, combinedHash))
                    {
                        return true;
                    }
                }
                catch
                {
                    // 忽略错误，尝试旧格式
                }

                try
                {
                    // 使用旧的HMACSHA512验证（用于迁移）
                    return CryptoHelper.VerifyLegacyPasswordHash(password, State.PasswordHash, State.PasswordSalt);
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

                // 生成新密码哈希
                var passwordHash = CryptoHelper.GeneratePasswordHash(newPassword);
                
                // 从哈希中提取盐（前16字节）
                var hashBytes = Convert.FromBase64String(passwordHash);
                var passwordSalt = Convert.ToBase64String(hashBytes.Take(16).ToArray());
                
                // 提取实际哈希值（剩余字节）
                passwordHash = Convert.ToBase64String(hashBytes.Skip(16).ToArray());

                // 更新密码
                State.PasswordHash = passwordHash;
                State.PasswordSalt = passwordSalt;
                await SaveUserStateAsync();

                return ChangePasswordResult.CreateSuccess();
            }, "[密码修改]");
        }

        #endregion

        #region 时间管理

        /// <summary>
        /// 获取最后登录时间
        /// </summary>
        public async Task<DateTime> GetLastLoginAsync()
        {
            return await SafeExecuteAsync("GetLastLoginAsync", async () =>
            {
                return State.LastLoginAt ?? DateTime.MinValue;
            });
        }

        /// <summary>
        /// 设置最后登录时间
        /// </summary>
        public async Task SetLastLoginAsync(DateTime lastLogin)
        {
            await SafeExecuteAsync("SetLastLoginAsync", async () =>
            {
                State.LastLoginAt = lastLogin;
                await SaveUserStateAsync();
            }, lastLogin.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        #endregion

        #region 在线状态管理

        /// <summary>
        /// 更新在线状态
        /// </summary>
        public async Task UpdateOnlineStatusAsync(bool isOnline)
        {
            await SafeExecuteAsync("UpdateOnlineStatusAsync", async () =>
            {
                State.Status = (isOnline ? UserStatus.Online : UserStatus.Offline).ToString();
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
                return Enum.TryParse<UserStatus>(State.Status, out var status) && status == UserStatus.Online;
            });
        }

        #endregion

        #region 社交关系管理

        /// <summary>
        /// 获取用户好友列表
        /// </summary>
        public async Task<List<long>> GetFriendsAsync()
        {
            return await SafeExecuteAsync("GetFriendsAsync", async () =>
            {
                return State.Friends.Keys.ToList();
            });
        }

        /// <summary>
        /// 添加好友
        /// </summary>
        public async Task AddFriendAsync(long friendUserId)
        {
            await SafeExecuteAsync("AddFriendAsync", async () =>
            {
                if (friendUserId <= 0)
                {
                    throw new ArgumentException("好友ID不能为空", nameof(friendUserId));
                }

                if (!State.Friends.ContainsKey(friendUserId))
                {
                    State.Friends.Add(friendUserId, DateTime.UtcNow);
                    await SaveUserStateAsync();
                }
            }, friendUserId.ToString());
        }

        /// <summary>
        /// 移除好友
        /// </summary>
        public async Task RemoveFriendAsync(long friendUserId)
        {
            await SafeExecuteAsync("RemoveFriendAsync", async () =>
            {
                if (friendUserId <= 0)
                {
                    throw new ArgumentException("好友ID不能为空", nameof(friendUserId));
                }

                if (State.Friends.ContainsKey(friendUserId))
                {
                    State.Friends.Remove(friendUserId);
                    await SaveUserStateAsync();
                }
            }, friendUserId.ToString());
        }

        /// <summary>
        /// 检查是否为好友
        /// </summary>
        public async Task<bool> IsFriendAsync(long friendUserId)
        {
            return await SafeExecuteAsync("IsFriendAsync", async () =>
            {
                if (friendUserId <= 0)
                {
                    return false;
                }
                return State.Friends.ContainsKey(friendUserId);
            }, friendUserId.ToString());
        }

        #endregion

        #region 屏蔽用户管理

        /// <summary>
        /// 获取被屏蔽的用户列表
        /// </summary>
        public async Task<List<long>> GetBlockedUsersAsync()
        {
            return await SafeExecuteAsync("GetBlockedUsersAsync", async () =>
            {
                return State.BlockedUsers.Keys.ToList();
            });
        }

        /// <summary>
        /// 屏蔽用户
        /// </summary>
        public async Task BlockUserAsync(long userId)
        {
            await SafeExecuteAsync("BlockUserAsync", async () =>
            {
                if (userId <= 0)
                {
                    throw new ArgumentException("用户ID不能为空", nameof(userId));
                }

                if (!State.BlockedUsers.ContainsKey(userId))
                {
                    State.BlockedUsers.Add(userId, DateTime.UtcNow);
                    await SaveUserStateAsync();
                }
            }, userId.ToString());
        }

        /// <summary>
        /// 取消屏蔽用户
        /// </summary>
        public async Task UnblockUserAsync(long userId)
        {
            await SafeExecuteAsync("UnblockUserAsync", async () =>
            {
                if (userId <= 0)
                {
                    throw new ArgumentException("用户ID不能为空", nameof(userId));
                }

                if (State.BlockedUsers.ContainsKey(userId))
                {
                    State.BlockedUsers.Remove(userId);
                    await SaveUserStateAsync();
                }
            }, userId.ToString());
        }

        /// <summary>
        /// 检查用户是否被屏蔽
        /// </summary>
        public async Task<bool> IsBlockedAsync(long userId)
        {
            return await SafeExecuteAsync("IsBlockedAsync", async () =>
            {
                if (userId <= 0)
                {
                    return false;
                }
                return State.BlockedUsers.ContainsKey(userId);
            }, userId.ToString());
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
                var settings = new Dictionary<string, string>
                {
                    // 通知设置
                    ["notifications.emailEnabled"] = State.Settings.Notifications.EmailEnabled.ToString(),
                    ["notifications.smsEnabled"] = State.Settings.Notifications.SmsEnabled.ToString(),
                    ["notifications.pushEnabled"] = State.Settings.Notifications.PushEnabled.ToString(),
                    // 隐私设置
                    ["privacy.showEmail"] = State.Settings.Privacy.ShowEmail.ToString(),
                    ["privacy.showPhone"] = State.Settings.Privacy.ShowPhone.ToString(),
                    ["privacy.allowFriendRequests"] = State.Settings.Privacy.AllowFriendRequests.ToString(),
                    // 主题设置
                    ["theme.themeName"] = State.Settings.Theme.ThemeName,
                    ["theme.autoTheme"] = State.Settings.Theme.AutoTheme.ToString()
                };
                return settings;
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

                var parts = key.Split('.');
                if (parts.Length < 2)
                {
                    return;
                }

                var category = parts[0].ToLower();
                var settingKey = parts[1].ToLower();

                switch (category)
                {
                    case "notifications":
                        UpdateNotificationSetting(settingKey, value);
                        break;
                    case "privacy":
                        UpdatePrivacySetting(settingKey, value);
                        break;
                    case "theme":
                        UpdateThemeSetting(settingKey, value);
                        break;
                }

                await SaveUserStateAsync();
            }, key);
        }

        /// <summary>
        /// 删除用户设置（重置为默认值）
        /// </summary>
        public async Task DeleteSettingAsync(string key)
        {
            await SafeExecuteAsync("DeleteSettingAsync", async () =>
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    throw new ArgumentException("设置键不能为空", nameof(key));
                }

                var parts = key.Split('.');
                if (parts.Length < 2)
                {
                    return;
                }

                var category = parts[0].ToLower();
                var settingKey = parts[1].ToLower();

                // 重置为默认值
                switch (category)
                {
                    case "notifications":
                        ResetNotificationSetting(settingKey);
                        break;
                    case "privacy":
                        ResetPrivacySetting(settingKey);
                        break;
                    case "theme":
                        ResetThemeSetting(settingKey);
                        break;
                }

                await SaveUserStateAsync();
            }, key);
        }

        #region 私有辅助方法

        /// <summary>
        /// 更新通知设置
        /// </summary>
        private void UpdateNotificationSetting(string key, string value)
        {
            switch (key)
            {
                case "emailenabled":
                    if (bool.TryParse(value, out var emailEnabled))
                    {
                        State.Settings.Notifications.EmailEnabled = emailEnabled;
                    }
                    break;
                case "smsenabled":
                    if (bool.TryParse(value, out var smsEnabled))
                    {
                        State.Settings.Notifications.SmsEnabled = smsEnabled;
                    }
                    break;
                case "pushenabled":
                    if (bool.TryParse(value, out var pushEnabled))
                    {
                        State.Settings.Notifications.PushEnabled = pushEnabled;
                    }
                    break;
            }
        }

        /// <summary>
        /// 更新隐私设置
        /// </summary>
        private void UpdatePrivacySetting(string key, string value)
        {
            switch (key)
            {
                case "showemail":
                    if (bool.TryParse(value, out var showEmail))
                    {
                        State.Settings.Privacy.ShowEmail = showEmail;
                    }
                    break;
                case "showphone":
                    if (bool.TryParse(value, out var showPhone))
                    {
                        State.Settings.Privacy.ShowPhone = showPhone;
                    }
                    break;
                case "allowfriendrequests":
                    if (bool.TryParse(value, out var allowFriendRequests))
                    {
                        State.Settings.Privacy.AllowFriendRequests = allowFriendRequests;
                    }
                    break;
            }
        }

        /// <summary>
        /// 更新主题设置
        /// </summary>
        private void UpdateThemeSetting(string key, string value)
        {
            switch (key)
            {
                case "themename":
                    State.Settings.Theme.ThemeName = value;
                    break;
                case "autotheme":
                    if (bool.TryParse(value, out var autoTheme))
                    {
                        State.Settings.Theme.AutoTheme = autoTheme;
                    }
                    break;
            }
        }

        /// <summary>
        /// 重置通知设置
        /// </summary>
        private void ResetNotificationSetting(string key)
        {
            var defaultSettings = new NotificationSettings();
            switch (key)
            {
                case "emailenabled":
                    State.Settings.Notifications.EmailEnabled = defaultSettings.EmailEnabled;
                    break;
                case "smsenabled":
                    State.Settings.Notifications.SmsEnabled = defaultSettings.SmsEnabled;
                    break;
                case "pushenabled":
                    State.Settings.Notifications.PushEnabled = defaultSettings.PushEnabled;
                    break;
            }
        }

        /// <summary>
        /// 重置隐私设置
        /// </summary>
        private void ResetPrivacySetting(string key)
        {
            var defaultSettings = new PrivacySettings();
            switch (key)
            {
                case "showemail":
                    State.Settings.Privacy.ShowEmail = defaultSettings.ShowEmail;
                    break;
                case "showphone":
                    State.Settings.Privacy.ShowPhone = defaultSettings.ShowPhone;
                    break;
                case "allowfriendrequests":
                    State.Settings.Privacy.AllowFriendRequests = defaultSettings.AllowFriendRequests;
                    break;
            }
        }

        /// <summary>
        /// 重置主题设置
        /// </summary>
        private void ResetThemeSetting(string key)
        {
            var defaultSettings = new ThemeSettings();
            switch (key)
            {
                case "themename":
                    State.Settings.Theme.ThemeName = defaultSettings.ThemeName;
                    break;
                case "autotheme":
                    State.Settings.Theme.AutoTheme = defaultSettings.AutoTheme;
                    break;
            }
        }

        #endregion

        #endregion

        /// <summary>
        /// 哈希密码
        /// </summary>
        private string HashPassword(string password)
        {
            return CryptoHelper.GeneratePasswordHash(password);
        }

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

    #endregion
}