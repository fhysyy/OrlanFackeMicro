
using System.ComponentModel.DataAnnotations;

namespace FakeMicro.Utilities.Configuration;

/// <summary>
/// 应用程序配置类
/// </summary>
/// appsettings.json中的 配置名称要一致 Database   Orleans Jwt RabbitMQ  Elasticsearch FileStorage Cors AnomalyDetection Hangfire    
public class AppSettings
{
    public DatabaseConfig Database { get; set; } = new();
    public OrleansConfig Orleans { get; set; } = new();
    public JwtSettings Jwt { get; set; } = new();
    public RabbitMQConfig RabbitMQ { get; set; } = new();
    public RateLimitConfig RateLimit { get; set; } = new();
    public AdvancedRateLimitConfig AdvancedRateLimit { get; set; } = new();
    public ElasticsearchConfig Elasticsearch { get; set; } = new();
    public FileStorageConfig FileStorage { get; set; } = new();
    public CorsConfig Cors { get; set; } = new();
    public AnomalyDetectionConfig AnomalyDetection { get; set; } = new();
    public HangfireConfig Hangfire { get; set; } = new();
    public CapConfig CAP { get; set; } = new(); // 添加CAP配置
}

/// <summary>
/// CAP事件总线配置
/// </summary>
public class CapConfig
{
    /// <summary>
    /// 是否启用CAP
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// 失败重试次数
    /// </summary>
    public int FailedRetryCount { get; set; } = 3;
    
    /// <summary>
    /// 失败重试间隔（秒）
    /// </summary>
    public int FailedRetryInterval { get; set; } = 30;
    
    /// <summary>
    /// 成功消息过期时间（秒）
    /// </summary>
    public int SucceedMessageExpiredAfter { get; set; } = 3600;
    
    /// <summary>
    /// 消费者线程数
    /// </summary>
    public int ConsumerThreadCount { get; set; } = 1;
    
    /// <summary>
    /// 是否启用Dashboard
    /// </summary>
    public bool UseDashboard { get; set; } = true;
    
    /// <summary>
    /// Dashboard路径
    /// </summary>
    public string DashboardPath { get; set; } = "/cap";
    
    /// <summary>
    /// Dashboard是否允许匿名访问
    /// </summary>
    public bool DashboardAllowAnonymous { get; set; } = false;
}

/// <summary>
/// Hangfire配置
/// </summary>
public class HangfireConfig
{
    public string ConnectionString { get; set; } = "Host=localhost;Database=fakemicro_hangfire;Username=postgres;Password=123456";
    public int WorkerCount { get; set; } = 5;
    public bool UseDashboard { get; set; } = true;
    public string DashboardPath { get; set; } = "/hangfire";
}

