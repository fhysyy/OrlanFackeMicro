using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using MySqlConnector;
using Microsoft.Data.SqlClient;
using FakeMicro.Utilities.Configuration;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// 数据库连接管理器
/// 管理数据库连接的创建和生命周期
/// </summary>
public class DatabaseConnectionManager : BaseDatabaseConnectionManager, IDisposable
{
    private new readonly ILogger<DatabaseConnectionManager> _logger;
    private IDbConnection? _connection; // 保留此字段以保持向后兼容性，但不再用于单例模式
    private bool _disposed = false;

    public DatabaseConnectionManager(AppSettings appSettings, ILogger<DatabaseConnectionManager> logger)
        : base(new DatabaseConfig
        {
            Type = Enum.TryParse<DatabaseType>(appSettings.Database.Type, true, out var dbType) ? dbType : DatabaseType.PostgreSQL,
            Server = appSettings.Database.Server ?? "localhost",
            Port = appSettings.Database.Port > 0 ? appSettings.Database.Port : 5432,
            Database = appSettings.Database.Database ?? "fakemicro",
            Username = appSettings.Database.Username ?? "postgres",
            Password = appSettings.Database.Password ?? "123456",
            TrustServerCertificate = appSettings.Database.TrustServerCertificate,
            ConnectionTimeout = appSettings.Database.ConnectionTimeout > 0 ? appSettings.Database.ConnectionTimeout : 30,
            // 连接池配置
            MinPoolSize = appSettings.Database.MinPoolSize > 0 ? appSettings.Database.MinPoolSize : 5,
            MaxPoolSize = appSettings.Database.MaxPoolSize > 0 ? appSettings.Database.MaxPoolSize : 100
        }, logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _logger.LogInformation("数据库连接管理器初始化完成，数据库类型: {DatabaseType}", _config.Type);
    }
    
