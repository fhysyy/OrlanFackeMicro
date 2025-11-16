using System.ComponentModel.DataAnnotations;

namespace FakeMicro.Utilities.Configuration
{
    /// <summary>
    /// JWT配置类 - 统一使用此类作为JWT配置标准
    /// </summary>
    public class JwtConfig
    {
        /// <summary>
        /// JWT签名密钥
        /// </summary>
        [Required(ErrorMessage = "JWT签名密钥不能为空")]
        [MinLength(32, ErrorMessage = "JWT签名密钥长度至少需要32个字符")]
        public string Secret { get; set; } = "your-super-secret-key-at-least-32-characters-long";

        /// <summary>
        /// 令牌发行者
        /// </summary>
        [Required(ErrorMessage = "令牌发行者不能为空")]
        public string Issuer { get; set; } = "FakeMicro";

        /// <summary>
        /// 令牌接收者
        /// </summary>
        [Required(ErrorMessage = "令牌接收者不能为空")]
        public string Audience { get; set; } = "FakeMicro-Users";

        /// <summary>
        /// 令牌过期时间（分钟）
        /// </summary>
        [Range(1, 1440, ErrorMessage = "令牌过期时间应在1-1440分钟之间")]
        public int ExpirationMinutes { get; set; } = 60;

        /// <summary>
        /// 刷新令牌过期时间（天）
        /// </summary>
        [Range(1, 30, ErrorMessage = "刷新令牌过期时间应在1-30天之间")]
        public int RefreshExpirationDays { get; set; } = 7;

        /// <summary>
        /// 是否验证令牌生存期
        /// </summary>
        public bool ValidateLifetime { get; set; } = true;

        /// <summary>
        /// 是否验证发行者
        /// </summary>
        public bool ValidateIssuer { get; set; } = true;

        /// <summary>
        /// 是否验证接收者
        /// </summary>
        public bool ValidateAudience { get; set; } = true;

        /// <summary>
        /// 时钟偏差（秒）
        /// </summary>
        [Range(0, 300, ErrorMessage = "时钟偏差应在0-300秒之间")]
        public int ClockSkewSeconds { get; set; } = 30;
    }
}