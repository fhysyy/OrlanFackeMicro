using System.Data.Common;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// 连接池健康状态
/// </summary>
public class ConnectionHealthStatus
{
    /// <summary>
    /// 连接池是否健康
    /// </summary>
    public bool IsHealthy { get; set; }
    
    /// <summary>
    /// 状态描述
    /// </summary>
    public string Status { get; set; } = string.Empty;
    
    /// <summary>
    /// 连接池状态信息
    /// </summary>
    public ConnectionPoolStatus ConnectionPoolStatus { get; set; } = new ConnectionPoolStatus();
    
    /// <summary>
    /// 详细信息
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 连接延迟（毫秒）
    /// </summary>
    public double ConnectionLatency { get; set; }
}

/// <summary>
/// 连接池性能指标
/// </summary>
public class ConnectionPoolMetrics
{
    /// <summary>
    /// 活动连接数
    /// </summary>
    public int ActiveConnections { get; set; }
    
    /// <summary>
    /// 空闲连接数
    /// </summary>
    public int IdleConnections { get; set; }
    
    /// <summary>
    /// 连接池使用百分比
    /// </summary>
    public double PoolUsagePercentage { get; set; }
    
    /// <summary>
    /// 平均连接时间（毫秒）
    /// </summary>
    public double AverageConnectionTime { get; set; }
    
    /// <summary>
    /// 当前时间戳
    /// </summary>
    public DateTime CurrentTimestamp { get; set; }
    
    // 基础指标
    public int MinPoolSize { get; set; }
    public int MaxPoolSize { get; set; }
    public int ErrorCount { get; set; }
    
    // 性能指标
    public int ConnectionTimeout { get; set; }
    
    // 时间指标
    public double PoolAgeSeconds { get; set; }
    public DateTime LastRefreshTime { get; set; }
    
    // 配置指标
    public string? Server { get; set; }
    public string? Database { get; set; }
    public string? DatabaseType { get; set; }
    
    // 健康指标
    public bool IsPoolingEnabled { get; set; }
}

/// <summary>
/// PostgreSQL数据库连接管理器
/// </summary>
public class PostgreSqlConnectionManager : BaseDatabaseConnectionManager
{
    private new readonly ILogger<PostgreSqlConnectionManager>? _logger;
    private readonly object _poolSyncLock = new object();
    private int _connectionErrorCount = 0;
    private int _maxConsecutiveErrors = 5; // 最大连续错误数
    private DateTime _lastPoolRefreshTime = DateTime.UtcNow;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="config">数据库配置</param>
    /// <param name="logger">日志记录器</param>
    public PostgreSqlConnectionManager(DatabaseConfig config, ILogger<PostgreSqlConnectionManager>? logger = null)
        : base(config, logger)
    {
        // 验证数据库类型
        if (config.Type != DatabaseType.PostgreSQL)
        {
            throw new ArgumentException("数据库配置类型必须为PostgreSQL", nameof(config));
        }
        
        // 设置默认端口
        if (config.Port <= 0)
        {
            config.Port = DatabaseConfig.GetDefaultPort(DatabaseType.PostgreSQL);
        }
        
        _logger = logger;
        
        // 初始化完成，配置信息已存储在_config中
        
        _logger?.LogInformation("PostgreSQL连接管理器初始化完成");
    }

