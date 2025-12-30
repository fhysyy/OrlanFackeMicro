using FakeMicro.Utilities.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace FakeMicro.Api.Extensions
{
    /// <summary>
    /// API项目Orleans扩展方法
    /// </summary>
    public static class OrleansExtensions
    {
        /// <summary>
        /// 添加默认配置
        /// </summary>
        // public static IConfigurationBuilder AddDefaultConfiguration(this IConfigurationBuilder builder)
        // {
        //     return builder
        //         .SetBasePath(Directory.GetCurrentDirectory())
        //         .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        //         .AddJsonFile($"appsettings.json", optional: true, reloadOnChange: true)
        //         .AddEnvironmentVariables();
        // }
        public static IConfigurationBuilder AddDefaultConfiguration(this IConfigurationBuilder builder,
          string envPrefix = "FAKEMICRO_",bool addCommandLine = true,params string[] args)
        {
            var configBuilder = builder
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
                            optional: true, reloadOnChange: true)
                .AddEnvironmentVariables(prefix: envPrefix)
                .AddEnvironmentPlaceholders();

            // 可选：添加命令行参数支持
            if (addCommandLine && args != null && args.Length > 0)
            {
                configBuilder.AddCommandLine(args);
            }

            return configBuilder;
        }
    }
}