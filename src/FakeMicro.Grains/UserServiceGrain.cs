using FakeMicro.Entities;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
using FakeMicro.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Concurrency;
using System.Security.Cryptography;
using System.Text;
using FakeMicro.DatabaseAccess;
using FakeMicro.DatabaseAccess.Transaction;
using FakeMicro.DatabaseAccess.Interfaces;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;
using System.Collections.Generic;
using SqlSugar;

// 使用别名解决命名空间冲突
using UserRoleEnum = FakeMicro.Entities.Enums.UserRole;
using UserStatusEnum = FakeMicro.Entities.Enums.UserStatus;
using FakeMicro.Utilities.Configuration;
using System.Text.Json;

namespace FakeMicro.Grains
{
    [StatelessWorker(10)] // 设置为无状态工作者，提高吞吐量
    [Reentrant]
    public class UserServiceGrain : Grain, IUserServiceGrain
    {
        private readonly ILogger<UserServiceGrain> _logger;
        private readonly JwtSettings _jwtSettings;
        private readonly IUserRepository _userRepository;
        private readonly ITransactionService _transactionService;
        private readonly IGrainFactory _grainFactory;

        public UserServiceGrain(
            ILogger<UserServiceGrain> logger, 
            IOptions<JwtSettings> jwtSettings, 
            IUserRepository userRepository,
            ITransactionService transactionService,
            IGrainFactory grainFactory)
        {
            _logger = logger;
            _jwtSettings = jwtSettings.Value;
            _userRepository = userRepository;
            _transactionService = transactionService;
            _grainFactory = grainFactory;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            // 参数验证
            if (string.IsNullOrWhiteSpace(request.Username) || 
                string.IsNullOrWhiteSpace(request.Email) || 
                string.IsNullOrWhiteSpace(request.Password))
            {
                return new AuthResponse { Success = false, ErrorMessage = "用户名、邮箱和密码不能为空" };
            }

            // 生成密码哈希和盐值
            GeneratePasswordHash(request.Password, out string passwordHash, out string passwordSalt);

            try
            {
                // 设置超时令牌源
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                
                // 在事务中执行注册过程
                AuthResponse response = null;
                await _transactionService.ExecuteInTransactionAsync(async () => {
                    timeoutCts.Token.ThrowIfCancellationRequested();
                    
                    // 检查用户名是否已存在
                    if (await _userRepository.UsernameExistsAsync(request.Username))
                    {
                        _logger.LogWarning("用户名已存在: {Username}", request.Username);
                        response = new AuthResponse { Success = false, ErrorMessage = "用户名已存在" };
                        return;
                    }

                    // 检查邮箱是否已存在
                    if (await _userRepository.EmailExistsAsync(request.Email))
                    {
                        _logger.LogWarning("邮箱已存在: {Email}", request.Email);
                        response = new AuthResponse { Success = false, ErrorMessage = "邮箱已被注册" };
                        return;
                    }

                    // 创建新用户 - 使用安全的密码存储机制
                    var user = new User
                    {
                        username = request.Username,
                        email = request.Email,
                        phone = request.Phone ?? string.Empty,
                        display_name = request.DisplayName ?? request.Username,
                        password_hash = passwordHash,
                        password_salt = passwordSalt,  // 保存盐值
                        role = UserRoleEnum.User.ToString(),
                        status = UserStatusEnum.Pending.ToString(),
                        is_active = false,
                        email_verified = false,
                        phone_verified = false,
                        created_at = DateTime.UtcNow,
                        updated_at = DateTime.UtcNow,
                        last_login_at = null
                    };

                    // 保存用户到数据库
                    await _userRepository.AddAsync(user);
                    _logger.LogInformation("创建用户成功: {Username}", user.username);
                    
                    // 异步预热用户Grain，不阻塞主流程
                    _ = Task.Run(async () => {
                        try {
                            var userGrain = _grainFactory.GetGrain<IUserGrain>(user.id.ToString());
                            await userGrain.InitializeAsync(cancellationToken);
                            _logger.LogDebug("用户Grain预热完成: {UserId}", user.id);
                        } catch (Exception ex) {
                            _logger.LogError(ex, "预热用户Grain失败: {UserId}", user.id);
                        }
                    });

                    // 生成令牌
                    var accessToken = GenerateToken(user);
                    var refreshToken = GenerateRefreshToken();

                    var userDto = new UserDto
                    {
                        Id = user.id,
                        Username = user.username,
                        Email = user.email,
                        Phone = user.phone,
                        DisplayName = user.display_name,
                        Role = Enum.TryParse<UserRoleEnum>(user.role, out var role) ? role : UserRoleEnum.User,
                        Status = Enum.TryParse<UserStatusEnum>(user.status, out var statusEnum) ? statusEnum : UserStatusEnum.Pending,
                        CreatedAt = user.created_at,
                        UpdatedAt = user.updated_at.Value
                    };

                    response = new AuthResponse
                    {
                        Success = true,
                        Token = accessToken,
                        RefreshToken = refreshToken,
                        User = userDto
                    };
                });
                return response;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("用户注册操作超时: {Username}", request.Username);
                return new AuthResponse { Success = false, ErrorMessage = "注册操作超时，请稍后重试" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建用户失败: {Username}", request.Username);
                return new AuthResponse { Success = false, ErrorMessage = "注册失败，请稍后重试" };
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                // 根据用户名或邮箱查找用户
                var user = await _userRepository.GetByUsernameAsync(request.UsernameOrEmail);
                if (user == null)
                {
                    // 尝试通过邮箱查找
                    user = await _userRepository.GetByEmailAsync(request.UsernameOrEmail);
                }
                if (user == null)
                {
                    _logger.LogWarning("用户不存在: {UsernameOrEmail}", request.UsernameOrEmail);
                    return new AuthResponse { Success = false, ErrorMessage = "用户名或密码错误" };
                }

                // 验证密码 - 使用盐值
                if (!VerifyPassword(request.Password, user.password_hash, user.password_salt))
                {
                    _logger.LogWarning("密码错误: {UsernameOrEmail}", request.UsernameOrEmail);
                    // 更新登录失败信息（移除UserGrain调用）
                    await _userRepository.UpdateLoginInfoAsync(user.id, false, DateTime.UtcNow, 
                        loginIp: null, cancellationToken: cancellationToken);
                    return new AuthResponse { Success = false, ErrorMessage = "用户名或密码错误" };
                }

                // 检查用户状态
                if (!user.is_active || user.status != UserStatusEnum.Active.ToString())
                {
                    _logger.LogWarning("用户未激活或状态异常: {UsernameOrEmail}", request.UsernameOrEmail);
                    return new AuthResponse { Success = false, ErrorMessage = "账号状态异常，请联系管理员" };
                }

                // 更新登录信息（移除UserGrain调用，直接使用Repository）
                await _userRepository.UpdateLoginInfoAsync(user.id, true, DateTime.UtcNow, 
                    loginIp: null, cancellationToken: cancellationToken);

                // 生成令牌
                var accessToken = GenerateToken(user);
                var refreshToken = await _userRepository.GenerateAndSaveRefreshTokenAsync(user.id, cancellationToken);

                _logger.LogInformation("用户登录成功: {UsernameOrEmail}", request.UsernameOrEmail);
                var userDto = new UserDto
                {
                    Id = user.id,
                    Username = user.username,
                    Email = user.email,
                    Phone = user.phone,
                    Role = Enum.TryParse<UserRoleEnum>(user.role, out var role) ? role : UserRoleEnum.User,
                    Status = Enum.TryParse<UserStatusEnum>(user.status, out var statusEnum) ? statusEnum : UserStatusEnum.Pending,
                    CreatedAt = user.created_at,
                    UpdatedAt = user.updated_at.Value
                };

                return new AuthResponse
                {
                    Success = true,
                    Token = accessToken,
                    RefreshToken = refreshToken,
                    User = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户登录失败: {UsernameOrEmail}", request.UsernameOrEmail);
                return new AuthResponse { Success = false, ErrorMessage = "登录失败" };
            }
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                // 参数验证
                if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.RefreshToken))
                {
                    return new AuthResponse { Success = false, ErrorMessage = "令牌和刷新令牌不能为空" };
                }

                // 从令牌中提取用户ID
                var userId = ParseUserIdFromToken(request.Token);
                if (userId <= 0)
                {
                    return new AuthResponse { Success = false, ErrorMessage = "无效的令牌" };
                }

                // 直接从数据库获取用户信息（移除UserGrain调用）
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("用户不存在: {UserId}", userId);
                    return new AuthResponse { Success = false, ErrorMessage = "用户不存在" };
                }

