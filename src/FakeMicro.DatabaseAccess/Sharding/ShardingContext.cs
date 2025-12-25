using System;using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace FakeMicro.DatabaseAccess.Sharding;

/// <summary>
/// 分片上下文实现类
/// </summary>
public class ShardingContext : IShardingContext
{
    private readonly ILogger<ShardingContext> _logger;
    private readonly Dictionary<Type, PropertyInfo> _shardingKeyProperties = new();
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="config">分片配置</param>
    public ShardingContext(ILogger<ShardingContext> logger, ShardingConfig config)
    {
        _logger = logger;
        Config = config;
    }
    
    /// <inheritdoc />
    public ShardingConfig Config { get; }
    
    /// <inheritdoc />
    public int GetShardIndex<TEntity, TKey>(TKey key)
    {
        if (!Config.Enabled)
        {
            return 0;
        }
        
        var strategy = Config.GetStrategy<TEntity>();
        if (strategy == null)
        {
            return 0;
        }
        
        try
        {
            switch (strategy.Type)
            {
                case ShardingType.Range:
                    return GetRangeShardIndex(key, strategy);
                case ShardingType.Hash:
                    return GetHashShardIndex(key, strategy);
                case ShardingType.Date:
                    return GetDateShardIndex(key, strategy);
                case ShardingType.Custom:
                    return strategy.CustomShardingFunc != null ? strategy.CustomShardingFunc(key) : 0;
                default:
                    return 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计算分片索引失败，实体类型: {EntityType}", typeof(TEntity).Name);
            return 0;
        }
    }
    
    /// <inheritdoc />
    public string GetShardName<TEntity, TKey>(TKey key)
    {
        var shardIndex = GetShardIndex<TEntity, TKey>(key);
        return $"shard_{shardIndex}";
    }
    
    /// <inheritdoc />
    public int[] GetAllShardIndexes()
    {
        if (!Config.Enabled)
        {
            return new[] { 0 };
        }
        
        // 获取最大分片数量
        int maxShards = 1;
        foreach (var strategy in Config.Strategies.Values)
        {
            if (strategy.ShardCount > maxShards)
            {
                maxShards = strategy.ShardCount;
            }
        }
        
        var indexes = new int[maxShards];
        for (int i = 0; i < maxShards; i++)
        {
            indexes[i] = i;
        }
        
        return indexes;
    }
    
    /// <summary>
    /// 获取范围分片索引
    /// </summary>
    private int GetRangeShardIndex<TKey>(TKey key, ShardingStrategy strategy)
    {
        // 简化实现：假设范围是均匀分布的
        var keyInt = Convert.ToInt32(key);
        return keyInt % strategy.ShardCount;
    }
    
    /// <summary>
    /// 获取哈希分片索引
    /// </summary>
    private int GetHashShardIndex<TKey>(TKey key, ShardingStrategy strategy)
    {
        var hash = key.GetHashCode();
        return Math.Abs(hash) % strategy.ShardCount;
    }
    
    /// <summary>
    /// 获取日期分片索引
    /// </summary>
    private int GetDateShardIndex<TKey>(TKey key, ShardingStrategy strategy)
    {
        if (key is DateTime date)
        {
            // 简化实现：按年份分片
            var year = date.Year;
            return year % strategy.ShardCount;
        }
        
        if (key is DateTimeOffset dateOffset)
        {
            var year = dateOffset.Year;
            return year % strategy.ShardCount;
        }
        
        throw new ArgumentException($"键类型 {typeof(TKey).Name} 不是有效的日期类型");
    }
}