    /// <summary>
    /// 使用指定的数据库配置构造函数
    /// </summary>
    /// <param name="config">数据库配置</param>
    /// <param name="logger">日志记录器</param>
    public DatabaseConnectionManager(DatabaseConfig config, ILogger<DatabaseConnectionManager> logger)
        : base(config, logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 获取数据库连接
    /// </summary>
    /// <returns>数据库连接对象</returns>
    public IDbConnection GetConnection()
    {
        // 对于同步方法，每次获取新连接而不是重用单个连接，避免并发问题
        var connection = (IDbConnection)CreateConnection();
        connection.Open();
        _logger.LogDebug("创建并打开数据库连接");
        return connection;
    }
    
    /// <summary>
    /// 获取可重用的数据库连接
    /// 注意：现在使用数据库内置连接池，不再维护单例连接
    /// </summary>
    /// <returns>数据库连接对象</returns>
    public IDbConnection GetReusableConnection()
    {
        // 不再使用单例模式，而是从连接池获取连接
        // 这样可以避免线程安全问题，同时利用数据库驱动的连接池管理
        var connection = (IDbConnection)CreateConnection();
        connection.Open();
        _logger.LogDebug("从连接池获取数据库连接");
        return connection;
    }

    /// <summary>
    /// 获取连接字符串
    /// </summary>
    /// <returns>连接字符串</returns>
    public new string GetConnectionString()
    {
        // 直接使用配置对象构建连接字符串
        return BuildConnectionString(base._config);
    }

    /// <summary>
    /// 创建数据库连接
    /// </summary>
    /// <returns>数据库连接</returns>
    public override DbConnection CreateConnection()
    {
        var connectionString = GetConnectionString();
        return (DbConnection)CreateConnectionInternal(connectionString);
    }

    /// <summary>
    /// 获取数据库连接（异步）
    /// </summary>
    /// <returns>数据库连接</returns>
    public new async Task<DbConnection> GetConnectionAsync()
    {
        try
        {
            // 避免使用Task.Run包装同步方法
            var connectionString = GetConnectionString();
            var connection = (DbConnection)CreateConnectionInternal(connectionString);
            
            // 确保使用异步打开连接
            if (connection.State != ConnectionState.Open)
            {
                _logger.LogDebug("正在异步打开数据库连接");
                await connection.OpenAsync().ConfigureAwait(false);
                _logger.LogDebug("数据库连接已异步打开");
            }
            
            return connection;
        }
        catch (Exception ex)
        {
            // 屏蔽密码等敏感信息后记录日志
            var sanitizedMessage = ex.Message.Replace(base._config.Password, "******");
            _logger.LogError(ex, $"异步获取数据库连接失败: {sanitizedMessage}");
            throw new InvalidOperationException("无法建立数据库连接", ex);
        }
    }

    /// <summary>
    /// 构建连接字符串
    /// </summary>
    /// <param name="config">数据库配置</param>
    /// <returns>连接字符串</returns>
    public new string BuildConnectionString(DatabaseConfig config)
    {
        // 验证配置
        if (config == null)
            throw new ArgumentNullException(nameof(config));
            
        // 验证必要的配置值
        ValidateDatabaseConfig(config);
            
        // 使用配置中的数据库类型优先
        switch (config.Type)
        {
            case DatabaseType.PostgreSQL:
                var pgBuilder = new NpgsqlConnectionStringBuilder
                {
                    Host = config.Server,
                    Port = config.Port,
                    Database = config.Database,
                    Username = config.Username,
                    Password = config.Password,
                    Timeout = config.ConnectionTimeout,
                    TrustServerCertificate = config.TrustServerCertificate,
                    // 连接池优化配置
                    Pooling = true,
                    MinPoolSize = config.MinPoolSize,
                    MaxPoolSize = config.MaxPoolSize,
                    ConnectionIdleLifetime = 600 // 10分钟，单位为秒
                };
                return pgBuilder.ConnectionString;
            
            case DatabaseType.MySQL:
            case DatabaseType.MariaDB:
                // 使用MySqlConnectionStringBuilder正确处理类型转换
                var mysqlBuilder = new MySqlConnectionStringBuilder
                {
                    Server = config.Server,
                    Database = config.Database,
                    UserID = config.Username,
                    Password = config.Password,
                    ConnectionTimeout = (uint)config.ConnectionTimeout,
                    SslMode = config.TrustServerCertificate ? MySqlSslMode.Required : MySqlSslMode.None,
                    // 连接池优化配置
                    Pooling = true,
                    MinimumPoolSize = (uint)config.MinPoolSize,
                    MaximumPoolSize = (uint)config.MaxPoolSize,
                    ConnectionIdleTimeout = 600000 // 10分钟，单位为毫秒
                };
                // 单独设置端口，避免int到uint的类型转换问题
                mysqlBuilder.Port = (uint)config.Port;
                return mysqlBuilder.ConnectionString;
            
            case DatabaseType.SQLServer:
                var sqlBuilder = new SqlConnectionStringBuilder
                {
                    DataSource = $"{config.Server},{config.Port}",
                    InitialCatalog = config.Database,
                    UserID = config.Username,
                    Password = config.Password,
                    ConnectTimeout = config.ConnectionTimeout,
                    TrustServerCertificate = config.TrustServerCertificate,
                    // 连接池优化配置
                    Pooling = true,
                    MinPoolSize = config.MinPoolSize,
                    MaxPoolSize = config.MaxPoolSize,
                    LoadBalanceTimeout = 30
                };
                return sqlBuilder.ConnectionString;
            
            case DatabaseType.SQLite:
                // SQLite连接字符串简单得多
                return $"Data Source={config.Database};Version=3;";
                
            default:
                throw new NotSupportedException($"不支持的数据库类型: {config.Type}");
        }
    }
    
    /// <summary>
    /// 验证数据库配置
    /// </summary>
    /// <param name="config">数据库配置</param>
    private void ValidateDatabaseConfig(DatabaseConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Server))
            throw new ArgumentException("数据库服务器地址不能为空", nameof(config));
            
        if (string.IsNullOrWhiteSpace(config.Database))
            throw new ArgumentException("数据库名称不能为空", nameof(config));
            
        if (config.Type != DatabaseType.SQLite && string.IsNullOrWhiteSpace(config.Username))
            throw new ArgumentException("数据库用户名不能为空", nameof(config));
            
        // 密码验证取决于数据库类型和配置，这里只做基本检查
        if (config.Type != DatabaseType.SQLite && config.Password == null)
            throw new ArgumentException("数据库密码不能为空", nameof(config));
            
        if (config.Port <= 0 || config.Port > 65535)
            throw new ArgumentException("数据库端口号无效", nameof(config));
            
