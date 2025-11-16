using System;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// 数据库配置类
/// </summary>
public class DatabaseConfig
{
    /// <summary>
    /// 数据库类型
    /// </summary>
    public DatabaseType Type { get; set; } = DatabaseType.PostgreSQL;
    
    /// <summary>
    /// 服务器地址
    /// </summary>
    public string Server { get; set; } = "localhost";
    
    /// <summary>
    /// 端口号
    /// </summary>
    public int Port { get; set; } = 5432;
    
    /// <summary>
    /// 数据库名称
    /// </summary>
    public string Database { get; set; } = "fakemicro";
    
    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = "postgres";
    
    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = "123456";
    
    /// <summary>
    /// 是否允许信任服务器证书（用于SSL连接）
    /// </summary>
    public bool TrustServerCertificate { get; set; } = true;
    
    /// <summary>
    /// 连接超时时间（秒）
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;
    
    /// <summary>
    /// 最小连接池大小
    /// </summary>
    public int MinPoolSize { get; set; } = 5;
    
    /// <summary>
    /// 最大连接池大小
    /// </summary>
    public int MaxPoolSize { get; set; } = 100;
    
    /// <summary>
    /// 根据数据库类型获取默认端口
    /// </summary>
    /// <param name="type">数据库类型</param>
    /// <returns>默认端口号</returns>
    public static int GetDefaultPort(DatabaseType type)
    {
        return type switch
        {
            DatabaseType.MySQL => 3306,
            DatabaseType.PostgreSQL => 5432,
            DatabaseType.MariaDB => 3306,
            _ => throw new ArgumentException("不支持的数据库类型")
        };
    }
    
    /// <summary>
    /// 根据配置生成连接字符串
    /// </summary>
    /// <returns>数据库连接字符串</returns>
    public string GetConnectionString()
    {
        return Type switch
        {
            DatabaseType.MySQL => $"Server={Server};Port={Port};Database={Database};User={Username};Password={Password};Connection Timeout={ConnectionTimeout};Min Pool Size={MinPoolSize};Max Pool Size={MaxPoolSize};" +
                              (TrustServerCertificate ? "SslMode=Required;" : ""),
            
            DatabaseType.PostgreSQL => $"Host={Server};Port={Port};Database={Database};Username={Username};Password={Password};Timeout={ConnectionTimeout};Min Pool Size={MinPoolSize};Max Pool Size={MaxPoolSize};" +
                                    (TrustServerCertificate ? "Trust Server Certificate=true;" : ""),
            
            DatabaseType.MariaDB => $"Server={Server};Port={Port};Database={Database};User={Username};Password={Password};Connection Timeout={ConnectionTimeout};Min Pool Size={MinPoolSize};Max Pool Size={MaxPoolSize};" +
                                 (TrustServerCertificate ? "SslMode=Required;" : ""),
            
            _ => throw new ArgumentException($"不支持的数据库类型: {Type}")
        };
    }
}