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
}
