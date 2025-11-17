using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Entities;
using FakeMicro.Entities.Enums;
using FakeMicro.Grains.States;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
using FakeMicro.Utilities.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;
using Orleans.Storage;
using Orleans.Concurrency;
using System.Security.Cryptography;
using System.Text;
using UserSession = FakeMicro.Grains.States.UserSession;
using Orleans.Providers;

namespace FakeMicro.Grains
{
    /// <summary>
    /// 用户Grain实现 - 遵循Orleans 9.x最佳实践
    /// </summary>

    public class UserGrain : Grain, IUserGrain
    {
        private readonly IUserRepository _repository;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<UserGrain> _logger;
        private readonly SemaphoreSlim _stateLoadLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// 持久化状态 - Orleans 9.x最佳实践：直接声明属性
        /// </summary>
        public readonly IPersistentState<UserState> UserState;

        //  private readonly IPersistentState<CounterState> _state;


        /// <summary>
        /// 保存状态的方法别名，与Orleans 9.x最佳实践保持一致
        /// </summary>
        private Task SaveStateAsync() => UserState.WriteStateAsync();

        public UserGrain(
            IUserRepository userRepository,
            [PersistentState("userState", "UserStateStore")] IPersistentState<UserState> state,
            ILogger<UserGrain> logger,
            IOptions<JwtSettings> jwtSettings)
        {
            UserState = state;
            // Orleans 9.x: IPersistentState 通过属性注入，不需要构造函数参数
            _repository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jwtSettings = jwtSettings?.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
        }

        /// <summary>
        /// Grain激活时的初始化逻辑
        /// </summary>
        public override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            var userIdStr = this.GetPrimaryKeyString();
            _logger.LogInformation("UserGrain激活: 用户ID: {UserId}", userIdStr);