        if (config.ConnectionTimeout <= 0 || config.ConnectionTimeout > 300)
            throw new ArgumentException("连接超时时间必须在1-300秒之间", nameof(config));
    }

    /// <summary>
    /// 测试数据库连接
    /// </summary>
    /// <returns>连接测试结果</returns>
    public new async Task<ConnectionTestResult> TestConnectionAsync()
    {
        try
        {
            var startTime = DateTime.UtcNow;
            DbConnection? connection = null;
            
            try
            {
                // 使用基类的测试方法或自定义实现
                connection = await GetConnectionAsync();
                
                // 执行简单的查询来验证连接可用性
                using var command = connection.CreateCommand();
                command.CommandText = GetHealthCheckQuery();
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                
                var endTime = DateTime.UtcNow;
                return new ConnectionTestResult
                {
                    Success = true,
                    Message = "数据库连接测试成功",
                    Latency = (endTime - startTime).TotalMilliseconds,
                    PoolingInfo = $"连接池已启用: {GetConnectionPoolStatus().IsPoolingEnabled}"
                };
            }
            finally
            {
                // 确保连接被关闭并返回连接池
                if (connection != null)
                {
                    await connection.CloseAsync().ConfigureAwait(false);
                    connection.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            // 记录详细错误但返回友好信息
            var sanitizedMessage = ex.Message.Replace(base._config.Password, "******");
            _logger.LogError(ex, $"数据库连接测试失败: {sanitizedMessage}");
            
            return new ConnectionTestResult
            {
                Success = false,
                Error = sanitizedMessage,
                Message = $"连接失败: {GetFriendlyErrorMessage(ex)}"
            };
        }
    }
    
    /// <summary>
    /// 根据数据库类型获取健康检查查询
    /// </summary>
    /// <returns>健康检查SQL查询</returns>
    private string GetHealthCheckQuery()
    {
        return base._config.Type switch
        {
            DatabaseType.PostgreSQL => "SELECT 1",
            DatabaseType.MySQL or DatabaseType.MariaDB => "SELECT 1",
            DatabaseType.SQLServer => "SELECT 1",
            DatabaseType.SQLite => "SELECT 1",
            _ => throw new NotSupportedException($"不支持的数据库类型: {base._config.Type}")
        };
    }
    
    /// <summary>
    /// 获取友好的错误消息
    /// </summary>
    /// <param name="ex">异常</param>
    /// <returns>友好的错误消息</returns>
    private string GetFriendlyErrorMessage(Exception ex)
    {
        // 根据不同异常类型返回更具体的错误信息
        if (ex is DbException dbEx)
        {
            // 可以根据不同数据库的错误代码提供更具体的错误信息
            return "数据库连接错误，请检查连接参数和网络状态";
        }
        else if (ex is TimeoutException)
        {
            return "连接超时，请检查网络状态和服务器响应";
        }
        else if (ex is InvalidOperationException)
        {
            return "配置错误，请检查数据库配置";
        }
        
        return ex.Message;
    }

    /// <summary>
    /// 数据库类型
    /// </summary>
    public new DatabaseType DatabaseType => base.DatabaseType;

    /// <summary>
    /// 获取数据库连接池状态
    /// </summary>
    public new ConnectionPoolStatus GetConnectionPoolStatus()
    {
        try
        {
            // 根据不同数据库类型获取更准确的连接池状态
            var status = new ConnectionPoolStatus
            {
                IsPoolingEnabled = true,
                // 使用配置的最小和最大池大小
                MinPoolSize = base._config.MinPoolSize,
                MaxPoolSize = base._config.MaxPoolSize,
                DatabaseType = base.DatabaseType,
                ConnectionStringConfigured = !string.IsNullOrEmpty(GetConnectionString())
            };
            
            // 尝试获取更多连接池信息
            try
            {
                switch (base.DatabaseType)
                {
                    case DatabaseType.MySQL:
                    case DatabaseType.MariaDB:
                        // 对于MySQL/MariaDB，可以获取更多连接池信息
                        status.PoolName = "MySQL Connection Pool";
                        // 可以通过MySqlConnector的API获取更多连接池统计信息
                        break;
                    case DatabaseType.PostgreSQL:
                        status.PoolName = "Npgsql Connection Pool";
                        // Npgsql提供了连接池统计信息
                        break;
                    case DatabaseType.SQLServer:
                        status.PoolName = "SQL Server Connection Pool";
                        break;
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不影响主功能
                _logger.LogWarning(ex, "获取连接池详细信息时出错");
            }
            
            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取连接池状态失败");
            // 返回基本状态
            return new ConnectionPoolStatus
            {
                IsPoolingEnabled = false,
                PoolSize = 0,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// 根据数据库类型创建数据库连接（内部方法）
    /// </summary>
    /// <param name="connectionString">连接字符串</param>
    /// <returns>数据库连接对象</returns>
    private IDbConnection CreateConnectionInternal(string connectionString)
    {
        // 验证连接字符串是否为空
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString), "连接字符串不能为空");
        }

        try
        {
            // 根据配置的数据库类型创建对应连接
            switch (base.DatabaseType)
            {
                case DatabaseType.PostgreSQL:
                    return new NpgsqlConnection(connectionString);
                case DatabaseType.MySQL:
                case DatabaseType.MariaDB:
                    // 使用MySqlConnector而不是MySqlConnection
                    return new MySqlConnection(connectionString);
                case DatabaseType.SQLServer:
                    return new Microsoft.Data.SqlClient.SqlConnection(connectionString);
                case DatabaseType.SQLite:
                    // 注意：使用SQLite需要添加System.Data.SQLite或Microsoft.Data.Sqlite包
                    throw new NotImplementedException("SQLite连接尚未实现，请添加相应的包并实现连接创建");
                default:
                    throw new NotSupportedException($"不支持的数据库类型: {base.DatabaseType}");
            }
        }
        catch (NotSupportedException)
        {
            throw;
        }
        catch (NotImplementedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // 屏蔽密码等敏感信息
            var sanitizedConnectionString = connectionString;
            if (base._config != null && !string.IsNullOrEmpty(base._config.Password))
            {
                sanitizedConnectionString = connectionString.Replace(base._config.Password, "******");
            }
            
            _logger.LogError(ex, $"创建数据库连接失败，连接字符串: {sanitizedConnectionString}");
            throw new InvalidOperationException($"创建{base.DatabaseType}数据库连接失败", ex);
        }
    }

    /// <summary>
    /// 开始事务
    /// </summary>
    /// <returns>事务对象</returns>
    public IDbTransaction BeginTransaction()
    {
        // 为事务创建新连接，确保事务隔离
        var connection = (IDbConnection)CreateConnection();
        connection.Open();
        _logger.LogDebug("创建新连接并开始事务");
        return connection.BeginTransaction();
    }

    /// <summary>
    /// 开始事务（指定隔离级别）
    /// </summary>
    /// <param name="isolationLevel">隔离级别</param>
    /// <returns>事务对象</returns>
    public IDbTransaction BeginTransaction(IsolationLevel isolationLevel)
    {
        // 为事务创建新连接，确保事务隔离
        var connection = (IDbConnection)CreateConnection();
        connection.Open();
        _logger.LogDebug("创建新连接并开始事务，隔离级别: {IsolationLevel}", isolationLevel);
        return connection.BeginTransaction(isolationLevel);
    }
    
    /// <summary>
    /// 开始事务（异步）
    /// </summary>
    /// <returns>事务对象</returns>
    public async Task<DbTransaction> BeginTransactionAsync()
    {
        var connection = await GetConnectionAsync();
        return await connection.BeginTransactionAsync().ConfigureAwait(false);
    }
    
    /// <summary>
    /// 开始事务（异步，指定隔离级别）
    /// </summary>
    /// <param name="isolationLevel">隔离级别</param>
    /// <returns>事务对象</returns>
    public async Task<DbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel)
    {
        var connection = await GetConnectionAsync();
        return await connection.BeginTransactionAsync(isolationLevel).ConfigureAwait(false);
    }

    /// <summary>
    /// 构建连接字符串（内部方法）
    /// </summary>
    /// <param name="config">数据库配置</param>
    /// <returns>连接字符串</returns>
    protected override string BuildConnectionStringInternal(DatabaseConfig config)
    {
        return BuildConnectionString(config);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            if (_connection != null)
            {
                try
                {
                    if (_connection.State == ConnectionState.Open)
                    {
                        _connection.Close();
                    }
                    _connection.Dispose();
                    _connection = null;
                    _logger.LogDebug("数据库连接已关闭并释放");
                }
                catch (Exception ex)
                {
                    // 记录释放资源时的错误，但不抛出异常
                    _logger.LogError(ex, "释放数据库连接资源时出错");
                }
            }
            // 对于使用连接池的情况，这里可以添加额外的清理逻辑
            _logger.LogDebug("数据库连接管理器已释放");
        }
        _disposed = true;
    }

    ~DatabaseConnectionManager()
    {
        Dispose(false);
    }
}