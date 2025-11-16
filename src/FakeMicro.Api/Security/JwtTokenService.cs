using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using FakeMicro.Utilities.Configuration;
using System.Linq;

namespace FakeMicro.Api.Security
{
    /// <summary>
    /// 标准的JWT Token服务实现（遵循安全最佳实践）
    /// </summary>
    public class JwtTokenService
    {
        private readonly JwtConfig _jwtConfig;
        private readonly byte[] _signingKey;

        public JwtTokenService(IOptions<JwtConfig> jwtConfig)
        {
            _jwtConfig = jwtConfig?.Value ?? throw new ArgumentNullException(nameof(jwtConfig));
            
            // 验证配置
            if (string.IsNullOrEmpty(_jwtConfig.Secret))
                throw new ArgumentException("JWT Secret不能为空");
            if (_jwtConfig.Secret.Length < 32)
                throw new ArgumentException("JWT Secret长度至少需要32个字符");

            _signingKey = Encoding.UTF8.GetBytes(_jwtConfig.Secret);
        }

        /// <summary>
        /// 生成JWT Token
        /// </summary>
        public string GenerateToken(string userId, string username, string[] roles)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // 添加角色声明
            foreach (var role in roles)
            {
                claims = claims.Append(new Claim(ClaimTypes.Role, role)).ToArray();
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtConfig.ExpirationMinutes),
                Issuer = _jwtConfig.Issuer,
                Audience = _jwtConfig.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_signingKey), SecurityAlgorithms.HmacSha256)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// 验证JWT Token
        /// </summary>
        public ClaimsPrincipal ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            
            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(_signingKey),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtConfig.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtConfig.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // 减少时钟偏差以提高安全性
                };

                return tokenHandler.ValidateToken(token, validationParameters, out _);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 生成安全的刷新令牌
        /// </summary>
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        /// <summary>
        /// 从Token中获取用户ID
        /// </summary>
        public string GetUserIdFromToken(string token)
        {
            var principal = ValidateToken(token);
            return principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}