            // 异步加载用户数据，不阻塞激活过程
            _ = Task.Run(async () => 
            {
                try
                {
                    await LoadUserDataAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "激活过程中加载用户数据失败: {UserId}", userIdStr);
                }
            }, cancellationToken);
        }
        
        /// <summary>
        /// Grain停用前的清理逻辑
        /// </summary>
        public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
        {
            _logger.LogInformation("UserGrain停用: 用户ID: {UserId}, 原因: {Reason}", 
                this.GetPrimaryKeyString(), reason.Description);
            
            // 清理资源
            _stateLoadLock.Dispose();
            
            await base.OnDeactivateAsync(reason, cancellationToken);
        }
        
        /// <summary>
        /// 从数据库加载用户数据到状态管理（Orleans最佳实践）
        /// </summary>
        private async Task LoadUserDataAsync(CancellationToken cancellationToken = default)
        {
            await _stateLoadLock.WaitAsync(cancellationToken);
            try
            {
                // 空值检查 - 防止空引用异常
                if (UserState?.State == null)
                {
                    _logger.LogWarning("UserState.State 为空，尝试重新初始化状态");
                    try
                    {
                        if (UserState != null)
                        {
                            await UserState.ReadStateAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "读取状态失败，可能需要初始化状态");
                        return;
                    }
                    
                    // 再次检查状态是否可用
                    if (UserState?.State == null)
                    {
                        _logger.LogError("状态仍然为空，无法加载用户数据");
                        return;
                    }
                }

                if (UserState?.State?.IsLoaded == true && UserState?.State?.UserId > 0)
                {
                    return;
                }
                
                var userIdStr = this.GetPrimaryKeyString();
                if (!long.TryParse(userIdStr, out var userId))
                {
                    _logger.LogWarning("无效的用户ID格式: {UserId}", userIdStr);
                    return;
                }
                
                var user = await _repository.GetByIdAsync(userId);
                
                if (user != null)
                {
                    if (UserState != null)
                    {
                        if (UserState.State == null)
                        {
                            UserState.State = new UserState();
                        }

                        UserState!.State!.UserId = user.id;
                        UserState!.State!.Username = user.username ?? string.Empty;
                        UserState!.State!.Email = user.email ?? string.Empty;
                        UserState!.State!.Phone = user.phone ?? string.Empty;
                        UserState!.State!.DisplayName = user.display_name ?? string.Empty;
                        UserState!.State!.Role = user.role ?? string.Empty;
                        UserState!.State!.Status = user.status ?? string.Empty;
                        UserState!.State!.LastLoginAt = user.last_login_at ?? default(DateTime);
                        UserState!.State!.IsActive = user.is_active;
                        UserState!.State!.EmailVerified = user.email_verified;
                        UserState!.State!.PhoneVerified = user.phone_verified;
                        UserState!.State!.CreatedAt = user.created_at;
                        UserState!.State!.UpdatedAt = user.updated_at;
                        UserState!.State!.IsLoaded = true;
                    
                        // 批量更新状态 - Orleans状态管理最佳实践
                        await UserState.WriteStateAsync();
                        _logger.LogInformation("用户状态初始化完成: {UserId}", userId);
                    }
                    else
                    {
                        _logger.LogError("UserState为空，无法初始化用户状态");
                    }
                }
                else
                {
                    _logger.LogWarning("用户不存在，无法初始化状态: {UserId}", userId);
                }
            }
            finally
            {
                _stateLoadLock.Release();
            }
        }

        /// <summary>
        /// 检查状态是否有效
        /// </summary>
        private bool IsStateValid()
        {
            return UserState?.State?.IsValid() == true;
        }

        /// <summary>
        /// 初始化Grain
        /// </summary>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            await LoadUserDataAsync(cancellationToken);
        }

        /// <summary>
        /// 获取用户信息（使用状态管理）
        /// </summary>
        [ReadOnly]
        public async Task<UserDto?> GetUserAsync(CancellationToken cancellationToken = default)
        {
            // 空值检查 - 防止空引用异常
            if (UserState?.State == null)
            {
                _logger.LogWarning("UserState.State 为空，尝试重新初始化状态");
                try
                {
                    if (UserState != null)
                    {
                        await UserState.ReadStateAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "读取状态失败，无法获取用户信息");
                    return null;
                }
            }

            // 确保状态已加载
            if (UserState?.State?.IsLoaded != true || UserState?.State?.UserId == 0)
            {
                await LoadUserDataAsync(cancellationToken);
            }
            
            if (!IsStateValid())
            {
                return null;
            }

            try
            {
                if (UserState?.State?.UserId == 0)
                {
                    return null;
                }

                // 返回用户DTO的新实例，避免外部修改影响状态
                return new UserDto
                {
                    Id = UserState?.State?.UserId ?? 0,
                    Username = UserState?.State?.Username ?? string.Empty,
                     Email = UserState?.State?.Email ?? string.Empty,
                     Phone = UserState?.State?.Phone ?? string.Empty,
                    DisplayName = UserState?.State?.DisplayName ?? string.Empty,
                    Role = Enum.TryParse<FakeMicro.Entities.Enums.UserRole>(UserState?.State?.Role ?? string.Empty, out var role) ? role : FakeMicro.Entities.Enums.UserRole.User,
                    Status = Enum.TryParse<FakeMicro.Entities.Enums.UserStatus>(UserState?.State?.Status ?? string.Empty, out var status) ? status : FakeMicro.Entities.Enums.UserStatus.Pending,
                    CreatedAt = UserState?.State?.CreatedAt ?? default(DateTime),
                    UpdatedAt = UserState?.State?.UpdatedAt ?? default(DateTime)
                };
            }
            catch (Exception ex)
            {
                var userId = UserState?.State?.UserId ?? 0;
                _logger.LogError(ex, "获取用户信息失败: {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// 从数据库直接获取用户信息
        /// </summary>
        [ReadOnly]
        public async Task<UserDto?> GetUserFromDatabaseAsync(CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync<UserDto?>("GetUserFromDatabaseAsync", async () =>
            {
                try
                {
                    // 从 Grain 的主键获取用户ID
                    var userIdStr = this.GetPrimaryKeyString();
                    if (!long.TryParse(userIdStr, out var userId))
                    {
                        _logger.LogWarning("无效的用户ID格式: {UserId}", userIdStr);
                        return null;
                    }
                    
                    // 直接从数据库读取用户数据
                    var user = await _repository.GetByIdAsync(userId);
                    
                    if (user == null)
                    {
                        _logger.LogWarning("用户不存在: {UserId}", userId);
                        return null;
                    }
                    
                    // 将数据库实体转换为DTO
                    var userRole = FakeMicro.Entities.Enums.UserRole.User;
                    if (!string.IsNullOrEmpty(user.role))
                    {
                        Enum.TryParse<FakeMicro.Entities.Enums.UserRole>(user.role, out userRole);
                    }
                    
                    var userStatus = FakeMicro.Entities.Enums.UserStatus.Pending;
                    if (!string.IsNullOrEmpty(user.status))
                    {
                        Enum.TryParse<FakeMicro.Entities.Enums.UserStatus>(user.status, out userStatus);
                    }
                    
                    _logger.LogInformation("从数据库成功获取用户信息: {UserId}", userId);
                    
                    return new UserDto
                    {
                        Id = user.id,
                        Username = user.username ?? string.Empty,
                        Email = user.email ?? string.Empty,
                        Phone = user.phone ?? string.Empty,
                        DisplayName = user.display_name ?? string.Empty,
                        Role = userRole,
                        Status = userStatus,
                        CreatedAt = user.created_at,
                        UpdatedAt = user.updated_at
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "从数据库获取用户信息失败");
                    return null;
                }
            });
        }

        /// <summary>
        /// 更新用户信息（使用状态管理）
        /// </summary>
        public async Task<UpdateUserResult> UpdateUserAsync(UserDto userDto, CancellationToken cancellationToken = default)
        {
            // 空值检查 - 防止空引用异常
            if (UserState?.State == null)
            {
                _logger.LogWarning("UserState.State 为空，尝试重新初始化状态");
                try
                {
                    if (UserState != null)
                    {
                        await UserState.ReadStateAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "读取状态失败，无法更新用户信息");
                    return UpdateUserResult.CreateFailed("用户状态初始化失败");
                }
            }

            return await SafeExecuteAsync<UpdateUserResult>("UpdateUserAsync", async () =>
            {
                if (userDto == null)
                {
                    return UpdateUserResult.CreateFailed("UserDto对象不能为空");
                }
                
                if (UserState?.State?.UserId == 0)
                {
                    // 确保状态已加载
                    await LoadUserDataAsync(cancellationToken);
                    if (UserState?.State?.UserId == 0)
                    {
                        return UpdateUserResult.CreateFailed("用户不存在");
                    }
                }

                try
                {
                    // 复制当前状态以进行验证和比较
                    var originalState = UserState?.State?.DeepCopy() ?? new UserState();
                    
                    // 获取用户ID，如果没有则返回失败
                    var userId = UserState?.State?.UserId ?? 0;
                    if (userId == 0)
                    {
                        return UpdateUserResult.CreateFailed("用户ID无效");
                    }

                    // 更新状态 - 仅更新非空字段
                    if (!string.IsNullOrEmpty(userDto.Email))
                    {
                        UserState.State.Email = userDto.Email;
                    }
                    
                    if (!string.IsNullOrEmpty(userDto.Phone))
                    {
                        UserState.State.Phone = userDto.Phone;
                    }
                    
                    if (!string.IsNullOrEmpty(userDto.DisplayName))
                    {
                        UserState.State.DisplayName = userDto.DisplayName;
                    }
                    
                    // 角色和状态可以更新
                    UserState.State.Role = userDto.Role.ToString();
                    UserState.State.Status = userDto.Status.ToString();
                    UserState.State.UpdatedAt = DateTime.UtcNow;

                    // 同步到数据库
                    var user = await _repository.GetByIdAsync(userId);
                    if (user == null)
                    {
                        return UpdateUserResult.CreateFailed("用户不存在于数据库中");
                    }
                    
                    user.email = userDto.Email;
                    user.phone = userDto.Phone;
                    user.display_name = userDto.DisplayName;
                    user.role = userDto.Role.ToString();
                    user.status = userDto.Status.ToString();
                    user.updated_at = DateTime.UtcNow;

                    await _repository.UpdateAsync(user);
                    
                    // 使用持久化状态机制保存状态
                    if (UserState != null)
                    {
                        await UserState.WriteStateAsync();
                    }

                    var currentUserId = UserState?.State?.UserId ?? userDto.Id;
                    _logger.LogInformation("用户信息更新成功: {UserId}", currentUserId);
                    // 创建一个UserDto对象返回
                    var userRole = FakeMicro.Entities.Enums.UserRole.User;
                    if (UserState?.State != null && UserState.State.Role != null)
                    {
                        Enum.TryParse<FakeMicro.Entities.Enums.UserRole>(UserState.State.Role, out userRole);
                    }
                    
                    var updatedUserDto = new UserDto
                    {
                        Id = currentUserId,
                        Username = UserState?.State?.Username ?? string.Empty,
                        Email = UserState?.State?.Email ?? string.Empty,
                        Phone = UserState?.State?.Phone ?? string.Empty,
                        Role = userRole
                    };
                    return UpdateUserResult.CreateSuccess(updatedUserDto);
                }
                catch (Exception ex)
                {
                    var existingUserId = UserState?.State?.UserId ?? 0;
                    _logger.LogError(ex, "更新用户信息失败: {UserId}", existingUserId);
                    return UpdateUserResult.CreateFailed("更新用户信息失败: " + ex.Message);
                }
            }, userDto.Username, userDto.Email);
        }
        
        // 注意：UpdateUserResult类已移至接口定义中

        /// <summary>
        /// 验证密码
        /// </summary>
        [ReadOnly]
        public async Task<bool> ValidatePasswordAsync(string password, CancellationToken cancellationToken = default)
        {
            // 空值检查 - 防止空引用异常
            if (UserState?.State == null)
            {
                _logger.LogWarning("UserState.State 为空，尝试重新初始化状态");
                try
                {
                    if (UserState != null)
                    {
                        await UserState.ReadStateAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "读取状态失败，无法验证密码");
                    return false;
                }
            }

            return await SafeExecuteAsync<bool>("ValidatePasswordAsync", async () =>
            {
                var userId = UserState?.State?.UserId ?? 0;
                if (userId == 0)
                {
                    return false;
                }
                
                var user = await _repository.GetByIdAsync(userId);
                if (user == null || string.IsNullOrEmpty(user.password_hash) || string.IsNullOrEmpty(user.password_salt))
                {
                    return false;
                }

                return VerifyPasswordHash(password, user.password_hash, user.password_salt);
            }, password);
        }

        /// <summary>
        /// 更新会话信息（使用状态管理）
        /// </summary>
        public async Task UpdateSessionAsync(string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
        {
            // 空值检查 - 防止空引用异常
            if (UserState?.State == null)
            {
                _logger.LogWarning("UserState.State 为空，尝试重新初始化状态");
                try
                {
                    if (UserState != null)
                    {
                        await UserState.ReadStateAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "读取状态失败，无法更新会话");
                    return;
                }
            }

            await SafeExecuteAsync("UpdateSessionAsync", async () =>
            {
                // 再次检查状态是否可用
                if (UserState?.State == null)
                {
                    _logger.LogError("状态仍然为空，无法更新会话");
                    return false;
                }

                var sessionId = Guid.NewGuid().ToString();
                var newSession = new UserSession
                {
                    SessionId = sessionId,
                    LoginTime = DateTime.UtcNow,
                    LastActivity = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    IsCurrent = true
                };

                // 将之前的当前会话标记为非当前
                foreach (var session in UserState?.State?.Sessions ?? new List<UserSession>())
                {
                    if (session == null) continue;
                    
                    if (session.IsCurrent)
                    {
                        session.IsCurrent = false;
                        session.LastActivity = DateTime.UtcNow;
                    }
                }

                UserState?.State?.AddSession(newSession);
                UserState?.State?.UpdateLoginInfo(true);

                var userId = UserState?.State?.UserId ?? 0;
                _logger.LogInformation("用户会话更新成功: {UserId}, SessionId: {SessionId}", 
                    userId, sessionId);
                
                return true;
            }, ipAddress ?? string.Empty, userAgent ?? string.Empty);
        }

        /// <summary>
        /// 获取用户会话列表
        /// </summary>
        [ReadOnly]
        public async Task<List<FakeMicro.Interfaces.UserSession>> GetSessionsAsync(CancellationToken cancellationToken = default)
        {
            // 空值检查 - 防止空引用异常
            if (UserState?.State == null)
            {
                _logger.LogWarning("UserState.State 为空，尝试重新初始化状态");
                try
                {
                    if (UserState != null)
                    {
                        await UserState.ReadStateAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "读取状态失败，返回空会话列表");
                    return new List<FakeMicro.Interfaces.UserSession>();
                }
            }

            if (!IsStateValid())
            {
                return new List<FakeMicro.Interfaces.UserSession>();
            }

            return await SafeExecuteAsync<List<FakeMicro.Interfaces.UserSession>>("GetSessionsAsync", async () =>
            {
                // 将状态中的会话转换为接口定义的类型
                var sessions = new List<FakeMicro.Interfaces.UserSession>();
                foreach (var stateSession in UserState?.State?.Sessions ?? new List<UserSession>())
                {
                    if (stateSession == null) continue;
                    
                    sessions.Add(new FakeMicro.Interfaces.UserSession
                    {
                        SessionId = stateSession.SessionId,
                        LoginTime = stateSession.LoginTime,
                        LastActivity = stateSession.LastActivity,
                        IpAddress = stateSession.IpAddress,
                        UserAgent = stateSession.UserAgent,
                        IsCurrent = stateSession.IsCurrent
                    });
                }
                return sessions;
            });
        }

        /// <summary>
        /// 终止会话
        /// </summary>
        public async Task<TerminateSessionResult> TerminateSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync<TerminateSessionResult>("TerminateSessionAsync", async () =>
            {
                var result = UserState?.State?.RemoveSession(sessionId) ?? false;
                if (result)
                {
                    _logger.LogInformation("会话已终止: {SessionId}", sessionId);
                    return TerminateSessionResult.CreateSuccess(1);
                }
                else
                {
                    _logger.LogWarning("终止会话失败: 会话不存在: {SessionId}", sessionId);
                    return TerminateSessionResult.CreateFailed("会话不存在");
                }
            }, sessionId);
        }

        /// <summary>
        /// 获取用户权限（使用状态管理）
        /// </summary>
        [ReadOnly]
        public async Task<List<Permission>> GetPermissionsAsync(CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync<List<Permission>>("GetPermissionsAsync", async () =>
            {
                var permissions = new List<Permission>();
                
                // 根据用户角色生成权限
                var userRole = UserState?.State?.Role ?? "User";
                if (userRole == "Admin")
                {
                    permissions.AddRange(GenerateAdminPermissions());
                }
                else if (userRole == "ContentManager")
                {
                    permissions.AddRange(GenerateContentManagerPermissions());
                }
                else
                {
                    permissions.AddRange(GenerateUserPermissions());
                }
                
                return permissions;
            });
        }

        // 辅助方法
        private List<Permission> GenerateAdminPermissions()
        {
            return new List<Permission>
            {
                new Permission { Resource = "user", Type = PermissionType.Read },
                new Permission { Resource = "user", Type = PermissionType.Write },
                new Permission { Resource = "user", Type = PermissionType.Delete },
                new Permission { Resource = "user", Type = PermissionType.Manage },
                new Permission { Resource = "score", Type = PermissionType.Read },
                new Permission { Resource = "score", Type = PermissionType.Write },
                new Permission { Resource = "score", Type = PermissionType.Delete },
                new Permission { Resource = "exam", Type = PermissionType.Read },
                new Permission { Resource = "exam", Type = PermissionType.Write },
                new Permission { Resource = "exam", Type = PermissionType.Delete },
                new Permission { Resource = "dashboard", Type = PermissionType.Read }
            };
        }

        private List<Permission> GenerateContentManagerPermissions()
        {
            return new List<Permission>
            {
                new Permission { Resource = "user", Type = PermissionType.Read },
                new Permission { Resource = "score", Type = PermissionType.Read },
                new Permission { Resource = "score", Type = PermissionType.Write },
                new Permission { Resource = "exam", Type = PermissionType.Read },
                new Permission { Resource = "exam", Type = PermissionType.Write },
                new Permission { Resource = "dashboard", Type = PermissionType.Read }
            };
        }

        private List<Permission> GenerateUserPermissions()
        {
            return new List<Permission>
            {
                new Permission { Resource = "user", Type = PermissionType.Read },
                new Permission { Resource = "score", Type = PermissionType.Read },
                new Permission { Resource = "exam", Type = PermissionType.Read }
            };
        }

        private void GeneratePasswordHash(string password, out string hash, out string salt)
        {
            using var hmac = new HMACSHA512();
            salt = Convert.ToBase64String(hmac.Key);
            hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }

        private bool VerifyPasswordHash(string password, string hash, string salt)
        {
            try
            {
                var saltBytes = Convert.FromBase64String(salt);
                using var hmac = new HMACSHA512(saltBytes);
                var computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
                return computedHash == hash;
            }
            catch
            {
                return false;
            }
        }

        private async Task<T> SafeExecuteAsync<T>(string methodName, Func<Task<T>> operation, params object[] args)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }
            
            var safeArgs = args ?? Array.Empty<object>();
            
            try
            {
                _logger.LogTrace("开始执行方法: {MethodName}, 参数: {@Args}", methodName, safeArgs);
                var result = await operation();
                _logger.LogTrace("方法执行成功: {MethodName}", methodName);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "方法执行失败: {MethodName}, 参数: {@Args}", methodName, safeArgs);
                // 根据返回类型创建适当的失败结果
                if (typeof(T) == typeof(UpdateUserResult))
                {
                    return (T)(object)UpdateUserResult.CreateFailed(ex.Message);
                }
                else if (typeof(T) == typeof(TerminateSessionResult))
                {
                    return (T)(object)TerminateSessionResult.CreateFailed(ex.Message);
                }
                else if (typeof(T) == typeof(SendVerificationResult))
                {
                    return (T)(object)SendVerificationResult.CreateFailed(ex.Message);
                }
                else if (typeof(T) == typeof(VerifyEmailResult))
                {
                    return (T)(object)VerifyEmailResult.CreateFailed(ex.Message);
                }
                else if (typeof(T) == typeof(VerifyPhoneResult))
                {
                    return (T)(object)VerifyPhoneResult.CreateFailed(ex.Message);
                }
                else if (typeof(T) == typeof(RequestPasswordResetResult))
                {
                    return (T)(object)RequestPasswordResetResult.CreateFailed(ex.Message);
                }
                else if (typeof(T) == typeof(ResetPasswordResult))
                {
                    return (T)(object)ResetPasswordResult.CreateFailed(ex.Message);
                }
                else if (typeof(T) == typeof(ChangePasswordResult))
                {
                    return (T)(object)ChangePasswordResult.CreateFailed(ex.Message);
                }
                else if (typeof(T) == typeof(DeleteUserResult))
                {
                    return (T)(object)DeleteUserResult.CreateFailed(ex.Message);
                }
                else if (typeof(T) == typeof(bool))
                {
                    return (T)(object)false;
                }
                else if (typeof(T) == typeof(List<FakeMicro.Interfaces.UserSession>))
                {
                    return (T)(object)new List<FakeMicro.Interfaces.UserSession>();
                }
                else if (typeof(T) == typeof(List<Permission>))
                {
                    return (T)(object)new List<Permission>();
                }
                else if (typeof(T) == typeof(string))
                {
                    return (T)(object)string.Empty;
                }
                else if (typeof(T) == typeof(string))
                {
                    return (T)(object)null!;
                }
                else if (typeof(T) == typeof(UserDto))
                {
                    return (T)(object)null!;
                }
                throw;
            }
        }

        private async Task SafeExecuteAsync(string methodName, Func<Task> operation, params object[] args)
        {
            var safeArgs = args ?? Array.Empty<object>();
            
            try
            {
                _logger.LogTrace("开始执行方法: {MethodName}, 参数: {@Args}", methodName, safeArgs);
                await operation();
                _logger.LogTrace("方法执行成功: {MethodName}", methodName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "方法执行失败: {MethodName}, 参数: {@Args}", methodName, safeArgs);
                throw;
            }
        }

        /// <summary>
        /// 更新登录信息（使用状态管理 + 数据库同步）
        /// </summary>
        public async Task UpdateLoginInfoAsync(bool success, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
        {
            await SafeExecuteAsync("UpdateLoginInfoAsync", async () =>
            {
                // 更新状态
                UserState?.State?.UpdateLoginInfo(success);
                
                // 同步到数据库
                var userId = UserState?.State?.UserId ?? 0;
                if (userId > 0)
                {
                    await _repository.UpdateLoginInfoAsync(userId, success, DateTime.UtcNow, ipAddress, cancellationToken);
                }
                
                _logger.LogInformation("更新登录信息成功: {UserId}, 成功: {Success}", userId, success);
                return true;
            }, success.ToString());
        }

        /// <summary>
        /// 生成刷新令牌（使用数据库 + 状态管理）
        /// </summary>
        public async Task<string?> GenerateRefreshTokenAsync(CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync<string?>("GenerateRefreshTokenAsync", async () =>
            {
                var userId = UserState?.State?.UserId ?? 0;
                if (userId == 0)
                {
                    return null;
                }

                try
                {
                    // 使用数据库生成并保存刷新令牌
                    string refreshToken = await _repository.GenerateAndSaveRefreshTokenAsync(userId, cancellationToken);
                    
                    // 同步到状态管理
                    var expiresAt = DateTime.UtcNow.AddDays(7);
                    if (UserState?.State != null)
                    {
                        UserState.State.CurrentRefreshToken = refreshToken;
                        UserState.State.RefreshTokenExpiresAt = expiresAt;
                    }
                    if (UserState != null)
                    {
                        await UserState.WriteStateAsync();
                    }
                    
                    _logger.LogInformation("刷新令牌生成成功: {UserId}, 过期时间: {ExpiresAt}", 
                        userId, expiresAt);
                    return refreshToken;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "生成刷新令牌失败: {UserId}", userId);
                    return null;
                }
            });
        }

        /// <summary>
        /// 验证刷新令牌（使用数据库 + 状态管理）
        /// </summary>
        [ReadOnly]
        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync<bool>("ValidateRefreshTokenAsync", async () =>
            {
                var userId = UserState?.State?.UserId ?? 0;
                if (userId == 0 || string.IsNullOrEmpty(refreshToken))
                {
                    return false;
                }

                try
                {
                    // 首先使用数据库验证
                    bool isValidDb = await _repository.ValidateRefreshTokenAsync(userId, refreshToken, cancellationToken);
                    if (!isValidDb)
                    {
                        _logger.LogWarning("刷新令牌验证失败: 数据库验证失败: {UserId}", userId);
                        return false;
                    }

                    // 然后检查状态管理中的过期时间
                    var refreshTokenExpiresAt = UserState?.State?.RefreshTokenExpiresAt;
                    if (UserState?.State != null && refreshTokenExpiresAt.HasValue && refreshTokenExpiresAt.Value < DateTime.UtcNow)
                    {
                        _logger.LogWarning("刷新令牌验证失败: 令牌已过期: {UserId}, 过期时间: {ExpiresAt}", 
                            userId, refreshTokenExpiresAt.Value);
                        return false;
                    }
                    
                    _logger.LogInformation("刷新令牌验证成功: {UserId}", userId);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "验证刷新令牌失败: {UserId}", userId);
                    return false;
                }
            }, refreshToken);
        }

        /// <summary>
        /// 发送邮箱验证邮件
        /// </summary>
        public async Task<SendVerificationResult> SendEmailVerificationAsync(CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync<SendVerificationResult>("SendEmailVerificationAsync", async () =>
            {
                var userId = UserState?.State?.UserId ?? 0;
                if (userId == 0)
                {
                    return SendVerificationResult.CreateFailed("用户不存在");
                }

                try
                {
                    // 这里应该实现发送验证邮件的逻辑
                    // 记录日志
                    var email = UserState?.State?.Email ?? "未知邮箱";
                    _logger.LogInformation("发送邮箱验证邮件: {UserId}, {Email}", userId, email);
                    return SendVerificationResult.CreateSuccess();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "发送邮箱验证邮件失败: {UserId}", userId);
                    return SendVerificationResult.CreateFailed("发送邮箱验证邮件失败: " + ex.Message);
                }
            });
        }

        /// <summary>
        /// 验证邮箱
        /// </summary>
        public async Task<VerifyEmailResult> VerifyEmailAsync(string token, CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync<VerifyEmailResult>("VerifyEmailAsync", async () =>
            {
                var userId = UserState?.State?.UserId ?? 0;
                if (userId == 0)
                {
                    return VerifyEmailResult.CreateFailed("用户不存在");
                }

                try
                {
                    // 这里应该验证令牌并更新用户状态
                    if (UserState?.State != null)
                    {
                        UserState.State.EmailVerified = true;
                        UserState.State.IsActive = true;
                    }
                    if (UserState != null)
                    {
                        await UserState.WriteStateAsync();
                    }
                    _logger.LogInformation("邮箱验证成功: {UserId}", userId);
                    return VerifyEmailResult.CreateSuccess();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "邮箱验证失败: {UserId}", userId);
                    return VerifyEmailResult.CreateFailed("邮箱验证失败: " + ex.Message);
                }
            }, token);
        }

        /// <summary>
        /// 发送手机验证码
        /// </summary>
        public async Task<SendVerificationResult> SendPhoneVerificationAsync(CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync<SendVerificationResult>("SendPhoneVerificationAsync", async () =>
            {
                var userId = UserState?.State?.UserId ?? 0;
                if (userId == 0)
                {
                    return SendVerificationResult.CreateFailed("用户不存在");
                }

                try
                {
                    // 这里应该实现发送手机验证码的逻辑
                    _logger.LogInformation("发送手机验证码: {UserId}", userId);
                    return SendVerificationResult.CreateSuccess();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "发送手机验证码失败: {UserId}", userId);
                    return SendVerificationResult.CreateFailed("发送手机验证码失败: " + ex.Message);
                }
            });
        }

        /// <summary>
        /// 验证手机
        /// </summary>
        public async Task<VerifyPhoneResult> VerifyPhoneAsync(string code, CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync<VerifyPhoneResult>("VerifyPhoneAsync", async () =>
            {
                var userId = UserState?.State?.UserId ?? 0;
                if (userId == 0)
                {
                    return VerifyPhoneResult.CreateFailed("用户不存在");
                }

                try
                {
                    // 这里应该验证令牌并更新用户状态
                    if (UserState?.State != null)
                    {
                        UserState.State.PhoneVerified = true;
                        await UserState.WriteStateAsync();
                    }
                    _logger.LogInformation("手机验证成功: {UserId}", userId);
                    return VerifyPhoneResult.CreateSuccess();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "手机验证失败: {UserId}", userId);
                    return VerifyPhoneResult.CreateFailed("手机验证失败: " + ex.Message);
                }
            }, code);
        }

        /// <summary>
        /// 请求密码重置
        /// </summary>
        public async Task<RequestPasswordResetResult> RequestPasswordResetAsync(CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync<RequestPasswordResetResult>("RequestPasswordResetAsync", async () =>
            {
                var userId = UserState?.State?.UserId ?? 0;
                if (userId == 0)
                {
                    return RequestPasswordResetResult.CreateFailed("用户不存在");
                }

                try
                {
                    // 这里应该实现密码重置请求的逻辑
                    var email = UserState?.State?.Email ?? "未知邮箱";
                    _logger.LogInformation("密码重置请求: {UserId}, {Email}", userId, email);
                    return RequestPasswordResetResult.CreateSuccess();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "密码重置请求失败: {UserId}", userId);
                    return RequestPasswordResetResult.CreateFailed("密码重置请求失败: " + ex.Message);
                }
            });
        }

        /// <summary>
        /// 重置密码（使用状态管理 + 数据库操作）
        /// </summary>
        public async Task<ResetPasswordResult> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync<ResetPasswordResult>("ResetPasswordAsync", async () =>
            {
                var userId = UserState?.State?.UserId ?? 0;
                if (userId == 0)
                {
                    return ResetPasswordResult.CreateFailed("用户不存在");
                }

                try
                {
                    // 这里应该验证令牌
                    // 生成新密码的哈希和盐值
                    GeneratePasswordHash(newPassword, out string hash, out string salt);
                    
                    // 通过数据库更新密码
                    var user = await _repository.GetByIdAsync(userId);
                    if (user != null)
                    {
                        user.password_hash = hash;
                        user.password_salt = salt;
                        await _repository.UpdateAsync(user);

                        // 同步状态
                        if (UserState?.State != null)
                        {
                            UserState.State.PasswordHash = hash;
                            UserState.State.PasswordSalt = salt;
                            await UserState.WriteStateAsync();
                        }
                    }

                    _logger.LogInformation("密码重置成功: {UserId}", userId);
                    return ResetPasswordResult.CreateSuccess();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "密码重置失败: {UserId}", userId);
                    return ResetPasswordResult.CreateFailed("密码重置失败: " + ex.Message);
                }
            }, token, newPassword);
        }

        /// <summary>
        /// 修改密码（使用状态管理 + 数据库操作）
        /// </summary>
        public async Task<ChangePasswordResult> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                return ChangePasswordResult.CreateFailed("输入参数无效");
            }

            return await SafeExecuteAsync<ChangePasswordResult>("ChangePasswordAsync", async () =>
            {
                var userId = UserState?.State?.UserId ?? 0;
                if (userId == 0)
                {
                    return ChangePasswordResult.CreateFailed("用户不存在");
                }

                try
                {
                    // 验证当前密码
                    var user = await _repository.GetByIdAsync(userId);
                    if (user == null || !VerifyPasswordHash(currentPassword, user.password_hash, user.password_salt))
                    {
                        _logger.LogWarning("当前密码验证失败: {UserId}", userId);
                        return ChangePasswordResult.CreateFailed("当前密码不正确");
                    }

                    // 生成新密码的哈希和盐值
                    GeneratePasswordHash(newPassword, out string hash, out string salt);
                    
                    // 通过数据库更新密码
                    user.password_hash = hash;
                    user.password_salt = salt;
                    await _repository.UpdateAsync(user);

                    // 同步状态
                    if (UserState?.State != null)
                    {
                        UserState.State.PasswordHash = hash;
                        UserState.State.PasswordSalt = salt;
                        await UserState.WriteStateAsync();
                    }

                    _logger.LogInformation("密码修改成功: {UserId}", userId);
                    return ChangePasswordResult.CreateSuccess();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "密码修改失败: {UserId}", userId);
                    return ChangePasswordResult.CreateFailed("密码修改失败: " + ex.Message);
                }
            }, currentPassword, newPassword);
        }

        /// <summary>
        /// 检查用户是否有权限
        /// </summary>
        [ReadOnly]
        public async Task<bool> HasPermissionAsync(string resource, string permissionType, CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync<bool>("HasPermissionAsync", async () =>
            {
                var permissions = await GetPermissionsAsync();
                return permissions.Any(p => p.Resource == resource && p.Type.ToString() == permissionType);
            }, resource, permissionType);
        }

        /// <summary>
        /// 删除用户（使用状态管理 + 数据库操作）
        /// </summary>
        public async Task<DeleteUserResult> DeleteUserAsync(CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync<DeleteUserResult>("DeleteUserAsync", async () =>
            {
                var userId = UserState?.State?.UserId ?? 0;
                if (userId == 0)
                {
                    return DeleteUserResult.CreateFailed("用户不存在");
                }

                try
                {
                    var user = await _repository.GetByIdAsync(userId);
                    if (user != null)
                    {
                        await _repository.DeleteAsync(user);
                        
                        // 同步状态 - 标记为已删除状态而不是完全清空
                        if (UserState?.State != null)
                        {
                            UserState.State.IsActive = false;
                            await UserState.WriteStateAsync();
                        }
                    }
                    else
                    {
                        // 用户不存在，但仍清空状态
                        if (UserState?.State != null)
                        {
                            UserState.State.Reset();
                        }
                    }

                    _logger.LogInformation("删除用户成功: {UserId}", userId);
                    return DeleteUserResult.CreateSuccess(true); // 默认软删除
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "删除用户失败: {UserId}", userId);
                    return DeleteUserResult.CreateFailed("删除用户失败: " + ex.Message);
                }
            });
        }

        /// <summary>
        /// 终止所有其他会话（除当前会话外）
        /// </summary>
        public async Task<TerminateSessionResult> TerminateOtherSessionsAsync(CancellationToken cancellationToken = default)
        {
            return await SafeExecuteAsync<TerminateSessionResult>("TerminateOtherSessionsAsync", async () =>
            {
                var userId = UserState?.State?.UserId ?? 0;
                if (userId == 0)
                {
                    return TerminateSessionResult.CreateFailed("用户不存在");
                }

                var currentSession = UserState?.State?.Sessions?.FirstOrDefault(s => s.IsCurrent);
                
                // 只保留当前会话
                if (UserState?.State != null)
                {
                    UserState.State.Sessions = UserState.State.Sessions?.Where(s => s.IsCurrent).ToList() ?? new List<UserSession>();
                }
                var terminatedCount = (UserState?.State?.Sessions?.Count ?? 0) - (currentSession != null ? 1 : 0);
                
                _logger.LogInformation("已终止其他会话: 用户 {UserId}, 终止数量: {Count}", 
                    userId, terminatedCount);
                    
                return TerminateSessionResult.CreateSuccess(terminatedCount);
            });
        }
        
        /// <summary>
        /// 生成JWT令牌
        /// </summary>
        public async Task<string?> GenerateJwtTokenAsync()
        {
            return await SafeExecuteAsync<string?>("GenerateJwtTokenAsync", async () =>
            {
                var userId = UserState?.State?.UserId ?? 0;
                if (userId == 0)
                {
                    return null;
                }

                try
                {
                    // 在实际项目中，应该使用标准的JWT库
                    // 这里只是一个简化的示例
                    var username = UserState?.State?.Username ?? "未知用户";
                     var role = UserState?.State?.Role ?? "User";
                    var payload = $"{{\"userId\":{userId},\"username\":\"{username}\",\"role\":\"{role}\",\"exp\":{DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()}}}";
                    var encodedPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
                    
                    // 生成简化的签名（实际项目中应该使用HMAC-SHA256）
                    var signature = Convert.ToBase64String(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey.Substring(0, 32)));
                    
                    return $"{encodedPayload}.{signature}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "生成JWT令牌失败: {UserId}", userId);
                    return null;
                }
            });
        }
    }
}