using System;
using System.Text.RegularExpressions;

namespace FakeMicro.Utilities.Configuration
{
    /// <summary>
    /// 环境变量配置助手 - 支持从环境变量替换配置值
    /// </summary>
    public static class EnvironmentConfigHelper
    {
        private static readonly Regex EnvVarPattern = new Regex(@"\$\{([^}]+)\}", RegexOptions.Compiled);

        /// <summary>
        /// 解析配置字符串，替换环境变量占位符
        /// 支持格式: ${ENV_VAR_NAME} 或 ${ENV_VAR_NAME:default_value}
        /// </summary>
        /// <param name="value">配置值</param>
        /// <returns>解析后的值</returns>
        public static string ResolveEnvironmentVariables(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return EnvVarPattern.Replace(value, match =>
            {
                var envVarExpression = match.Groups[1].Value;
                var parts = envVarExpression.Split(':', 2);
                var envVarName = parts[0].Trim();
                var defaultValue = parts.Length > 1 ? parts[1].Trim() : null;

                var envValue = Environment.GetEnvironmentVariable(envVarName);
                
                if (!string.IsNullOrEmpty(envValue))
                {
                    return envValue;
                }

                if (defaultValue != null)
                {
                    return defaultValue;
                }

                // 如果没有默认值且环境变量不存在，抛出异常
                throw new InvalidOperationException(
                    $"Environment variable '{envVarName}' is not set and no default value provided.");
            });
        }

        /// <summary>
        /// 验证必需的环境变量是否已设置
        /// </summary>
        /// <param name="requiredVariables">必需的环境变量名称数组</param>
        /// <exception cref="InvalidOperationException">当必需变量未设置时抛出</exception>
        public static void ValidateRequiredVariables(params string[] requiredVariables)
        {
            var missingVariables = new System.Collections.Generic.List<string>();

            foreach (var variable in requiredVariables)
            {
                var value = Environment.GetEnvironmentVariable(variable);
                if (string.IsNullOrWhiteSpace(value))
                {
                    missingVariables.Add(variable);
                }
            }

            if (missingVariables.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Required environment variables are missing: {string.Join(", ", missingVariables)}. " +
                    "Please check your .env file or environment configuration.");
            }
        }

        /// <summary>
        /// 获取环境变量，如果不存在则使用默认值
        /// </summary>
        public static string GetEnvironmentVariable(string name, string defaultValue = "")
        {
            return Environment.GetEnvironmentVariable(name) ?? defaultValue;
        }

        /// <summary>
        /// 生成安全的JWT密钥（用于开发环境）
        /// </summary>
        public static string GenerateSecureJwtKey()
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var bytes = new byte[64];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