                // 验证刷新令牌（直接查询数据库）
                if (string.IsNullOrEmpty(user.refresh_token) || user.refresh_token != request.RefreshToken)
                {
                    _logger.LogWarning("刷新令牌无效: {UserId}", userId);
                    return new AuthResponse { Success = false, ErrorMessage = "无效的刷新令牌" };
                }

                // 检查用户状态
                if (!user.is_active || user.status != UserStatusEnum.Active.ToString())
                {
                    _logger.LogWarning("用户未激活或状态异常: {UserId}", userId);
                    return new AuthResponse { Success = false, ErrorMessage = "用户状态异常" };
                }

                // 生成新的令牌
                var accessToken = GenerateToken(user);
                var newRefreshToken = GenerateRefreshToken();

                // 更新数据库中的刷新令牌
                user.refresh_token = newRefreshToken;
                user.updated_at = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("刷新令牌成功: {UserId}", userId);
                var userDto = new UserDto
                {
                    Id = user.id,
                    Username = user.username,
                    Email = user.email,
                    Phone = user.phone,
                    DisplayName = user.display_name,
                    Role = Enum.TryParse<UserRoleEnum>(user.role, out var role) ? role : UserRoleEnum.User,
                    Status = Enum.TryParse<UserStatusEnum>(user.status, out var statusEnum) ? statusEnum : UserStatusEnum.Pending,
                    CreatedAt = user.created_at,
                    UpdatedAt = user.updated_at.Value
                };

