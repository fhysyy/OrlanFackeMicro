namespace FakeMicro.Api.Config;

/// <summary>
/// Elasticsearch配置类
/// </summary>
public class ElasticsearchConfig
{
    /// <summary>
    /// Elasticsearch服务器地址
    /// </summary>
    public string Url { get; set; } = "http://localhost:9200";
    
    /// <summary>
    /// 索引格式
    /// </summary>
    public string IndexFormat { get; set; } = "fakemicro-api-logs-{0:yyyy.MM}";
    
    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// 分片数量
    /// </summary>
    public int NumberOfShards { get; set; } = 1;
    
    /// <summary>
    /// 副本数量
    /// </summary>
    public int NumberOfReplicas { get; set; } = 0;
}