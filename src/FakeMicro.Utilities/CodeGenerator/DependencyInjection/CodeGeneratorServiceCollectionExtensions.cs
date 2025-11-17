using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace FakeMicro.Utilities.CodeGenerator.DependencyInjection
{
    /// <summary>
    /// 代码生成器依赖注入扩展
    /// </summary>
    public static class CodeGeneratorServiceCollectionExtensions
    {
        /// <summary>
        /// 添加代码生成器服务
        /// </summary>
        public static IServiceCollection AddCodeGenerator(this IServiceCollection services, IConfiguration? configuration = null)
        {
            // 注册配置
            if (configuration != null)
            {
                services.Configure<CodeGeneratorConfiguration>(
                    configuration.GetSection("CodeGenerator"));
            }
            else
            {
                // 尝试从默认配置文件加载
                var config = LoadDefaultConfiguration();
                services.AddSingleton(config);
            }

            // 注册代码生成器
            services.AddScoped<global::FakeMicro.Utilities.CodeGenerator.CodeGenerator>();

            return services;
        }

        /// <summary>
        /// 添加代码生成器服务，使用自定义配置
        /// </summary>
        public static IServiceCollection AddCodeGenerator(this IServiceCollection services, CodeGeneratorConfiguration configuration)
        {
            services.AddSingleton(configuration);
            services.AddScoped<global::FakeMicro.Utilities.CodeGenerator.CodeGenerator>();

            return services;
        }

        /// <summary>
        /// 添加代码生成器服务，使用配置回调
        /// </summary>
        public static IServiceCollection AddCodeGenerator(this IServiceCollection services, Action<CodeGeneratorConfiguration> configureOptions)
        {
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            var config = new CodeGeneratorConfiguration();
            configureOptions(config);
            
            services.AddSingleton(config);
            services.AddScoped<global::FakeMicro.Utilities.CodeGenerator.CodeGenerator>();

            return services;
        }

        /// <summary>
        /// 加载默认配置
        /// </summary>
        private static CodeGeneratorConfiguration LoadDefaultConfiguration()
        {
            var config = new CodeGeneratorConfiguration();

            // 尝试从各种可能的位置加载配置文件
            var possiblePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.codegen.json"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "appsettings.codegen.json"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "appsettings.codegen.json"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "appsettings.codegen.json")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        var json = File.ReadAllText(path);
                        var loadedConfig = System.Text.Json.JsonSerializer.Deserialize<CodeGeneratorConfiguration>(json);
                        if (loadedConfig != null)
                        {
                            return loadedConfig;
                        }
                    }
                    catch
                    {
                        // 忽略配置文件错误，使用默认配置
                    }
                }
            }

            return config;
        }
    }
}