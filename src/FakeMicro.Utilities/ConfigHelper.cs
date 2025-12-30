using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace FakeMicro.Utilities
{
    /// <summary>
    /// 配置助手类
    /// </summary>
    public static class ConfigHelper
    {
        /// <summary>
        /// 从JSON文件加载配置
        /// </summary>
        public static T? LoadFromJsonFile<T>(string filePath) where T : class, new()
        {
            if (!File.Exists(filePath)) return new T();
            
            try
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new T();
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"Failed to load configuration from {filePath}", ex);
                return new T();
            }
        }

        /// <summary>
        /// 异步从JSON文件加载配置
        /// </summary>
        public static async Task<T?> LoadFromJsonFileAsync<T>(string filePath) where T : class, new()
        {
            if (!File.Exists(filePath)) return new T();
            
            try
            {
                using var stream = File.OpenRead(filePath);
                return await JsonSerializer.DeserializeAsync<T>(stream, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new T();
            }
            catch (Exception ex)
            {
                await LoggerHelper.LogAsync(LoggerHelper.LogLevel.Error, $"Failed to load configuration from {filePath}", ex);
                return new T();
            }
        }

        /// <summary>
        /// 保存配置到JSON文件
        /// </summary>
        public static void SaveToJsonFile<T>(T config, string filePath) where T : class
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                LoggerHelper.Error($"Failed to save configuration to {filePath}", ex);
            }
        }

        /// <summary>
        /// 异步保存配置到JSON文件
        /// </summary>
        public static async Task SaveToJsonFileAsync<T>(T config, string filePath) where T : class
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                using var stream = File.Create(filePath);
                await JsonSerializer.SerializeAsync(stream, config, options);
            }
            catch (Exception ex)
            {
                await LoggerHelper.LogAsync(LoggerHelper.LogLevel.Error, $"Failed to save configuration to {filePath}", ex);
            }
        }

        /// <summary>
        /// 合并配置对象
        /// </summary>
        public static T MergeConfigurations<T>(T primary, T secondary) where T : class, new()
        {
            if (primary == null) return secondary ?? new T();
            if (secondary == null) return primary;

            var result = new T();
            var properties = typeof(T).GetProperties();

            foreach (var property in properties)
            {
                if (property.CanRead && property.CanWrite)
                {
                    var primaryValue = property.GetValue(primary);
                    var secondaryValue = property.GetValue(secondary);

                    // 如果主配置有值，使用主配置；否则使用备用配置
                    var value = primaryValue ?? secondaryValue;
                    property.SetValue(result, value);
                }
            }

            return result;
        }

        /// <summary>
        /// 验证配置对象
        /// </summary>
        public static bool ValidateConfiguration<T>(T config, out List<string> errors) where T : class
        {
            errors = new List<string>();
            
            if (config == null)
            {
                errors.Add("Configuration object is null");
                return false;
            }

            // 这里可以添加更复杂的验证逻辑
            // 例如检查必需字段、数值范围等

            return errors.Count == 0;
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        public static T CreateDefaultConfig<T>() where T : class, new()
        {
            return new T();
        }

        /// <summary>
        /// 配置提供者基类
        /// </summary>
        public abstract class ConfigurationProvider<TConfigType> where TConfigType : class, new()
        {
            protected TConfigType _configuration = new TConfigType();

            /// <summary>
            /// 当前配置
            /// </summary>
            public TConfigType Configuration => _configuration;

            /// <summary>
            /// 重新加载配置
            /// </summary>
            public abstract void Reload();

            /// <summary>
            /// 异步重新加载配置
            /// </summary>
            public abstract Task ReloadAsync();

            /// <summary>
            /// 保存配置
            /// </summary>
            public abstract void Save();

            /// <summary>
            /// 异步保存配置
            /// </summary>
            public abstract Task SaveAsync();
        }

        /// <summary>
        /// JSON文件配置提供者
        /// </summary>
        public class JsonFileConfigurationProvider<TConfig> : ConfigurationProvider<TConfig> where TConfig : class, new()
        {
            private readonly string _filePath;

            public JsonFileConfigurationProvider(string filePath)
            {
                _filePath = filePath;
                Reload();
            }

            public override void Reload()
            {
                _configuration = LoadFromJsonFile<TConfig>(_filePath) ?? new TConfig();
            }

            public override async Task ReloadAsync()
            {
                _configuration = await LoadFromJsonFileAsync<TConfig>(_filePath) ?? new TConfig();
            }

            public override void Save()
            {
                SaveToJsonFile(_configuration, _filePath);
            }

            public override async Task SaveAsync()
            {
                await SaveToJsonFileAsync(_configuration, _filePath);
            }
        }
    }
}