    /// <summary>
    /// 创建数据库连接
    /// </summary>
    /// <returns>数据库连接</returns>
    public override DbConnection CreateConnection()
    {
        try
        {
            var connection = new NpgsqlConnection(GetConnectionString());
            
            // 配置连接池选项
            var builder = new NpgsqlConnectionStringBuilder(connection.ConnectionString)
            {
                Pooling = true,
                MinPoolSize = 5,
                MaxPoolSize = 100,
                ConnectionIdleLifetime = 600, // 10分钟（秒）
                KeepAlive = 30
            };
            
            connection.ConnectionString = builder.ConnectionString;
            return connection;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "创建PostgreSQL连接失败");
            throw;
        }
    }



    /// <summary>
    /// 内部构建连接字符串
    /// </summary>
    /// <param name="config">数据库配置</param>
    /// <returns>连接字符串</returns>
    protected override string BuildConnectionStringInternal(DatabaseConfig config)
    {
        // 从环境变量获取密码（优先级最高）
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? config.Password;
        
        // 使用参数化连接字符串构建器
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = config.Server,
            Port = config.Port,
            Database = config.Database,
            Username = config.Username,
            Password = password,
            Timeout = config.ConnectionTimeout, // Npgsql中连接超时参数为Timeout
            CommandTimeout = config.ConnectionTimeout,
            // SSL配置
            SslMode = config.TrustServerCertificate ? SslMode.Require : SslMode.Prefer,
            // 移除过时的TrustServerCertificate属性，使用更现代的SSL配置
            // 连接池配置
            Pooling = true,
            MinPoolSize = config.MinPoolSize > 0 ? config.MinPoolSize : 5,
            MaxPoolSize = config.MaxPoolSize > 0 ? config.MaxPoolSize : 100,
            ConnectionIdleLifetime = 600, // 10分钟
            ConnectionPruningInterval = 30, // 连接修剪间隔（秒）
            KeepAlive = 30,
            // 编码配置
            Encoding = "UTF8",
            // 性能优化
            NoResetOnClose = false,
            ApplicationName = "FakeMicro",
            // 高级性能参数
            TcpKeepAlive = true,
            InternalCommandTimeout = 30,
            IncludeErrorDetail = true,
        };

        return builder.ConnectionString;
    }

        /// <summary>
        /// 获取数据库连接池状态
        /// 增强的连接池监控实现
        /// </summary>
        /// <returns>连接池状态信息</returns>
        public override ConnectionPoolStatus GetConnectionPoolStatus()
        {
            try
            {
                // 收集连接池关键指标
                var status = new ConnectionPoolStatus
                {
                    // 基础配置信息
                    IsPoolingEnabled = true,
                    MinPoolSize = _config.MinPoolSize,
                    MaxPoolSize = _config.MaxPoolSize,
                    
                    // 配置信息
                    DatabaseType = DatabaseType,
                    PoolName = $"PostgreSQL_{_config.Server}_{_config.Database}"
                };
                
                // 记录连接池状态日志
                _logger?.LogInformation("连接池状态: Min={Min}, Max={Max}, Errors={Errors}, Age={Age}s, Timeout={Timeout}s",
                    status.MinPoolSize, status.MaxPoolSize, _connectionErrorCount, 
                    (DateTime.UtcNow - _lastPoolRefreshTime).TotalSeconds.ToString("F0"), _config.ConnectionTimeout);
                
                return status;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取PostgreSQL连接池状态失败");
                return base.GetConnectionPoolStatus();
            }
        }
        
        /// <summary>
        /// 检查连接池健康状态
        /// </summary>
        /// <returns>健康状态结果</returns>
        public ConnectionHealthStatus CheckConnectionPoolHealth()
        {
            try
            {
                var status = GetConnectionPoolStatus();
                var isHealthy = _connectionErrorCount < _maxConsecutiveErrors;
                
                var healthStatus = new ConnectionHealthStatus
                {
                    IsHealthy = isHealthy,
                    Status = isHealthy ? "健康" : "异常",
                    ConnectionPoolStatus = status,
                    Message = isHealthy 
                        ? "数据库连接池运行正常" 
                        : $"连接池异常: 连续错误 {_connectionErrorCount}/{_maxConsecutiveErrors}"
                };
                
                // 如果连接池不健康，记录警告日志
                if (!isHealthy)
                {
                    _logger?.LogWarning("连接池健康检查失败: {Message}", healthStatus.Message);
                }
                else
                {
                    _logger?.LogDebug("连接池健康检查通过");
                }
                
                return healthStatus;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "连接池健康检查失败");
                return new ConnectionHealthStatus
                {
                    IsHealthy = false,
                    Status = "未知",
                    Message = "健康检查执行失败: " + ex.Message
                };
            }
        }
        
        /// <summary>
        /// 异步检查连接池健康状态
        /// </summary>
        /// <returns>健康状态结果</returns>
        public async Task<ConnectionHealthStatus> CheckConnectionPoolHealthAsync()
        {
            // 异步测试连接以验证健康状态
            var connectionTestResult = await TestConnectionAsync();
            var poolStatus = CheckConnectionPoolHealth();
            
            // 合并测试结果
            poolStatus.IsHealthy &= connectionTestResult.Success;
            poolStatus.ConnectionLatency = connectionTestResult.Latency;
            
            if (!connectionTestResult.Success)
            {
                poolStatus.Message += $"; 连接测试失败: {connectionTestResult.Message}";
                _logger?.LogWarning("连接测试失败: {Message}", connectionTestResult.Message);
            }
            
            return poolStatus;
        }
    
        /// <summary>
        /// 刷新连接池
        /// 提供外部调用的连接池重置方法
        /// </summary>
        public void RefreshConnectionPool()
        {
            lock (_poolSyncLock)
            {
                RefreshConnectionPoolInternal();
            }
        }
        
        /// <summary>
        /// 刷新连接池内部方法
        /// </summary>
        private void RefreshConnectionPoolInternal()
        {
            try
            {
                // 这里可以实现连接池的刷新逻辑
                // 例如重置连接计数器或其他维护操作
                _logger?.LogDebug("连接池已刷新");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "刷新连接池时发生错误");
                throw;
            }
        }
        
        /// <summary>
        /// 获取连接池统计指标
        /// 用于监控系统集成
        /// </summary>
        /// <returns>连接池统计指标</returns>
        public ConnectionPoolMetrics GetConnectionPoolMetrics()
        {
            var status = GetConnectionPoolStatus();
            
            return new ConnectionPoolMetrics
            {
                // 基础指标
                MinPoolSize = status.MinPoolSize,
                MaxPoolSize = status.MaxPoolSize,
                ErrorCount = _connectionErrorCount,
                
                // 性能指标
                ConnectionTimeout = _config.ConnectionTimeout,
                
                // 时间指标
                PoolAgeSeconds = (DateTime.UtcNow - _lastPoolRefreshTime).TotalSeconds,
                LastRefreshTime = _lastPoolRefreshTime,
                
                // 配置指标
                Server = _config.Server,
                Database = _config.Database,
                DatabaseType = _config.Type.ToString(),
                
                // 健康指标
                IsPoolingEnabled = status.IsPoolingEnabled,
                CurrentTimestamp = DateTime.UtcNow
            };
        }
        
        /// <summary>
        /// 优化连接池配置
        /// 可根据系统负载动态调整
        /// </summary>
        /// <param name="newMaxPoolSize">新的最大连接池大小</param>
        public void OptimizePoolConfiguration(int newMaxPoolSize)
        {
            lock (_poolSyncLock)
            {
                // 验证新配置
                if (newMaxPoolSize < _config.MinPoolSize || newMaxPoolSize > 500)
                {
                    throw new ArgumentException($"最大连接池大小必须在 {_config.MinPoolSize} 到 500 之间");
                }
                
                _logger?.LogInformation("优化连接池配置: MaxPoolSize从{OldSize}调整为{NewSize}", 
                    _config.MaxPoolSize, newMaxPoolSize);
                
                // 更新配置
                _config.MaxPoolSize = newMaxPoolSize;
                
                // 刷新连接池以应用新配置
                RefreshConnectionPoolInternal();
            }
        }
}