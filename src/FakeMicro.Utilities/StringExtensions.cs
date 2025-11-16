using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FakeMicro.Utilities
{
    /// <summary>
    /// 字符串扩展方法
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// 检查字符串是否为null或空
        /// </summary>
        public static bool IsNullOrEmpty(this string? value)
        {
            return string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// 检查字符串是否为null、空或仅包含空白字符
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string? value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// 安全地转换为大写
        /// </summary>
        public static string? ToUpperSafe(this string? value)
        {
            return value?.ToUpperInvariant();
        }

        /// <summary>
        /// 安全地转换为小写
        /// </summary>
        public static string? ToLowerSafe(this string? value)
        {
            return value?.ToLowerInvariant();
        }

        /// <summary>
        /// 安全地修剪字符串
        /// </summary>
        public static string? TrimSafe(this string? value)
        {
            return value?.Trim();
        }

        /// <summary>
        /// 检查字符串是否包含指定的子字符串（不区分大小写）
        /// </summary>
        public static bool ContainsIgnoreCase(this string? value, string substring)
        {
            if (value == null || substring == null) return false;
            return value.Contains(substring, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 将字符串转换为驼峰命名法
        /// </summary>
        public static string ToCamelCase(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            
            var words = Regex.Split(value, @"\W+|(?<=[a-z])(?=[A-Z])");
            if (words.Length == 0) return value;
            
            var result = new StringBuilder();
            result.Append(words[0].ToLowerInvariant());
            
            for (int i = 1; i < words.Length; i++)
            {
                if (!string.IsNullOrEmpty(words[i]))
                {
                    result.Append(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(words[i].ToLowerInvariant()));
                }
            }
            
            return result.ToString();
        }

        /// <summary>
        /// 将字符串转换为帕斯卡命名法
        /// </summary>
        public static string ToPascalCase(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            
            var camelCase = value.ToCamelCase();
            if (string.IsNullOrEmpty(camelCase)) return camelCase;
            
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(camelCase);
        }

        /// <summary>
        /// 安全地获取子字符串
        /// </summary>
        public static string? SafeSubstring(this string? value, int startIndex, int length)
        {
            if (value == null) return null;
            if (startIndex < 0) startIndex = 0;
            if (startIndex >= value.Length) return string.Empty;
            if (startIndex + length > value.Length) length = value.Length - startIndex;
            
            return value.Substring(startIndex, length);
        }

        /// <summary>
        /// 安全地获取子字符串（从开始到指定长度）
        /// </summary>
        public static string? SafeSubstring(this string? value, int length)
        {
            return value.SafeSubstring(0, length);
        }

        /// <summary>
        /// 将字符串转换为Base64编码
        /// </summary>
        public static string ToBase64(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            var bytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// 从Base64解码字符串
        /// </summary>
        public static string FromBase64(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            var bytes = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// 检查字符串是否为有效的电子邮件地址
        /// </summary>
        public static bool IsValidEmail(this string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            
            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
                return regex.IsMatch(value);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查字符串是否为有效的URL
        /// </summary>
        public static bool IsValidUrl(this string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            
            try
            {
                return Uri.TryCreate(value, UriKind.Absolute, out _);
            }
            catch
            {
                return false;
            }
        }
    }
}