using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using FakeMicro.DatabaseAccess.Interfaces;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// 数据库连接管理器基类
/// 提供数据库连接的基础功能
/// </summary>
public abstract class BaseDatabaseConnectionManager : IDatabaseConnectionManager
{
    protected readonly DatabaseConfig _config;
    protected readonly ILogger<BaseDatabaseConnectionManager>? _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="config">数据库配置</param>
    /// <param name="logger">日志记录器</param>
    protected BaseDatabaseConnectionManager(DatabaseConfig config, ILogger<BaseDatabaseConnectionManager>? logger = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger;
        
        // 验证配置
        ValidateConfig();
    }
    
    /// <summary>
    /// 初始化方法（用于子类调用）
    /// </summary>
    /// <param name="config">数据库配置</param>
    /// <param name="logger">日志记录器</param>
    protected void Init(DatabaseConfig config, ILogger? logger)
    {
        // 验证配置
        if (config == null)
            throw new ArgumentNullException(nameof(config));
            
        // 验证配置
        ValidateConfig();
    }

    /// <summary>
    /// 验证数据库配置
    /// </summary>
    protected virtual void ValidateConfig()
    {
        if (_config == null)
            throw new ArgumentNullException(nameof(_config));
            
        // 验证数据库类型
        if (!Enum.IsDefined(typeof(DatabaseType), _config.Type))
            throw new ArgumentException($"无效的数据库类型: {_config.Type}");
            
        // 验证必要的配置值
        if (_config.Type != DatabaseType.SQLite)
        {
            if (string.IsNullOrEmpty(_config.Server))
                throw new ArgumentException("数据库服务器地址必须指定");
            
            if (_config.Port <= 0)
                throw new ArgumentException("数据库端口必须指定");
            
            if (string.IsNullOrEmpty(_config.Database))
                throw new ArgumentException("数据库名称必须指定");
        }
        else
        {
            // SQLite不需要服务器、端口、用户名和密码
            if (string.IsNullOrEmpty(_config.Database))
                throw new ArgumentException("SQLite数据库文件路径必须指定");
        }
    }

    /// <summary>
    /// 创建数据库连接
    /// </summary>
    /// <returns>数据库连接</returns>
    public abstract DbConnection CreateConnection();

    /// <summary>
    /// 获取数据库连接（异步）
    /// </summary>
    /// <returns>数据库连接</returns>
    public async Task<DbConnection> GetConnectionAsync()
    {
        var connection = CreateConnection();
        await connection.OpenAsync();
        return connection;
    }

    /// <summary>
    /// 构建连接字符串
    /// </summary>
    /// <param name="config">数据库配置</param>
    /// <returns>连接字符串</returns>
    public string BuildConnectionString(DatabaseConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));
        
        return BuildConnectionStringInternal(config);
    }

    /// <summary>
    /// 内部构建连接字符串
    /// </summary>
    /// <param name="config">数据库配置</param>
    /// <returns>连接字符串</returns>
    protected abstract string BuildConnectionStringInternal(DatabaseConfig config);

    /// <summary>
    /// 测试数据库连接
    /// </summary>
    /// <returns>连接测试结果</returns>
    public async Task<ConnectionTestResult> TestConnectionAsync()
    {
        using var connection = CreateConnection();
        try
        {
            _logger?.LogInformation("正在测试数据库连接...");
            var startTime = DateTime.UtcNow;
            await connection.OpenAsync();
            var endTime = DateTime.UtcNow;
            var latency = (endTime - startTime).TotalMilliseconds;
            
            _logger?.LogInformation($"数据库连接测试成功，延迟: {latency:F2}ms");
            return new ConnectionTestResult
            {
                Success = true,
                Latency = latency,
                Message = "连接成功"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "数据库连接测试失败");
            return new ConnectionTestResult
            {
                Success = false,
                Error = ex.Message,
                Message = "连接失败: " + ex.Message
            };
        }
    }

    /// <summary>
    /// 获取连接字符串（兼容旧方法）
    /// </summary>
    /// <returns>连接字符串</returns>
    public string GetConnectionString()
    {
        return BuildConnectionStringInternal(_config);
    }

    /// <summary>
    /// 获取数据库类型
    /// </summary>
    public DatabaseType DatabaseType => _config.Type;

    /// <summary>
    /// 格式化连接字符串模板
    /// </summary>
    /// <param name="template">连接字符串模板</param>
    /// <param name="config">数据库配置</param>
    /// <returns>格式化后的连接字符串</returns>
    protected string FormatConnectionString(string template, DatabaseConfig config)
    {
        return template
            .Replace("{Server}", config.Server)
            .Replace("{Port}", config.Port.ToString())
            .Replace("{Database}", config.Database)
            .Replace("{Username}", config.Username)
            .Replace("{Password}", config.Password)
            .Replace("{ConnectionTimeout}", config.ConnectionTimeout.ToString())
            .Replace("{Timeout}", config.ConnectionTimeout.ToString()) // 添加对Timeout参数的支持
            .Replace("{TrustServerCertificate}", config.TrustServerCertificate ? "true" : "false");
    }

    /// <summary>
    /// 获取数据库连接池状态
    /// </summary>
    /// <returns>连接池状态信息</returns>
    public virtual ConnectionPoolStatus GetConnectionPoolStatus()
    {
        // 默认实现，子类可以重写
        return new ConnectionPoolStatus
        {
            IsPoolingEnabled = true,
            PoolSize = -1, // 具体实现由子类提供
            ActiveConnections = -1
        };
    }
}

/// <summary>
/// 连接测试结果
/// </summary>
public class ConnectionTestResult
{
    /// <summary>
    /// 是否连接成功
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 连接延迟（毫秒）
    /// </summary>
    public double Latency { get; set; }
    
    /// <summary>
    /// 错误信息
    /// </summary>
    public string? Error { get; set; }
    
    /// <summary>
    /// 结果消息
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// 连接池信息
    /// </summary>
    public string? PoolingInfo { get; set; }
}

/// <summary>
    /// 连接池状态
    /// </summary>
    public class ConnectionPoolStatus
    {
        /// <summary>
        /// 是否启用连接池
        /// </summary>
        public bool IsPoolingEnabled { get; set; }
        
        /// <summary>
        /// 连接池大小
        /// </summary>
        public int PoolSize { get; set; }
        
        /// <summary>
        /// 最小连接池大小
        /// </summary>
        public int MinPoolSize { get; set; }
        
        /// <summary>
        /// 最大连接池大小
        /// </summary>
        public int MaxPoolSize { get; set; }
        
        /// <summary>
        /// 活跃连接数
        /// </summary>
        public int ActiveConnections { get; set; }
        
        /// <summary>
        /// 数据库类型
        /// </summary>
        public DatabaseType DatabaseType { get; set; }
        
        /// <summary>
        /// 连接字符串是否已配置
        /// </summary>
        public bool ConnectionStringConfigured { get; set; }
        
        /// <summary>
        /// 连接池名称
        /// </summary>
        public string? PoolName { get; set; }
        
        /// <summary>
        /// 错误信息
        /// </summary>
        public string? Error { get; set; }
        
        /// <summary>
        /// 连接延迟（毫秒）
        /// </summary>
        public double ConnectionLatency { get; set; }
        
        /// <summary>
        /// 是否健康
        /// </summary>
        public bool IsHealthy { get; set; }
        
        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }