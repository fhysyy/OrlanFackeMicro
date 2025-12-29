using System.ComponentModel.DataAnnotations;

namespace FakeMicro.Utilities.Configuration;

/// <summary>
/// 简化的应用配置类
/// </summary>
public class SimpleAppSettings
{
    /// <summary>
    /// 数据库连接字符串
    /// </summary>
    public string DatabaseConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// 应用程序密钥
    /// </summary>
    public string AppSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// JWT密钥
    /// </summary>
    public string JwtSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// JWT过期时间（分钟）
    /// </summary>
    public int JwtExpirationMinutes { get; set; } = 60;
    
    /// <summary>
    /// Orleans连接字符串
    /// </summary>
    public string OrleansConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否使用本地集群
    /// </summary>
    public bool UseLocalhostClustering { get; set; } = true;
    
    /// <summary>
    /// Elasticsearch URL
    /// </summary>
    public string ElasticsearchUrl { get; set; } = "http://localhost:9200";
    
    /// <summary>
    /// RabbitMQ连接字符串
    /// </summary>
    public string RabbitMQConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// 初始化数据库连接字符串
    /// 优先使用环境变量，然后使用配置文件值
    /// </summary>
    public void InitializeDatabaseConnection()
    {
        if (string.IsNullOrEmpty(DatabaseConnectionString))
        {
            var dbServer = Environment.GetEnvironmentVariable("DB_SERVER") ?? "localhost";
            var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
            var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "fakemicro";
            var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
            var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? string.Empty;
            
            DatabaseConnectionString = $"Server={dbServer};Port={dbPort};Database={dbName};User Id={dbUser};Password={dbPassword};";
        }
    }
}