                return new AuthResponse
                {
                    Success = true,
                    Token = accessToken,
                    RefreshToken = newRefreshToken,
                    User = userDto
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新令牌失败");
                return new AuthResponse { Success = false, ErrorMessage = "刷新令牌失败" };
            }
        }

        public async Task<bool> LogoutAsync(long userId, CancellationToken cancellationToken = default)
        {
            try
            {
                // 直接更新数据库中的用户状态（移除UserGrain调用）
                await _userRepository.UpdateLoginInfoAsync(userId, false, DateTime.UtcNow, 
                    loginIp: null, cancellationToken: cancellationToken);
                
                _logger.LogInformation("用户登出成功: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户登出失败: {UserId}", userId);
                return false;
            }
        }

        public async Task<long?> FindUserByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default)
        {
            try
            {
                // 先尝试按用户名查找
                var user = await _userRepository.GetByUsernameAsync(usernameOrEmail);
                if (user == null)
                {
                    // 再尝试按邮箱查找
                    user = await _userRepository.GetByEmailAsync(usernameOrEmail);
                }
                return user?.id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查找用户失败: {UsernameOrEmail}", usernameOrEmail);
                return null;
            }
        }

        public async Task<bool> ValidateUserPermissionAsync(long userId, string resource, string permissionType, CancellationToken cancellationToken = default)
        {
            try
            {
                // 直接从数据库获取用户信息（移除UserGrain调用）
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null || !user.is_active)
                {
                    return false;
                }
                
                // 简单的权限验证逻辑（根据角色判断基本权限）
                // 这里可以根据具体需求扩展为更复杂的权限系统
                switch (user.role)
                {
                    case "Admin":
                        // 管理员拥有所有权限
                        return true;
                    case "User":
                        // 普通用户权限检查
                        return resource.StartsWith("user") || resource.StartsWith("profile");
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证用户权限失败: {UserId}, {Resource}, {PermissionType}", userId, resource, permissionType);
                return false;
            }
        }

        public async Task<UserStatistics> GetUserStatisticsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var users = await _userRepository.GetAllAsync();
                
                return new UserStatistics
                {
                    TotalUsers = users.Count(),
                    ActiveUsers = users.Count(u => u.status == UserStatusEnum.Active.ToString()),
                    NewUsersToday = users.Count(u => u.created_at.Date == DateTime.UtcNow.Date),
                    StatusDistribution = users.GroupBy(u => u.status).ToDictionary(g => g.Key, g => g.Count()),
                    RoleDistribution = users.GroupBy(u => u.role).ToDictionary(g => g.Key, g => g.Count())
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户统计信息失败");
                return new UserStatistics();
            }
        }

        public async Task<List<UserDto>> GetUsersAsync(string? username = null, string? email = null, string? status = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var users = await _userRepository.SearchUsersAsync(username, email, status);
                
                // 转换为DTO
                return users.Select(u => new UserDto
                {
                    Id = u.id,
                    Username = u.username,
                    Email = u.email,
                    Phone = u.phone,
                    Role = Enum.TryParse<UserRoleEnum>(u.role, out var role) ? role : UserRoleEnum.User,
                    Status = Enum.TryParse<UserStatusEnum>(u.status, out var statusEnum) ? statusEnum : UserStatusEnum.Pending,
                    CreatedAt = u.created_at,
                    UpdatedAt = u.updated_at.Value
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户列表失败");
                return new List<UserDto>();
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 生成密码哈希和盐值 - 与UserGrain保持一致的安全机制
        /// </summary>
        private void GeneratePasswordHash(string password, out string hash, out string salt)
        {
            using var hmac = new HMACSHA512();
            salt = Convert.ToBase64String(hmac.Key);
            hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }

        /// <summary>
        /// 验证密码 - 与UserGrain保持一致的安全机制
        /// </summary>
        private bool VerifyPassword(string password, string hash, string salt)
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

        /// <summary>
        /// 生成JWT令牌
        /// </summary>
        private string GenerateToken(User user)
        {
            // 生成标准JWT格式的token
            var header = new { alg = "HS256", typ = "JWT" };
            var payload = new
            {
                sub = user.id.ToString(),           // subject - 用户ID (主要字段)
                nameId = user.id.ToString(),         // 兼容性字段
                user_id = user.id,                   // 数字字段 (备用)
                unique_name = user.username,         // 用户名
                username = user.username,            // 用户名 (备用)
                jti = Guid.NewGuid().ToString(),     // JWT ID
                iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                nbf = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                exp = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.ExpireMinutes).ToUnixTimeSeconds(),
                iss = "FakeMicro",
                aud = "FakeMicro-Users",
                // 添加role字段便于前端处理
                role = user.role ?? "User",
                // 添加权限数组
                roles = user.role != null ? new[] { user.role } : new[] { "User" }
            };
            
            // 序列化payload
            var payloadJson = JsonSerializer.Serialize(payload);
            var headerJson = JsonSerializer.Serialize(header);
            
            // Base64编码
            var encodedHeader = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            var encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
            
            // 生成签名
            var data = $"{encodedHeader}.{encodedPayload}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var signature = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
            
            return $"{data}.{signature}";
        }
        
        /// <summary>
        /// Base64URL编码
        /// </summary>
        private string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        /// <summary>
        /// 生成刷新令牌
        /// </summary>
        private string GenerateRefreshToken()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var randomBytes = new byte[32];
                rng.GetBytes(randomBytes);
                return Convert.ToBase64String(randomBytes);
            }
        }

        /// <summary>
        /// Base64URL解码
        /// </summary>
        private byte[] Base64UrlDecode(string input)
        {
            var base64 = input.Replace('-', '+').Replace('_', '/');
            
            // 添加必要的填充
            while (base64.Length % 4 != 0)
            {
                base64 += "=";
            }
            
            return Convert.FromBase64String(base64);
        }

        /// <summary>
        /// 从令牌中解析用户ID
        /// </summary>
        private long ParseUserIdFromToken(string token)
        {
            try
            {
                // 检查令牌格式
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("令牌为空");
                    return 0;
                }

                var parts = token.Split('.');
                if (parts.Length < 2)
                {
                    _logger.LogWarning("令牌格式无效: {Token}", token);
                    return 0;
                }

                try
                {
                    // 使用Base64URL解码payload
                    var payloadBytes = Base64UrlDecode(parts[1]);
                    var payload = Encoding.UTF8.GetString(payloadBytes);
                    
                    // 解析JSON
                    using var document = JsonDocument.Parse(payload);
                    var root = document.RootElement;
                    
                    // 尝试从不同的字段获取用户ID
                    if (root.TryGetProperty("sub", out var subElement) && subElement.ValueKind == JsonValueKind.String)
                    {
                        var userIdStr = subElement.GetString();
                        if (long.TryParse(userIdStr, out var userId))
                        {
                            _logger.LogDebug("从sub字段解析用户ID: {UserId}", userId);
                            return userId;
                        }
                    }
                    
                    // 备用字段nameId
                    if (root.TryGetProperty("nameId", out var nameIdElement) && nameIdElement.ValueKind == JsonValueKind.String)
                    {
                        var userIdStr = nameIdElement.GetString();
                        if (long.TryParse(userIdStr, out var userId))
                        {
                            _logger.LogDebug("从nameId字段解析用户ID: {UserId}", userId);
                            return userId;
                        }
                    }
                    
                    // 备用字段user_id
                    if (root.TryGetProperty("user_id", out var userIdElement) && userIdElement.ValueKind == JsonValueKind.Number)
                    {
                        var userId = userIdElement.GetInt64();
                        _logger.LogDebug("从user_id字段解析用户ID: {UserId}", userId);
                        return userId;
                    }
                    
                    _logger.LogWarning("在令牌中未找到有效的用户ID字段. Payload: {Payload}", payload);
                    return 0;
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "解析令牌JSON失败");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析令牌失败: {Token}", token);
                return 0;
            }
        }
        #endregion
    }
}