/// <summary>
/// 数据库配置
/// </summary>
public class DatabaseConfig
    {
        public string Type { get; set; } = "PostgreSQL";
        public string Server { get; set; } = "localhost";
        public int Port { get; set; } = 5432;
        public string Database { get; set; } = "fakemicro";
        public string Username { get; set; } = "postgres";
        public string Password { get; set; } = string.Empty; // 不再使用硬编码默认密码
        public bool TrustServerCertificate { get; set; } = false;
        public int ConnectionTimeout { get; set; } = 30;
        public int CommandTimeout { get; set; } = 60;
        public int MinPoolSize { get; set; } = 10;
        public int MaxPoolSize { get; set; } = 200;
        public int ConnectionLifetime { get; set; } = 3600;
        public bool IncludeErrorDetail { get; set; } = true;
        
        // 读写分离配置
        public bool EnableReadWriteSeparation { get; set; } = false;
        public string ReadServer { get; set; } = "localhost";
        public int ReadPort { get; set; } = 5432;
        public string ReadDatabase { get; set; } = "fakemicro";
        public string ReadUsername { get; set; } = "postgres";
        public string ReadPassword { get; set; } = string.Empty;
        public int ReadMinPoolSize { get; set; } = 10;
        public int ReadMaxPoolSize { get; set; } = 200;

        public string GetConnectionString()
        {
            // 检查是否直接设置了完整的连接字符串（优先级最高）
            var fullConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
            if (!string.IsNullOrEmpty(fullConnectionString))
            {
                return fullConnectionString;
            }
            
            // 使用环境变量优先级高于配置文件
            var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? Password;
            var server = Environment.GetEnvironmentVariable("DB_HOST") ?? Environment.GetEnvironmentVariable("DB_SERVER") ?? Server; // 兼容两种命名
            var port = Environment.GetEnvironmentVariable("DB_PORT") != null ? int.Parse(Environment.GetEnvironmentVariable("DB_PORT")!) : Port;
            var database = Environment.GetEnvironmentVariable("DB_NAME") ?? Database;
            var username = Environment.GetEnvironmentVariable("DB_USER") ?? Environment.GetEnvironmentVariable("DB_USERNAME") ?? Username; // 兼容两种命名
            
            // 添加PostgreSQL特定选项，解决大小写敏感和表名引用问题
            return $"Host={server};Port={port};Database={database};Username={username};Password={password};Trust Server Certificate={TrustServerCertificate};Timeout={ConnectionTimeout};CommandTimeout={CommandTimeout};MinPoolSize={MinPoolSize};MaxPoolSize={MaxPoolSize};Connection Lifetime={ConnectionLifetime};Include Error Detail={IncludeErrorDetail};SearchPath=public;ApplicationName=FakeMicroApp;";
        }
        
        public string GetReadConnectionString()
        {
            // 检查是否直接设置了完整的读库连接字符串（优先级最高）
            var fullConnectionString = Environment.GetEnvironmentVariable("DB_READ_CONNECTION_STRING");
            if (!string.IsNullOrEmpty(fullConnectionString))
            {
                return fullConnectionString;
            }
            
            // 使用环境变量优先级高于配置文件
            var password = Environment.GetEnvironmentVariable("DB_READ_PASSWORD") ?? ReadPassword ?? Password;
            var server = Environment.GetEnvironmentVariable("DB_READ_HOST") ?? Environment.GetEnvironmentVariable("DB_READ_SERVER") ?? ReadServer ?? Server; // 兼容两种命名
            var port = Environment.GetEnvironmentVariable("DB_READ_PORT") != null ? int.Parse(Environment.GetEnvironmentVariable("DB_READ_PORT")!) : ReadPort;
            var database = Environment.GetEnvironmentVariable("DB_READ_NAME") ?? ReadDatabase ?? Database;
            var username = Environment.GetEnvironmentVariable("DB_READ_USER") ?? Environment.GetEnvironmentVariable("DB_READ_USERNAME") ?? ReadUsername ?? Username; // 兼容两种命名
            
            // 添加PostgreSQL特定选项，解决大小写敏感和表名引用问题
            return $"Host={server};Port={port};Database={database};Username={username};Password={password};Trust Server Certificate={TrustServerCertificate};Timeout={ConnectionTimeout};CommandTimeout={CommandTimeout};MinPoolSize={ReadMinPoolSize};MaxPoolSize={ReadMaxPoolSize};Connection Lifetime={ConnectionLifetime};Include Error Detail={IncludeErrorDetail};SearchPath=public;ApplicationName=FakeMicroApp_Read;";
        }
    }

/// <summary>
/// Orleans配置
/// </summary>
public class OrleansConfig
{
    [Required]
    public string ClusterId { get; set; } = "FakeMicroCluster";
    
    [Required]
    public string ServiceId { get; set; } = "FakeMicroService";
    
    public int SiloPort { get; set; } = 11111;
    public int GatewayPort { get; set; } = 30000;
    public bool UseLocalhostClustering { get; set; } = true;
    public int CollectionAgeMinutes { get; set; } = 5;
    public int CollectionQuantumSeconds { get; set; } = 1;
    public int DashboardPort { get; set; } = 8080;
    public string DashboardHost { get; set; } = "localhost";
    public int DashboardUpdateIntervalMs { get; set; } = 2000;
}







/// <summary>
/// JWT鉴权管理
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// 密钥
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// 签发者
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// 接收者
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// 过期时间（分钟）
    /// </summary>
    public int ExpireMinutes { get; set; } = 60;

    /// <summary>
    /// 刷新令牌过期时间（天）
    /// </summary>
    public int RefreshExpireDays { get; set; } = 7;
}

/// <summary>
/// 异常行为检测配置
/// </summary>
public class AnomalyDetectionConfig
{
    /// <summary>
    /// 是否启用异常行为检测
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// 短时间内失败尝试次数阈值
    /// </summary>
    public int FailedAttemptsThreshold { get; set; } = 5;
    
    /// <summary>
    /// 短时间窗口（秒）
    /// </summary>
    public int ShortTimeWindowSeconds { get; set; } = 60;
    
    /// <summary>
    /// 长时间内失败尝试次数阈值
    /// </summary>
    public int LongFailedAttemptsThreshold { get; set; } = 20;
    
    /// <summary>
    /// 长时间窗口（秒）
    /// </summary>
    public int LongTimeWindowSeconds { get; set; } = 3600;
    
    /// <summary>
    /// 阻止时间（秒）
    /// </summary>
    public int BlockDurationSeconds { get; set; } = 300;
    
    /// <summary>
    /// 最大请求速率（每分钟）
    /// </summary>
    public int MaxRequestsPerMinute { get; set; } = 100;
    
