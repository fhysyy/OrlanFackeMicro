using System.Data.Common;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Interfaces;

/// <summary>
/// 数据库连接管理器接口
/// 提供数据库连接的管理功能
/// </summary>
public interface IDatabaseConnectionManager
{
    /// <summary>
    /// 创建数据库连接
    /// </summary>
    /// <returns>数据库连接</returns>
    DbConnection CreateConnection();

    /// <summary>
    /// 获取数据库连接（异步）
    /// </summary>
    /// <returns>数据库连接</returns>
    Task<DbConnection> GetConnectionAsync();

    /// <summary>
    /// 构建连接字符串
    /// </summary>
    /// <param name="config">数据库配置</param>
    /// <returns>连接字符串</returns>
    string BuildConnectionString(DatabaseConfig config);

    /// <summary>
    /// 测试数据库连接
    /// </summary>
    /// <returns>连接测试结果</returns>
    Task<ConnectionTestResult> TestConnectionAsync();

    /// <summary>
    /// 获取数据库连接字符串
    /// </summary>
    /// <returns>连接字符串</returns>
    string GetConnectionString();

    /// <summary>
    /// 数据库类型
    /// </summary>
    DatabaseType DatabaseType { get; }

    /// <summary>
    /// 获取数据库连接池状态
    /// </summary>
    /// <returns>连接池状态信息</returns>
    ConnectionPoolStatus GetConnectionPoolStatus();
}

