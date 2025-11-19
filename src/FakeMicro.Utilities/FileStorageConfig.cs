namespace FakeMicro.Utilities;

/// <summary>
/// 文件存储配置
/// </summary>
public class FileStorageConfig
{
    /// <summary>
    /// 本地存储路径
    /// </summary>
    public string LocalStoragePath { get; set; } = "uploads";
    
    /// <summary>
    /// 最大文件大小（字节）
    /// </summary>
    public long MaxFileSize { get; set; } = 100 * 1024 * 1024; // 100MB
    
    /// <summary>
    /// 允许的文件类型
    /// </summary>
    public List<string> AllowedFileTypes { get; set; } = new()
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf", "text/plain", "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel", 
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
    };
    
    /// <summary>
    /// 预览地址基础URL
    /// </summary>
    public string PreviewBaseUrl { get; set; } = "https://localhost:5280/api/files/preview";
    
    /// <summary>
    /// 默认预览有效期（分钟）
    /// </summary>
    public int DefaultPreviewExpiryMinutes { get; set; } = 60;
    
    /// <summary>
    /// 云存储配置
    /// </summary>
    public CloudStorageConfig CloudStorage { get; set; } = new();
}

/// <summary>
/// 云存储配置
/// </summary>
public class CloudStorageConfig
{
    /// <summary>
    /// 是否启用云存储
    /// </summary>
    public bool Enabled { get; set; } = false;
    
    /// <summary>
    /// 云存储提供商
    /// </summary>
    public string Provider { get; set; } = "AWS"; // AWS, Azure, Aliyun
    
    /// <summary>
    /// 存储桶/容器名称
    /// </summary>
    public string? BucketName { get; set; }
    
    /// <summary>
    /// 访问密钥
    /// </summary>
    public string? AccessKey { get; set; }
    
    /// <summary>
    /// 秘密密钥
    /// </summary>
    public string? SecretKey { get; set; }
    
    /// <summary>
    /// 区域
    /// </summary>
    public string? Region { get; set; }
}