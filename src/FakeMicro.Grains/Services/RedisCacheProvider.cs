using FakeMicro.Interfaces.Services;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
namespace FakeMicro.Grains.Services
{
    public interface IRedisCacheProvider
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
        Task RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
        Task ClearByPatternAsync(string pattern);
    }

    public class RedisCacheProvider : IRedisCacheProvider
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        private readonly ILogger<RedisCacheProvider> _logger;
        private readonly int _databaseId;

        public RedisCacheProvider(
            IConnectionMultiplexer redis,
            ILogger<RedisCacheProvider> logger,
            int databaseId = 0)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _databaseId = databaseId;
            _database = _redis.GetDatabase(databaseId);
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                var value = await _database.StringGetAsync(key);
                if (value.HasValue)
                {
                    var result = JsonSerializer.Deserialize<T>(value.ToString());
                    _logger.LogDebug("从Redis缓存获取数据成功: {Key}", key);
                    return result;
                }

                _logger.LogDebug("Redis缓存中不存在该键: {Key}", key);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从Redis缓存获取数据失败: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            try
            {
                if (value == null)
                {
                    _logger.LogWarning("尝试将空值存入Redis缓存: {Key}", key);
                    return;
                }

                var jsonValue = JsonSerializer.Serialize(value);
                if (expiration.HasValue)
                {
                    await _database.StringSetAsync(key, jsonValue, expiration.Value);
                }
                else
                {
                    await _database.StringSetAsync(key, jsonValue);
                }
                _logger.LogDebug("将数据存入Redis缓存成功: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "将数据存入Redis缓存失败: {Key}", key);
                throw;
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _database.KeyDeleteAsync(key);
                _logger.LogDebug("从Redis缓存移除数据成功: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从Redis缓存移除数据失败: {Key}", key);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                return await _database.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查Redis缓存键是否存在失败: {Key}", key);
                return false;
            }
        }

        public async Task ClearByPatternAsync(string pattern)
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var keys = server.Keys(pattern: pattern, database: _databaseId).ToArray();
                
                if (keys.Length > 0)
                {
                    await _database.KeyDeleteAsync(keys);
                    _logger.LogInformation("从Redis缓存批量删除数据成功，共删除 {Count} 个键", keys.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "按模式清空Redis缓存失败: {Pattern}", pattern);
                throw;
            }
        }
    }
}
