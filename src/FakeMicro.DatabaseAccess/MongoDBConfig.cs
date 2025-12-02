using FakeMicro.DatabaseAccess.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

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

    /// <summary>
    /// MongoDB数据库连接管理器实现
    /// </summary>
    public class MongoDBConnectionManager : IDatabaseConnectionManager
    {
        private readonly IMongoClient _mongoClient;
        private readonly IMongoDatabase _database;
        private readonly ILogger<MongoDBConnectionManager> _logger;
        private readonly string _connectionString;
        private readonly string _databaseName;

        public MongoDBConnectionManager(MongoDBOptions options, ILogger<MongoDBConnectionManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionString = options.ConnectionString ?? throw new ArgumentNullException(nameof(options.ConnectionString));
            _databaseName = options.DatabaseName ?? throw new ArgumentNullException(nameof(options.DatabaseName));

            // 创建MongoDB客户端
            var mongoClientSettings = MongoClientSettings.FromConnectionString(_connectionString);
            mongoClientSettings.ConnectTimeout = TimeSpan.FromSeconds(options.ConnectionTimeout);
            mongoClientSettings.MaxConnectionPoolSize = options.MaxConnectionPoolSize;
            mongoClientSettings.MinConnectionPoolSize = options.MinConnectionPoolSize;

            _mongoClient = new MongoClient(mongoClientSettings);
            _database = _mongoClient.GetDatabase(_databaseName);
        }

        public DbConnection CreateConnection()
        {
            // MongoDB不使用传统的DbConnection
            // 记录警告日志，提示MongoDB使用特殊的连接方式
            _logger.LogWarning("MongoDB不使用传统的DbConnection，建议使用GetMongoClient()或GetDatabase()方法获取连接");
            return null;
        }

        public DatabaseType DatabaseType => DatabaseType.MongoDB;

        public async Task<DbConnection> GetConnectionAsync()
        {
            // MongoDB不使用传统的DbConnection
            // 记录警告日志，提示MongoDB使用特殊的连接方式
            _logger.LogWarning("MongoDB不使用传统的DbConnection，建议使用GetMongoClient()或GetDatabase()方法获取连接");
            return await Task.FromResult((DbConnection)null);
        }

        public string BuildConnectionString(DatabaseConfig config)
        {
            // MongoDB连接字符串构建逻辑
            return config?.ToString() ?? string.Empty;
        }

        public async Task<ConnectionTestResult> TestConnectionAsync()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                // 测试MongoDB连接
                await _database.RunCommandAsync((Command<Dictionary<string, object>>)"{ ping: 1 }");
                var endTime = DateTime.UtcNow;
                var latency = (endTime - startTime).TotalMilliseconds;

                return new ConnectionTestResult
                {
                    Success = true,
                    Message = "MongoDB连接测试成功",
                    Latency = latency,
                    PoolingInfo = "连接池已启用"
                };
            }
            catch (Exception ex)
            {
                return new ConnectionTestResult
                {
                    Success = false,
                    Error = ex.Message,
                    Message = "MongoDB连接测试失败"
                };
            }
        }

        public string GetConnectionString()
        {
            return _connectionString;
        }

        public ConnectionPoolStatus GetConnectionPoolStatus()
        {
            return new ConnectionPoolStatus
            {
                IsPoolingEnabled = true,
                PoolSize = _mongoClient.Settings.MaxConnectionPoolSize,
                MinPoolSize = _mongoClient.Settings.MinConnectionPoolSize,
                MaxPoolSize = _mongoClient.Settings.MaxConnectionPoolSize,
                ActiveConnections = 0, // MongoDB客户端不直接暴露活动连接数
                DatabaseType = DatabaseType.MongoDB,
                ConnectionStringConfigured = !string.IsNullOrEmpty(_connectionString),
                PoolName = "MongoDB Connection Pool"
            };
        }

        /// <summary>
        /// 获取MongoDB客户端
        /// </summary>
        public IMongoClient GetMongoClient()
        {
            return _mongoClient;
        }

        /// <summary>
        /// 获取MongoDB数据库
        /// </summary>
        public IMongoDatabase GetDatabase()
        {
            return _database;
        }
    }


}