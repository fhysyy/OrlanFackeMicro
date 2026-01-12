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
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.DatabaseAccess.Transaction;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;
using FakeMicro.Utilities.Configuration;
using System.Text.Json;

// 使用别名解决命名空间冲突
using UserRoleEnum = FakeMicro.Entities.Enums.UserRole;
using UserStatusEnum = FakeMicro.Entities.Enums.UserStatus;

namespace FakeMicro.Grains
{
    /// <summary>
    /// 认证Grain - 负责用户注册、登录和令牌管理
    /// </summary>
    [StatelessWorker(10)]
    [Reentrant]
    public class AuthenticationGrain : OrleansGrainBase, IAuthenticationGrain
    {
        private readonly ILogger<AuthenticationGrain> _logger;
        private readonly JwtSettings _jwtSettings;
        private readonly IUserRepository _userRepository;
        private readonly ITransactionService _transactionService;

        public AuthenticationGrain(
            ILogger<AuthenticationGrain> logger, 
            IOptions<JwtSettings> jwtSettings, 
            IUserRepository userRepository,
            ITransactionService transactionService) : base(logger)
        {
            _logger = logger;
            _jwtSettings = jwtSettings.Value;
            _userRepository = userRepository;
            _transactionService = transactionService;
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
            _logger.LogInformation("=== 开始生成密码哈希 ===");
            _logger.LogInformation("用户名: {Username}", request.Username);
            _logger.LogInformation("密码长度: {PasswordLength}", request.Password?.Length ?? 0);
            GeneratePasswordHash(request.Password, out string passwordHash, out string passwordSalt);
            _logger.LogInformation("生成的哈希长度: {HashLength}", passwordHash.Length);
            _logger.LogInformation("生成的盐长度: {SaltLength}", passwordSalt.Length);

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
                        password_salt = passwordSalt,
                        role = UserRoleEnum.User.ToString(),
                        status = UserStatusEnum.Pending.ToString(),
                        is_active = false,
                        email_verified = false,
                        phone_verified = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        last_login_at = null
                    };

                    // 保存用户到数据库
                    await _userRepository.AddAsync(user);
                    _logger.LogInformation("创建用户成功: {Username}", user.username);
                    
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
                        CreatedAt = user.CreatedAt,
                        UpdatedAt = user.UpdatedAt.Value
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

                // 验证密码
                _logger.LogInformation("=== 开始登录验证 ===");
                _logger.LogInformation("用户: {UsernameOrEmail}", request.UsernameOrEmail);
                _logger.LogInformation("用户状态: {Status}, 是否激活: {IsActive}", user.status, user.is_active);
                
                if (!VerifyPassword(request.Password, user.password_hash, user.password_salt))
                {
                    _logger.LogWarning("密码验证失败: {UsernameOrEmail}", request.UsernameOrEmail);
                    // 更新登录失败信息
                    await _userRepository.UpdateLoginInfoAsync(user.id, false, DateTime.UtcNow, 
                        loginIp: null, cancellationToken: cancellationToken);
                    return new AuthResponse { Success = false, ErrorMessage = "用户名或密码错误" };
                }
                
                _logger.LogInformation("密码验证成功: {UsernameOrEmail}", request.UsernameOrEmail);

                // 检查用户状态
                if (!user.is_active || user.status != UserStatusEnum.Active.ToString())
                {
                    _logger.LogWarning("用户未激活或状态异常: {UsernameOrEmail}", request.UsernameOrEmail);
                    return new AuthResponse { Success = false, ErrorMessage = "账号状态异常，请联系管理员" };
                }

                // 更新登录信息
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
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt.Value
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
                _logger.LogInformation("开始刷新令牌");

