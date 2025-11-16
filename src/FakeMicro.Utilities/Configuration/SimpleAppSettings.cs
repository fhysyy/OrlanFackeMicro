namespace FakeMicro.Utilities.Configuration;

/// <summary>
/// 简化的应用程序配置
/// </summary>
public class SimpleAppSettings
{
    /// <summary>
    /// 数据库连接字符串
    /// </summary>
    public string DatabaseConnectionString { get; set; } = "Server=localhost;Port=5432;Database=fakemicro;User Id=postgres;Password=123456;";
    
    /// <summary>
    /// JWT密钥
    /// </summary>
    public string JwtSecret { get; set; } = "your-super-secret-key-change-in-production";
    
    /// <summary>
    /// JWT过期时间（分钟）
    /// </summary>
    public int JwtExpiryMinutes { get; set; } = 60;
    
    /// <summary>
    /// Orleans集群连接字符串
    /// </summary>
    public string OrleansConnectionString { get; set; } = "Server=localhost;Port=5432;Database=orleans;User Id=postgres;Password=123456;Include Error Detail=true;";
    
    /// <summary>
    /// 是否使用本地主机集群（开发环境）
    /// </summary>
    public bool UseLocalhostClustering { get; set; } = true;
    
    /// <summary>
    /// 文件存储基础路径
    /// </summary>
    public string FileStoragePath { get; set; } = "uploads";
    
    /// <summary>
    /// Elasticsearch URL
    /// </summary>
    public string ElasticsearchUrl { get; set; } = "http://localhost:9200";
    
    /// <summary>
    /// RabbitMQ连接字符串
    /// </summary>
    public string RabbitMQConnectionString { get; set; } = "amqp://guest:guest@localhost:5672/";
}