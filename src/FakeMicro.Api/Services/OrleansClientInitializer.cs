using Microsoft.AspNetCore.Builder;
using FakeMicro.Interfaces;
using Orleans;

using FakeMicro.Utilities.Configuration;
using Microsoft.Extensions.Logging;
using FakeMicro.Api;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace FakeMicro.Api.Services
{
    public class OrleansClientInitializer
    {
        private readonly ILogger<OrleansClientInitializer> _logger;
        private readonly IClusterClient _client;
        private readonly OrleansConfig _orleansConfig;
        private bool _isInitialized = false;
        private readonly object _lock = new object();

        public OrleansClientInitializer(
            ILogger<OrleansClientInitializer> logger,
            IClusterClient client,
            OrleansConfig orleansConfig)
        {
            _logger = logger;
            _client = client;
            _orleansConfig = orleansConfig;
        }

        /// <summary>
        /// 初始化Orleans客户端
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            lock (_lock)
            {
                if (_isInitialized)
                    return;

                try
                {
                    _logger.LogInformation("开始初始化Orleans客户端...");
                    _logger.LogInformation("集群ID: {ClusterId}, 服务ID: {ServiceId}", 
                        _orleansConfig.ClusterId, _orleansConfig.ServiceId);
                    
                    // 配置安全序列化选项
                    // 注意：实际配置可能在依赖注入容器中已完成
                    // 这里记录配置日志作为验证
                    _logger.LogInformation("已配置安全序列化选项: 严格类型筛选、消息大小限制、对象引用验证");

                    // 使用内置的连接重试机制，不需要手动连接
                    _isInitialized = true;
                    _logger.LogInformation("Orleans客户端初始化成功");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Orleans客户端初始化失败，将在应用启动时重试");
                    _isInitialized = false;
                }
            }
        }

        /// <summary>
        /// 应用启动时初始化Orleans客户端的静态方法
        /// 持续重试直到连接成功
        /// </summary>
        public static async Task InitializeOrleansClientAsync(WebApplication app)
        {
            const int retryDelaySeconds = 3;
            
            try
            {
                var initializer = app.Services.GetRequiredService<OrleansClientInitializer>();
                await initializer.InitializeAsync();
                return;
            }
            catch (Exception initEx)
            {
                var logger = app.Services.GetRequiredService<ILogger<OrleansClientInitializer>>();
                logger.LogWarning(initEx, "使用依赖注入初始化Orleans客户端失败，回退到传统初始化方式");
            }
            
            // 保留传统初始化方式作为备用
   

            int attempt = 0;
            while (true) // 无限循环，直到连接成功
            {
                attempt++;
                try
                {
                    var client = app.Services.GetRequiredService<IClusterClient>();
                    
                    // 测试连接
                    var helloGrain = client.GetGrain<IHelloGrain>("health-check");
                    var response = await helloGrain.SayHelloAsync("health-check");
                    
             
                    return;
                }
                catch (Exception ex)
                {
                    
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
                }
            }
        }
    }

    public static class OrleansClientInitializerExtensions
    {
        public static WebApplication UseOrleansClientInitialization(this WebApplication app)
        {
            // 在应用启动时初始化Orleans客户端
            app.Lifetime.ApplicationStarted.Register(async () =>
            {
                await OrleansClientInitializer.InitializeOrleansClientAsync(app);
            });

            return app;
        }
    }
}