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
        public static IConfigurationBuilder AddDefaultConfiguration(this IConfigurationBuilder builder)
        {
            return builder
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
        }
    }
}