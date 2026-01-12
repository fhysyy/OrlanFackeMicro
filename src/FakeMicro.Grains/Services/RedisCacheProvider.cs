using FakeMicro.Interfaces.Services;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace FakeMicro.Grains.Services
{
    public interface IRedisCacheProvider
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
        Task RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
        Task ClearByPatternAsync(string pattern);
        Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null) where T : class;
    }

    public class RedisCacheProvider : IRedisCacheProvider
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        private readonly ILogger<RedisCacheProvider> _logger;
        private readonly int _databaseId;
        private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan BloomFilterExpiration = TimeSpan.FromHours(24);
        private const string BloomFilterPrefix = "bloom:";
        private const string NullCachePrefix = "null:";
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(100, 100);

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

        /// <summary>
        /// 获取缓存，实现防穿透保护（空值缓存）
        /// </summary>
        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                // 检查是否为空值缓存
                var nullKey = NullCachePrefix + key;
                if (await _database.KeyExistsAsync(nullKey))
                {
                    _logger.LogDebug("命中空值缓存: {Key}", key);
                    return null;
                }

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

        /// <summary>
        /// 设置缓存，添加随机过期时间防止雪崩
        /// </summary>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            try
            {
                if (value == null)
                {
                    // 空值缓存，防止缓存穿透
                    var nullKey = NullCachePrefix + key;
                    await _database.StringSetAsync(nullKey, "1", TimeSpan.FromMinutes(5));
                    _logger.LogDebug("将空值存入Redis缓存: {Key}", key);
                    return;
                }

                var jsonValue = JsonSerializer.Serialize(value);
                
                // 添加随机过期时间（±10%），防止缓存雪崩
                var finalExpiration = expiration ?? DefaultExpiration;
                var randomOffset = TimeSpan.FromSeconds(Random.Shared.Next(-(int)(finalExpiration.TotalSeconds * 0.1), (int)(finalExpiration.TotalSeconds * 0.1)));
                finalExpiration = finalExpiration.Add(randomOffset);

                await _database.StringSetAsync(key, jsonValue, finalExpiration);
                _logger.LogDebug("将数据存入Redis缓存成功: {Key}, 过期时间: {Expiration}", key, finalExpiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "将数据存入Redis缓存失败: {Key}", key);
                throw;
            }
        }

        /// <summary>
        /// 获取或设置缓存，使用分布式锁防止击穿
        /// </summary>
        public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null) where T : class
        {
            try
            {
                // 先尝试从缓存获取
                var cached = await GetAsync<T>(key);
                if (cached != null)
                {
                    return cached;
                }

                // 使用分布式锁防止缓存击穿
                var lockKey = $"lock:{key}";
                var lockValue = Guid.NewGuid().ToString();
                var lockExpiry = TimeSpan.FromSeconds(10);

                // 尝试获取锁
                var lockAcquired = await _database.StringSetAsync(lockKey, lockValue, lockExpiry, When.NotExists);

                if (lockAcquired)
                {
                    try
                    {
                        _logger.LogDebug("获取分布式锁成功: {LockKey}", lockKey);

                        // 双重检查：再次尝试从缓存获取
                        cached = await GetAsync<T>(key);
                        if (cached != null)
                        {
                            return cached;
                        }

                        // 从数据源获取数据
                        var value = await factory();
                        
                        // 设置缓存（包括空值）
                        await SetAsync(key, value, expiration);

                        return value;
                    }
                    finally
                    {
                        // 释放锁
                        var script = @"
                            if redis.call('get', KEYS[1]) == ARGV[1] then
                                return redis.call('del', KEYS[1])
                            else
                                return 0
                            end";
                        await _database.ScriptEvaluateAsync(script, new RedisKey[] { lockKey }, new RedisValue[] { lockValue });
                        _logger.LogDebug("释放分布式锁: {LockKey}", lockKey);
                    }
                }
                else
                {
                    // 未获取到锁，等待一段时间后重试
                    _logger.LogDebug("未获取到分布式锁，等待后重试: {LockKey}", lockKey);
                    await Task.Delay(100);
                    
                    // 重试获取缓存
                    cached = await GetAsync<T>(key);
                    if (cached != null)
                    {
                        return cached;
                    }

                    // 降级处理：直接查询数据源
                    _logger.LogWarning("多次重试失败，直接查询数据源: {Key}", key);
                    return await factory();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetOrSet操作失败: {Key}", key);
                // 降级：直接从数据源获取
                return await factory();
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _database.KeyDeleteAsync(key);
                
                // 同时删除空值缓存
                var nullKey = NullCachePrefix + key;
                await _database.KeyDeleteAsync(nullKey);
                
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
