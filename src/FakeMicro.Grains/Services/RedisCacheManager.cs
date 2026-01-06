using FakeMicro.Interfaces.Services;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FakeMicro.Grains.Services
{
    public interface IRedisCacheManager
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
        Task RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
        Task ClearByPatternAsync(string pattern);
        Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class;
    }

    public class RedisCacheManager : IRedisCacheManager
    {
        private readonly IRedisCacheProvider _cacheProvider;
        private readonly ILogger<RedisCacheManager> _logger;

        public RedisCacheManager(
            IRedisCacheProvider cacheProvider,
            ILogger<RedisCacheManager> logger)
        {
            _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                return await _cacheProvider.GetAsync<T>(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从Redis缓存管理器获取数据失败: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            try
            {
                await _cacheProvider.SetAsync(key, value, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "向Redis缓存管理器设置数据失败: {Key}", key);
                throw;
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cacheProvider.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从Redis缓存管理器删除数据失败: {Key}", key);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                return await _cacheProvider.ExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查Redis缓存管理器中键是否存在失败: {Key}", key);
                return false;
            }
        }

        public async Task ClearByPatternAsync(string pattern)
        {
            try
            {
                await _cacheProvider.ClearByPatternAsync(pattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "按模式清空Redis缓存管理器失败: {Pattern}", pattern);
                throw;
            }
        }

        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class
        {
            try
            {
                var value = await _cacheProvider.GetAsync<T>(key);
                if (value != null)
                {
                    _logger.LogDebug("从Redis缓存管理器获取数据成功: {Key}", key);
                    return value;
                }

                _logger.LogDebug("Redis缓存管理器中不存在该键，开始生成数据: {Key}", key);
                var newValue = await factory();
                if (newValue != null)
                {
                    await _cacheProvider.SetAsync(key, newValue, expiration);
                    _logger.LogDebug("将新生成的数据存入Redis缓存管理器成功: {Key}", key);
                }

                return newValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOrSet操作失败: {Key}", key);
                throw;
            }
        }
    }
}