                // 参数验证
                if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.RefreshToken))
                {
                    _logger.LogWarning("令牌和刷新令牌不能为空");
                    return new AuthResponse { Success = false, ErrorMessage = "令牌和刷新令牌不能为空" };
                }

                // 从令牌中提取用户ID
                var userId = ParseUserIdFromToken(request.Token);
                if (userId <= 0)
                {
                    _logger.LogWarning("无效的令牌，无法解析用户ID");
                    return new AuthResponse { Success = false, ErrorMessage = "无效的令牌" };
                }

                _logger.LogInformation("从令牌解析到用户ID: {UserId}", userId);

                // 直接从数据库获取用户信息
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("用户不存在: {UserId}", userId);
                    return new AuthResponse { Success = false, ErrorMessage = "用户不存在" };
                }

                _logger.LogInformation("找到用户: {Username}, 状态: {Status}, 是否激活: {IsActive}", user.username, user.status, user.is_active);

                // 验证刷新令牌
                if (string.IsNullOrEmpty(user.refresh_token))
                {
                    _logger.LogWarning("用户没有刷新令牌: {UserId}", userId);
                    return new AuthResponse { Success = false, ErrorMessage = "无效的刷新令牌" };
                }

                if (user.refresh_token != request.RefreshToken)
                {
                    _logger.LogWarning("刷新令牌不匹配: {UserId}", userId);
                    return new AuthResponse { Success = false, ErrorMessage = "无效的刷新令牌" };
                }

                // 检查用户状态
                if (!user.is_active)
                {
                    _logger.LogWarning("用户未激活: {UserId}", userId);
                    return new AuthResponse { Success = false, ErrorMessage = "用户未激活" };
                }

                if (user.status != UserStatusEnum.Active.ToString())
                {
                    _logger.LogWarning("用户状态异常: {UserId}, 当前状态: {Status}", userId, user.status);
                    return new AuthResponse { Success = false, ErrorMessage = $"用户状态异常: {user.status}" };
                }

                // 生成新的令牌
                var accessToken = GenerateToken(user);
                var newRefreshToken = GenerateRefreshToken();

                _logger.LogInformation("生成新令牌成功，准备更新数据库");

                // 更新数据库中的刷新令牌
                user.refresh_token = newRefreshToken;
                user.UpdatedAt = DateTime.UtcNow.EnsureUtc();

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
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt.Value
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
                _logger.LogError(ex, "刷新令牌失败: {Message}, 堆栈: {StackTrace}", ex.Message, ex.StackTrace);
                return new AuthResponse { Success = false, ErrorMessage = $"刷新令牌失败: {ex.Message}" };
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 生成密码哈希和盐值
        /// </summary>
        private void GeneratePasswordHash(string password, out string hash, out string salt)
        {
            // 使用PBKDF2生成更安全的密码哈希
            var combinedHash = CryptoHelper.GeneratePasswordHash(password);
            var hashBytes = Convert.FromBase64String(combinedHash);
            
            // 分割盐和哈希（前16字节是盐，后面是哈希）
            salt = Convert.ToBase64String(hashBytes.Take(16).ToArray());
            hash = Convert.ToBase64String(hashBytes.Skip(16).ToArray());
            
            _logger.LogDebug("生成密码哈希成功，盐长度: {SaltLength}, 哈希长度: {HashLength}", salt.Length, hash.Length);
        }

        /// <summary>
        /// 验证密码
        /// </summary>
        private bool VerifyPassword(string password, string hash, string salt)
        {
            _logger.LogInformation("=== VerifyPassword 开始 ===");
            _logger.LogInformation("输入密码长度: {PasswordLength}", password?.Length ?? 0);
            _logger.LogInformation("存储哈希长度: {HashLength}", hash?.Length ?? 0);
            _logger.LogInformation("存储盐长度: {SaltLength}", salt?.Length ?? 0);
            
            try
            {
                _logger.LogInformation("尝试PBKDF2验证（新格式）");
                
                var saltBytes = Convert.FromBase64String(salt);
                var hashBytes = Convert.FromBase64String(hash);
                
                var combinedHashBytes = new byte[saltBytes.Length + hashBytes.Length];
                Array.Copy(saltBytes, 0, combinedHashBytes, 0, saltBytes.Length);
                Array.Copy(hashBytes, 0, combinedHashBytes, saltBytes.Length, hashBytes.Length);
                var combinedHash = Convert.ToBase64String(combinedHashBytes);
                
                if (CryptoHelper.VerifyPasswordHash(password, combinedHash))
                {
                    _logger.LogInformation("✓ PBKDF2验证成功");
                    return true;
                }
                else
                {
                    _logger.LogWarning("✗ PBKDF2验证失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ PBKDF2验证异常，尝试旧格式");
            }

            try
            {
                _logger.LogInformation("尝试HMACSHA512验证（旧格式）");
                
                var result = CryptoHelper.VerifyLegacyPasswordHash(password, hash, salt);
                
                if (result)
                {
                    _logger.LogInformation("✓ HMACSHA512验证成功");
                }
                else
                {
                    _logger.LogWarning("✗ HMACSHA512验证失败");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ HMACSHA512验证异常");
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
                sub = user.id.ToString(),
                nameId = user.id.ToString(),
                user_id = user.id,
                unique_name = user.username,
                username = user.username,
                jti = Guid.NewGuid().ToString(),
                iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                nbf = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                exp = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.ExpireMinutes).ToUnixTimeSeconds(),
                iss = "FakeMicro",
                aud = "FakeMicro-Users",
                role = user.role ?? "User",
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