    /// <summary>
    /// 是否阻止可疑IP
    /// </summary>
    public bool BlockSuspiciousIps { get; set; } = true;
    
    /// <summary>
    /// 是否记录异常行为
    /// </summary>
    public bool LogAnomalies { get; set; } = true;
}

/// <summary>
/// RabbitMQ配置
/// </summary>
public class RabbitMQConfig
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "";
    public string VirtualHost { get; set; } = "/";
    
    // 获取RabbitMQ密码（优先使用环境变量）
    public string GetPassword()
    {
        return Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? Password;
    }
    
    // 获取RabbitMQ主机（优先使用环境变量）
    public string GetHostName()
    {
        return Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? HostName;
    }
}



/// <summary>
/// 限流配置
/// </summary>
public class RateLimitConfig
{
    public int WindowSeconds { get; set; } = 60;
    public int MaxRequests { get; set; } = 10000;
    public int BurstLimit { get; set; } = 200;
}

/// <summary>
/// 限流选项
/// </summary>
public class RateLimitOptions
{
    /// <summary>
    /// 最大请求数
    /// </summary>
    public int MaxRequests { get; set; }

    /// <summary>
    /// 时间窗口（秒）
    /// </summary>
    public int WindowSeconds { get; set; }

    /// <summary>
    /// 令牌桶容量
    /// </summary>
    public int BucketCapacity { get; set; }

    /// <summary>
    /// 每秒补充令牌数
    /// </summary>
    public double TokensPerSecond { get; set; }
}

/// <summary>
    /// 高级限流配置
    /// </summary>
    public class AdvancedRateLimitConfig
    {
        /// <summary>
        /// 默认限流配置
        /// </summary>
        public RateLimitOptions Default { get; set; } = new RateLimitOptions
        {
            MaxRequests = 60,
            WindowSeconds = 60,
            BucketCapacity = 60,
            TokensPerSecond = 1.0
        };

        /// <summary>
        /// 认证接口限流配置
        /// </summary>
        public RateLimitOptions AuthEndpoints { get; set; } = new RateLimitOptions
        {
            MaxRequests = 10,
            WindowSeconds = 60,
            BucketCapacity = 10,
            TokensPerSecond = 0.2
        };

        /// <summary>
        /// API接口限流配置
        /// </summary>
        public RateLimitOptions ApiEndpoints { get; set; } = new RateLimitOptions
        {
            MaxRequests = 100,
            WindowSeconds = 60,
            BucketCapacity = 100,
            TokensPerSecond = 2.0
        };

        /// <summary>
        /// 文件接口限流配置
        /// </summary>
        public RateLimitOptions FileEndpoints { get; set; } = new RateLimitOptions
        {
            MaxRequests = 20,
            WindowSeconds = 60,
            BucketCapacity = 20,
            TokensPerSecond = 0.5
        };

        /// <summary>
        /// 管理接口限流配置
        /// </summary>
        public RateLimitOptions AdminEndpoints { get; set; } = new RateLimitOptions
        {
            MaxRequests = 200,
            WindowSeconds = 60,
            BucketCapacity = 200,
            TokensPerSecond = 4.0
        };
    }

/// <summary>
/// Elasticsearch配置
/// </summary>
public class ElasticsearchConfig
{
    public string Url { get; set; } = "http://localhost:9200";
    public string IndexFormat { get; set; } = "fakemicro-logs-{0:yyyy.MM}";
    public int NumberOfShards { get; set; } = 1;
    public int NumberOfReplicas { get; set; } = 0;
    public string? Username { get; set; }
    public string? Password { get; set; }
    
    // 获取Elasticsearch密码（优先使用环境变量）
    public string? GetPassword()
    {
        return Environment.GetEnvironmentVariable("ELASTIC_PASSWORD") ?? Password;
    }
}

/// <summary>
/// 文件存储配置
/// </summary>
public class FileStorageConfig
{
    public string LocalStoragePath { get; set; } = "uploads";
    public long MaxFileSize { get; set; } = 100 * 1024 * 1024; // 100MB
    public List<string> AllowedFileTypes { get; set; } = new()
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf", "text/plain", "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel", 
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    };
    public string PreviewBaseUrl { get; set; } = "https://localhost:5280/api/files/preview";
    public int DefaultPreviewExpiryMinutes { get; set; } = 60;
}

/// <summary>
/// CORS配置
/// </summary>
public class CorsConfig
{
    public List<string> AllowedOrigins { get; set; } = new() { 
        "http://localhost:3000", 
        "http://127.0.0.1:3000", 
        "http://localhost:3001", 
        "http://127.0.0.1:3001" 
    };
    public bool AllowCredentials { get; set; } = true;
}