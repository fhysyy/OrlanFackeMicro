using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;

/// <summary>
/// 实用工具配置扩展
/// 提供依赖注入和配置相关功能
/// </summary>
namespace FakeMicro.Utilities.Configuration
{
    /// <summary>
    /// 缓存相关接口和实现
    /// </summary>
    public static class CachingServiceExtensions
    {
        /// <summary>
        /// 添加缓存服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddCachingServices(
            this IServiceCollection services)
        {
            // 添加内存缓存
            services.AddMemoryCache();
            services.AddSingleton<IMemoryCacheProvider, MemoryCacheProvider>();

            // 添加缓存管理器
            services.AddSingleton<ICacheManager, CacheManager>();

            return services;
        }
    }

    /// <summary>
    /// 缓存提供商接口
    /// </summary>
    public interface ICacheProvider
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class;
        Task RemoveAsync(string key);
        Task ClearAsync();
    }

    /// <summary>
    /// 内存缓存提供商接口
    /// </summary>
    public interface IMemoryCacheProvider : ICacheProvider { }

    /// <summary>
    /// 分布式缓存提供商接口
    /// </summary>
    public interface IDistributedCacheProvider : ICacheProvider { }

    /// <summary>
    /// 缓存管理器接口
    /// </summary>
    public interface ICacheManager
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? memoryExpiration = null, TimeSpan? distributedExpiration = null) where T : class;
        Task RemoveAsync(string key);
        Task ClearByPatternAsync(string pattern);
        Task WarmupCacheAsync<T>(IEnumerable<KeyValuePair<string, T>> items) where T : class;
    }

    /// <summary>
    /// 内存缓存提供商实现
    /// </summary>
    public class MemoryCacheProvider : IMemoryCacheProvider
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<MemoryCacheProvider> _logger;

        public MemoryCacheProvider(IMemoryCache cache, ILogger<MemoryCacheProvider> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ClearAsync()
        {
            // 内存缓存没有直接清空所有缓存的方法
            _logger.LogWarning("内存缓存不支持批量清空操作");
            await Task.CompletedTask;
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            if (_cache.TryGetValue(key, out T? value))
            {
                _logger.LogDebug("从内存缓存获取数据成功: {Key}", key);
                return value;
            }

            _logger.LogDebug("内存缓存中不存在该键: {Key}", key);
            return null;
        }

        public async Task RemoveAsync(string key)
        {
            _cache.Remove(key);
            _logger.LogDebug("从内存缓存移除数据成功: {Key}", key);
            await Task.CompletedTask;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
        {
            if (value == null)
            {
                _logger.LogWarning("尝试将空值存入内存缓存: {Key}", key);
                return;
            }

            _cache.Set(key, value, expiration);
            _logger.LogDebug("将数据存入内存缓存成功: {Key}", key);
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// 分布式缓存提供商实现
    /// </summary>
    public class DistributedCacheProvider : IDistributedCacheProvider
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<DistributedCacheProvider> _logger;

        public DistributedCacheProvider(IDistributedCache cache, ILogger<DistributedCacheProvider> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ClearAsync()
        {
            // 分布式缓存没有直接清空所有缓存的方法，需要根据具体实现来处理
            _logger.LogWarning("分布式缓存不支持批量清空操作");
            await Task.CompletedTask;
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            var value = await _cache.GetStringAsync(key);
            if (value != null)
            {
                _logger.LogDebug("从分布式缓存获取数据成功: {Key}", key);
                return System.Text.Json.JsonSerializer.Deserialize<T>(value);
            }

            _logger.LogDebug("分布式缓存中不存在该键: {Key}", key);
            return null;
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
            _logger.LogDebug("从分布式缓存移除数据成功: {Key}", key);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
        {
            if (value == null)
            {
                _logger.LogWarning("尝试将空值存入分布式缓存: {Key}", key);
                return;
            }

            var jsonValue = System.Text.Json.JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, jsonValue, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            });
            _logger.LogDebug("将数据存入分布式缓存成功: {Key}", key);
        }
    }

    /// <summary>
    /// 缓存管理器实现
    /// </summary>
    public class CacheManager : ICacheManager
    {
        private readonly IMemoryCacheProvider _memoryCacheProvider;
        private readonly IDistributedCacheProvider? _distributedCacheProvider;
        private readonly ILogger<CacheManager> _logger;

        public CacheManager(
            IMemoryCacheProvider memoryCacheProvider,
            IDistributedCacheProvider? distributedCacheProvider,
            ILogger<CacheManager> logger)
        {
            _memoryCacheProvider = memoryCacheProvider ?? throw new ArgumentNullException(nameof(memoryCacheProvider));
            _distributedCacheProvider = distributedCacheProvider;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ClearByPatternAsync(string pattern)
        {
            // 简单实现，实际应用中可能需要更复杂的模式匹配
            _logger.LogWarning("缓存管理器不支持按模式清空操作");
            await Task.CompletedTask;
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            // 先从内存缓存获取
            var value = await _memoryCacheProvider.GetAsync<T>(key);
            if (value != null)
            {
                return value;
            }

            // 如果内存缓存没有，且有分布式缓存，则从分布式缓存获取
            if (_distributedCacheProvider != null)
            {
                value = await _distributedCacheProvider.GetAsync<T>(key);
                if (value != null)
                {
                    // 将数据存入内存缓存，设置较短的过期时间
                    await _memoryCacheProvider.SetAsync(key, value, TimeSpan.FromMinutes(5));
                }
            }

            return value;
        }

        public async Task RemoveAsync(string key)
        {
            // 从内存缓存移除
            await _memoryCacheProvider.RemoveAsync(key);

            // 如果有分布式缓存，也从分布式缓存移除
            if (_distributedCacheProvider != null)
            {
                await _distributedCacheProvider.RemoveAsync(key);
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? memoryExpiration = null, TimeSpan? distributedExpiration = null) where T : class
        {
            if (value == null)
            {
                _logger.LogWarning("尝试将空值存入缓存: {Key}", key);
                return;
            }

            // 设置内存缓存，默认10分钟
            await _memoryCacheProvider.SetAsync(key, value, memoryExpiration ?? TimeSpan.FromMinutes(10));

            // 如果有分布式缓存，也设置分布式缓存，默认1小时
            if (_distributedCacheProvider != null)
            {
                await _distributedCacheProvider.SetAsync(key, value, distributedExpiration ?? TimeSpan.FromHours(1));
            }
        }

        public async Task WarmupCacheAsync<T>(IEnumerable<KeyValuePair<string, T>> items) where T : class
        {
            foreach (var item in items)
            {
                await SetAsync(item.Key, item.Value);
            }
        }
    }
}