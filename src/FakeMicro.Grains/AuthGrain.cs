using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Entities;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
using FakeMicro.Utilities.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Orleans;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FakeMicro.Grains
{
    /// <summary>
    /// 认证Grain实现 - 遵循Orleans 9.x最佳实践
    /// </summary>
    public class AuthGrain : OrleansGrainBase, IAuthGrain
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtSettings _jwtSettings;

        public AuthGrain(
            IUserRepository userRepository, 
            ILogger<AuthGrain> logger,
            IOptions<JwtSettings> jwtSettings)
            : base(logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _jwtSettings = jwtSettings?.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
        }

        /// <summary>
        /// 生成雪花ID
        /// </summary>
        private long GenerateSnowflakeId()
        {
            // 简化实现，实际项目中应该使用更复杂的雪花ID生成算法
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var random = new Random();
            var machineId = random.Next(0, 1023); // 10位机器ID
            var sequence = random.Next(0, 4095);  // 12位序列号
            
            return (timestamp << 22) | (machineId << 12) | sequence;
        }

        public async Task<UserAuthResult> RegisterAsync(string username, string email, string password, string? displayName = null)
        {
            try
            {
                // 直接调用仓储进行基础验证
                var usernameExists = await _userRepository.UsernameExistsAsync(username);
                if (usernameExists)
                {
                    return new UserAuthResult { Success = false, Message = "用户名已存在" };
                }

                var emailExists = await _userRepository.EmailExistsAsync(email);
                if (emailExists)
                {
                    return new UserAuthResult { Success = false, Message = "邮箱已被注册" };
                }

                // 生成密码哈希和盐值
                var (passwordHash, passwordSalt) = GeneratePasswordHash(password);

                // 创建新用户
                var user = new User
                {
                    id = GenerateSnowflakeId(), // 生成雪花ID作为主键
                    username = username,
                    email = email,
                    display_name = displayName ?? username,
                    password_hash = passwordHash,
                    password_salt = passwordSalt,
                    role = FakeMicro.Entities.Enums.UserRole.User.ToString(),
                    status = FakeMicro.Entities.Enums.UserStatus.Active.ToString(),
                    created_at = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                    updated_at = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                    email_verified = false,
                    phone_verified = false,
                    is_deleted = false,
                    is_active = true
                };

                await _userRepository.AddAsync(user);
                await _userRepository.SaveChangesAsync();

                // 生成JWT令牌（使用内部方法，确保与RequireRole兼容）
                var token = GenerateJwtToken(user.id, user.username, new string[] { user.role });
                var refreshToken = GenerateRefreshToken();

                LogInformation("用户注册成功: {Username}", username);

                return new UserAuthResult
                {
                    Success = true,
                    Message = "注册成功",
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpireMinutes),
                    User = new UserInfo
                    {
                        Id = (long)user.id,
                        Username = user.username ?? string.Empty,
                        Email = user.email ?? string.Empty,
                        DisplayName = user.display_name ?? string.Empty,
                        Role = user.role ?? string.Empty,
                        CreatedAt = user.created_at
                    }
                };
            }
            catch (Exception ex)
            {
                LogError(ex, "用户注册失败: {Username}", username);
                return new UserAuthResult { Success = false, Message = $"注册失败{ex.Message}{ex.StackTrace}" };
            }
        }

        public async Task<UserAuthResult> LoginAsync(string username, string password)
        {
            try
            {
                // 查找用户（支持用户名或邮箱登录）

                var user = await _userRepository.GetByUsernameAsync(username) ??
                          await _userRepository.GetByEmailAsync(username);

                if (user == null || !user.is_active)
                {
                    return new UserAuthResult { Success = false, Message = "用户名或密码错误" };
                }

                // 验证密码
                if (!VerifyPasswordHash(password, user.password_hash, user.password_salt))
                {
                    return new UserAuthResult { Success = false, Message = "用户名或密码错误" };
                }

                // 生成JWT令牌（使用内部方法，确保与RequireRole兼容）
                var token = GenerateJwtToken(user.id, user.username, new string[] { user.role });
                var refreshToken = GenerateRefreshToken();

                // 更新用户信息
                user.last_login_at = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                user.updated_at = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                user.refresh_token = refreshToken;
                user.refresh_token_expiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshExpireDays);

                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();

                LogInformation("用户登录成功: {Username}", user.username);

                return new UserAuthResult
                {
                    Success = true,
                    Message = "登录成功",
                    Token = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpireMinutes),
                    User = new UserInfo
                    {
                        Id = (long)user.id,
                        Username = user.username ?? string.Empty,
                        Email = user.email ?? string.Empty,
                        DisplayName = user.display_name ?? string.Empty,
                        Role = user.role ?? string.Empty,
                        CreatedAt = user.created_at
                    }
                };
            }
            catch (Exception ex)
            {
                LogError(ex, "用户登录失败: {Username}", username);
                return new UserAuthResult { Success = false, Message = "登录失败" };
            }
        }

        public Task<UserAuthResult> ValidateTokenAsync(string token)
        {
            // 简化实现，实际项目中应该验证JWT Token
            return Task.FromResult(new UserAuthResult { Success = false, Message = "Token验证功能待实现" });
        }

        public async Task<UserAuthResult> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                // 查找拥有此刷新令牌的用户
                var user = await _userRepository.GetByRefreshTokenAsync(refreshToken);
                if (user == null || !user.is_active)
                {
                    return new UserAuthResult { Success = false, Message = "无效的刷新令牌" };
                }

                // 检查刷新令牌是否过期
                if (user.refresh_token_expiry != null && user.refresh_token_expiry < DateTime.UtcNow)
                {
                    return new UserAuthResult { Success = false, Message = "刷新令牌已过期" };
                }

                // 生成新的访问令牌（修复雪花ID类型转换问题）
                var newToken = GenerateJwtToken(user.id, user.username, new string[] { user.role });
                var newRefreshToken = GenerateRefreshToken();

                // 更新用户的刷新令牌和过期时间
                user.refresh_token = newRefreshToken;
                user.refresh_token_expiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshExpireDays);
                user.updated_at = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();

                LogInformation("令牌刷新成功: {Username}", user.username);

                return new UserAuthResult
                {
                    Success = true,
                    Message = "令牌刷新成功",
                    Token = newToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpireMinutes),
                    User = new UserInfo
                    {
                        Id = (long)user.id,
                        Username = user.username ?? string.Empty,
                        Email = user.email ?? string.Empty,
                        DisplayName = user.display_name ?? string.Empty,
                        Role = user.role ?? string.Empty,
                        CreatedAt = user.created_at
                    }
                };
            }
            catch (Exception ex)
            {
                LogError(ex, "Token刷新失败: {RefreshToken}", refreshToken);
                return new UserAuthResult { Success = false, Message = "令牌刷新失败" };
            }
        }

        public Task LogoutAsync()
        {
            LogInformation("用户登出: {UserId}", this.GetPrimaryKeyString());
            return Task.CompletedTask;
        }

        public async Task<bool> ChangePasswordAsync(string oldPassword, string newPassword)
        {
            try
            {
                var primaryKeyString = this.GetPrimaryKeyString();
                if (!long.TryParse(primaryKeyString, out long userId))
                {
                    LogError(new ArgumentException($"无效的用户ID格式: {primaryKeyString}"), "无效的用户ID格式");
                    throw new ArgumentException("无效的用户ID格式", nameof(primaryKeyString));
                }
                //var user = await _userRepository.GetByIdAsync(userId);

                //if (user == null || !VerifyPasswordHash(oldPassword, user.PasswordHash, user.PasswordSalt))
                //{
                //    return false;
                //}

                //var (newPasswordHash, newPasswordSalt) = GeneratePasswordHash(newPassword);
                //user.PasswordHash = newPasswordHash;
                //user.PasswordSalt = newPasswordSalt;

                //_userRepository.Update(user);
                //await _userRepository.SaveChangesAsync();

                LogInformation("密码修改成功: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex, "密码修改失败: {UserId}", this.GetPrimaryKeyString());
                return false;
            }
        }

        private (string hash, string salt) GeneratePasswordHash(string password)
        {
            using var hmac = new HMACSHA512();
            var salt = Convert.ToBase64String(hmac.Key);
            var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
            return (hash, salt);
        }

        private bool VerifyPasswordHash(string password, string storedHash, string storedSalt)
        {
            if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
                return false;

            var saltBytes = Convert.FromBase64String(storedSalt);
            using var hmac = new HMACSHA512(saltBytes);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(computedHash) == storedHash;
        }

        private string GenerateJwtToken(long userId, string username, string[] roles)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();

                // 使用与API项目一致的密钥处理逻辑
                var secretKey = _jwtSettings.SecretKey ?? "a-string-secret-at-least-256-bits-long";

                // 如果密钥长度不够，使用SHA256加密（与API项目保持一致）
                if (string.IsNullOrEmpty(secretKey))
                {
                    secretKey = "a-string-secret-at-least-256-bits-long";
                }
                else if (secretKey.Length < 32)
                {
                    using var sha256 = System.Security.Cryptography.SHA256.Create();
                    var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(secretKey));
                    secretKey = System.Convert.ToBase64String(hashedBytes).Substring(0, 32);
                }

                var key = Encoding.UTF8.GetBytes(secretKey);

                // 创建声明列表 - 与API项目完全一致
                var claims = new List<Claim>
                {
                    new Claim("nameid", userId.ToString()),           // 明确使用nameid
                    new Claim("unique_name", username),
                    new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),// 明确使用unique_name
                };

                // claims.Add(new Claim(ClaimTypes.Role, user.Role))
                // 添加角色声明，与API项目完全一致
                if (roles != null)
                {
                    foreach (var role in roles)
                    {
                        if (!string.IsNullOrEmpty(role))
                        {
                            claims.Add(new Claim("role", role));
                            claims.Add(new Claim(ClaimTypes.Role, role));
                        }
                    }
                }
                // 添加所有角色声明，与API项目保持一致
                //if (roles != null)
                //{
                //    foreach (var role in roles)
                //    {
                //        if (!string.IsNullOrEmpty(role))
                //        {
                //            claims.Add(new Claim("role", role));
                //            claims.Add(new Claim(ClaimTypes.Role, role));
                //        }
                //    }
                //}

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpireMinutes > 0 ? _jwtSettings.ExpireMinutes : 60),
                    Issuer = !string.IsNullOrEmpty(_jwtSettings.Issuer) ? _jwtSettings.Issuer : "FakeMicro",
                    Audience = "FakeMicro-Users",
                    //_jwtSettings.Audience ??
                    //Claims = claims.ToDictionary(c => c.Type, c => (object)c.Value),
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);
            }
            catch (Exception ex)
            {
                LogError(ex, "JWT令牌生成失败");
                throw new InvalidOperationException("JWT令牌生成失败", ex);
            }
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        // GetMachineId() 和 GenerateSnowflakeId() 方法现在由 BaseGrain 基类提供

        ///// <summary>
        ///// 生成雪花ID
        ///// </summary>
        //private long GenerateSnowflakeId()
        //{
        //    return _idGenerator.NextId();
        //}

        // 注意：GetMachineId方法已在上方定义，使用GetPrimaryKeyString()获取主键
    }

}