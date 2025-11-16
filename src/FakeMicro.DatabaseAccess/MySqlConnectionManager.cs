using System.Data.Common;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// MySQL数据库连接管理器
/// </summary>
public class MySqlConnectionManager : BaseDatabaseConnectionManager
{
    private new readonly ILogger<MySqlConnectionManager>? _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="config">数据库配置</param>
    /// <param name="logger">日志记录器</param>
    public MySqlConnectionManager(DatabaseConfig config, ILogger<MySqlConnectionManager>? logger = null) : base(config, logger)
    {
        // 验证数据库类型
        if (config.Type != DatabaseType.MySQL)
        {
            throw new ArgumentException("数据库配置类型必须为MySQL", nameof(config));
        }
        
        // 设置默认端口
        if (config.Port <= 0)
        {
            config.Port = DatabaseConfig.GetDefaultPort(DatabaseType.MySQL);
        }
        
        _logger = logger;
        _logger?.LogInformation("MySQL连接管理器初始化完成");
    }

    /// <summary>
    /// 创建数据库连接
    /// </summary>
    /// <returns>数据库连接</returns>
    public override DbConnection CreateConnection()
    {
        try
        {
            var connection = new MySqlConnection(GetConnectionString());
            // 配置连接池选项
            connection.ConnectionString += ";Pooling=True;Minimum Pool Size=5;Maximum Pool Size=100;ConnectionIdleTimeout=60;";
            return connection;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "创建MySQL连接失败");
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
        // 使用参数化连接字符串构建器
        var builder = new MySqlConnectionStringBuilder
        {
            Server = config.Server,
            Port = (uint)config.Port,
            Database = config.Database,
            UserID = config.Username,
            Password = config.Password,
            ConnectionTimeout = (uint)config.ConnectionTimeout,
            SslMode = config.TrustServerCertificate ? MySqlSslMode.Required : MySqlSslMode.Preferred,
            AllowPublicKeyRetrieval = true,
            AllowUserVariables = true,
            CharacterSet = "utf8mb4",
            ConvertZeroDateTime = true,
            // 连接池配置
            Pooling = true,
            MinimumPoolSize = 5,
            MaximumPoolSize = 100,
            ConnectionIdleTimeout = 60
        };

        return builder.ConnectionString;
    }

    /// <summary>
    /// 获取数据库连接池状态
    /// </summary>
    /// <returns>连接池状态信息</returns>
    public override ConnectionPoolStatus GetConnectionPoolStatus()
    {
        try
        {
            // MySQL连接池状态需要通过性能计数器或自定义监控来实现
            // 这里提供一个基本的实现
            return new ConnectionPoolStatus
            {
                IsPoolingEnabled = true,
                PoolSize = 100, // 最大连接池大小
                ActiveConnections = -1 // 无法直接获取，需要自定义监控
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "获取MySQL连接池状态失败");
            return base.GetConnectionPoolStatus();
        }
    }
}