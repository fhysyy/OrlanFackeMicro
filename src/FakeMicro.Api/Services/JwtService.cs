using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace FakeMicro.Api.Services
{
    public class JwtService
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expireMinutes;

        public JwtService(IConfiguration configuration)
        {
            // 直接从配置获取JWT设置，确保与Program.cs中的配置一致
            var jwtSettings = configuration.GetSection("JwtSettings");
            
            // 获取密钥
            _secretKey = jwtSettings["SecretKey"] ?? "a-string-secret-at-least-256-bits-long";
            
            // 确保密钥存在且长度足够
            if (string.IsNullOrEmpty(_secretKey))
            {
                _secretKey = "a-string-secret-at-least-256-bits-long";
            }
            
            // 如果密钥长度不够，使用SHA256加密
            if (_secretKey.Length < 32)
            {
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(_secretKey));
                _secretKey = System.Convert.ToBase64String(hashedBytes).Substring(0, 32);
            }
            
            _issuer = jwtSettings["Issuer"] ?? "FakeMicro";
            _audience = jwtSettings["Audience"] ?? "FakeMicro-Users";
            
            // 关键修复：确保audience不为空，防止"empty"字符串的问题
            if (string.IsNullOrEmpty(_audience) || _audience.ToLower() == "empty")
            {
                _audience = "FakeMicro-Users";
            }
            
            _expireMinutes = int.TryParse(jwtSettings["ExpireMinutes"], out var expireMinutes) ? expireMinutes : 60;
        }

        public string GenerateToken(int userId, string username, string role)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_secretKey);
                
                // 最终修复：确保audience不会为"empty"
                var finalAudience = string.IsNullOrEmpty(_audience) || _audience.ToLower() == "empty" 
                    ? "FakeMicro-Users" 
                    : _audience;
                
                // 创建声明列表 - 使用明确的字符串类型确保一致性
                var claims = new List<Claim>
                {
                    new Claim("nameid", userId.ToString()),           // 明确使用nameid
                    new Claim("unique_name", username),               // 明确使用unique_name
                    new Claim("role", role),                         // 角色声明
                    new Claim(ClaimTypes.Role, role),                 // 同时添加标准Role声明类型
                    //new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddMinutes(_expireMinutes),
                    Issuer = !string.IsNullOrEmpty(_issuer) ? _issuer : "FakeMicro",
                    Audience = finalAudience, // 使用修复后的audience
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key), 
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                
                // 生成后验证令牌audience
                var jwtToken = token as JwtSecurityToken;
                if (jwtToken?.Audiences?.Any() == false)
                {
                    throw new InvalidOperationException("JWT令牌生成失败：audience为空");
                }
                
                return tokenHandler.WriteToken(token);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("JWT Token生成失败", ex);
            }
        }
        
        /// <summary>
        /// 使用角色数组生成令牌（更灵活的版本）
        /// </summary>
        public string GenerateToken(int userId, string username, string[] roles)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_secretKey);
                
                // 创建声明列表 - 使用与第一个方法完全一致的声明类型
                var claims = new List<Claim>
                {
                    new Claim("nameid", userId.ToString()),           // 明确使用nameid
                    new Claim("unique_name", username),               // 明确使用unique_name
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };
                
                // 添加所有角色作为单独的role声明，并同时添加标准Role声明类型
                foreach (var role in roles)
                {
                    claims.Add(new Claim("role", role));
                    claims.Add(new Claim(ClaimTypes.Role, role)); // 同时添加标准Role声明类型，确保与ASP.NET Core授权系统兼容
                }

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddMinutes(_expireMinutes),
                    Issuer = !string.IsNullOrEmpty(_issuer) ? _issuer : "FakeMicro",
                    Audience = _audience,
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key), 
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                return tokenHandler.WriteToken(token);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("JWT Token生成失败", ex);
            }
        }

        public ClaimsPrincipal ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            // 确保audience不为空
            var audience = !string.IsNullOrEmpty(_audience) ? _audience : "FakeMicro-Users";
            
            var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, // 与Program.cs保持一致，确保验证发行者
                    ValidateAudience = true, // 与Program.cs保持一致，确保验证受众
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _issuer,
                    ValidAudience = audience,
                    ValidAudiences = new[] { audience }, // 明确设置audiences数组
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    RoleClaimType = "role" // 确保使用与Program.cs中一致的角色声明类型
                };

            try
            {
                // 验证令牌
                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                
                // 如果令牌有效，确保角色声明被正确映射
                if (principal.Identity is ClaimsIdentity identity && validatedToken is JwtSecurityToken jwtToken)
                {
                    // 检查是否有role声明但没有ClaimTypes.Role声明
                    if (!identity.HasClaim(c => c.Type == ClaimTypes.Role) && 
                        identity.HasClaim(c => c.Type == "role"))
                    {
                        // 创建新的ClaimsIdentity，复制所有声明并添加ClaimTypes.Role声明
                        var newIdentity = new ClaimsIdentity(identity.AuthenticationType);
                        foreach (var claim in identity.Claims)
                        {
                            newIdentity.AddClaim(claim);
                        }
                        
                        // 添加从"role"到ClaimTypes.Role的映射
                        var roleClaim = identity.FindFirst("role");
                        if (roleClaim != null)
                        {
                            newIdentity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
                        }
                        
                        return new ClaimsPrincipal(newIdentity);
                    }
                }
                
                return principal;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Token验证失败", ex);
            }
        }
    }
}