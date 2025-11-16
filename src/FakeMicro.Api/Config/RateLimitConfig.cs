using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using FakeMicro.Utilities.Configuration;

namespace FakeMicro.Api.Config
{
    /// <summary>
    /// 流量控制配置类
    /// 用于管理API限流参数
    /// </summary>
    public class RateLimitConfig
    {
        private readonly AdvancedRateLimitConfig _advancedRateLimitConfig;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="advancedRateLimitConfig">高级限流配置</param>
        public RateLimitConfig(AdvancedRateLimitConfig advancedRateLimitConfig)
        {
            _advancedRateLimitConfig = advancedRateLimitConfig;
        }
        
        /// <summary>
    /// 默认限流配置
    /// </summary>
    public RateLimitOptions Default => MapRateLimitOptions(_advancedRateLimitConfig.Default);

    /// <summary>
    /// 认证接口限流配置（更严格）
    /// </summary>
    public RateLimitOptions AuthEndpoints => MapRateLimitOptions(_advancedRateLimitConfig.AuthEndpoints);

    /// <summary>
    /// 普通API接口限流配置
    /// </summary>
    public RateLimitOptions ApiEndpoints => MapRateLimitOptions(_advancedRateLimitConfig.ApiEndpoints);

    /// <summary>
    /// 文件上传接口限流配置
    /// </summary>
    public RateLimitOptions FileEndpoints => MapRateLimitOptions(_advancedRateLimitConfig.FileEndpoints);

    /// <summary>
    /// 管理接口限流配置（较宽松）
    /// </summary>
    public RateLimitOptions AdminEndpoints => MapRateLimitOptions(_advancedRateLimitConfig.AdminEndpoints);
    
    /// <summary>
    /// 将Utilities命名空间的RateLimitOptions映射到Api.Config命名空间的RateLimitOptions
    /// </summary>
    private RateLimitOptions MapRateLimitOptions(FakeMicro.Utilities.Configuration.RateLimitOptions source)
    {
        return new RateLimitOptions
        {
            MaxRequests = source.MaxRequests,
            WindowSeconds = source.WindowSeconds,
            BucketCapacity = source.BucketCapacity,
            TokensPerSecond = source.TokensPerSecond
        };
    }
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
    /// 限流配置扩展
    /// </summary>
    public static class RateLimitExtensions
    {
        /// <summary>
        /// 添加限流服务
        /// </summary>
        public static IServiceCollection AddRateLimitServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 注册RateLimitConfig，使其可以注入到其他服务中
            // 由于我们直接注册了AdvancedRateLimitConfig的单例，这里直接获取
            services.AddSingleton(provider =>
            {
                var advancedRateLimitConfig = provider.GetRequiredService<AdvancedRateLimitConfig>();
                return new RateLimitConfig(advancedRateLimitConfig);
            });
            
            return services;
        }
    }
}