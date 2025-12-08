using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// MongoDB配置类
/// 提供MongoDB的配置和数据库连接管理
/// </summary>
public static class MongoDBConfig
{
    /// <summary>
    /// MongoDB配置选项
    /// </summary>
    public class MongoDBOptions
    {
        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// 数据库名称
        /// </summary>
        public string DatabaseName { get; set; } = "FakeMicroDB";

        /// <summary>
        /// 是否启用日志
        /// </summary>
        public bool EnableLogging { get; set; } = false;

        /// <summary>
        /// 连接超时时间（秒）
        /// </summary>
        public int ConnectionTimeout { get; set; } = 30;

        /// <summary>
        /// 最大连接池大小
        /// </summary>
        public int MaxConnectionPoolSize { get; set; } = 100;

        /// <summary>
        /// 最小连接池大小
        /// </summary>
        public int MinConnectionPoolSize { get; set; } = 10;
    }
}

