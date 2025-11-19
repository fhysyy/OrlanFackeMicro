using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace FakeMicro.Utilities.Configuration.Extensions
{
    /// <summary>
    /// 环境变量占位符配置提供程序
    /// 用于解析配置文件中的 ${变量名} 格式的环境变量占位符
    /// </summary>
    public class EnvironmentPlaceholderConfigurationProvider : ConfigurationProvider
    {
        private readonly IConfigurationRoot _root;
        private readonly Regex _placeholderRegex = new Regex(@"\$\{(\w+)\}", RegexOptions.Compiled);

        public EnvironmentPlaceholderConfigurationProvider(IConfigurationRoot root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
        }

        public override void Load()
        {
            // 从其他配置提供程序获取数据
            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            ProcessConfiguration(_root, data);
            Data = data;
        }

        private void ProcessConfiguration(IConfiguration config, Dictionary<string, string?> result, string parentKey = "")
        {
            foreach (var child in config.GetChildren())
            {
                var fullKey = string.IsNullOrEmpty(parentKey) ? child.Key : $"{parentKey}:{child.Key}";
                var value = child.Value;
                if (!string.IsNullOrEmpty(value))
                {
                    // 替换占位符
                    var processedValue = ReplacePlaceholders(value);
                    result[fullKey] = processedValue;
                }
                
                // 递归处理子配置
                ProcessConfiguration(child, result, fullKey);
            }
        }

        private string ReplacePlaceholders(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return _placeholderRegex.Replace(input, match =>
            {
                if (match.Groups.Count > 1)
                {
                    var variableName = match.Groups[1].Value;
                    var envValue = Environment.GetEnvironmentVariable(variableName);
                    
                    // 如果环境变量不存在，则保留原始占位符
                    return envValue ?? match.Value;
                }
                return match.Value;
            });
        }

        /// <summary>
        /// 刷新配置（支持热更新）
        /// </summary>
        public void Refresh()
        {
            Load();
            OnReload();
        }
    }

    /// <summary>
    /// 环境变量占位符配置源
    /// </summary>
    public class EnvironmentPlaceholderConfigurationSource : IConfigurationSource
    {
        private IConfigurationRoot _root;

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            // 获取已构建的配置根
            _root = builder.Build();
            return new EnvironmentPlaceholderConfigurationProvider(_root);
        }
    }

    /// <summary>
    /// 配置构建器扩展方法
    /// </summary>
    public static class EnvironmentPlaceholderConfigurationExtensions
    {
        /// <summary>
        /// 添加环境变量占位符解析支持
        /// </summary>
        /// <param name="builder">配置构建器</param>
        /// <returns>配置构建器</returns>
        public static IConfigurationBuilder AddEnvironmentPlaceholders(this IConfigurationBuilder builder)
        {
            builder.Add(new EnvironmentPlaceholderConfigurationSource());
            return builder;
        }
    }
}