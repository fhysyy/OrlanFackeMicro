using System;using System.Collections.Generic;

namespace FakeMicro.DatabaseAccess.Sharding;

/// <summary>
/// 分片类型枚举
/// </summary>
public enum ShardingType
{
    /// <summary>
    /// 基于键的范围分片
    /// </summary>
    Range,
    /// <summary>
    /// 基于键的哈希分片
    /// </summary>
    Hash,
    /// <summary>
    /// 基于日期的分片
    /// </summary>
    Date,
    /// <summary>
    /// 自定义分片
    /// </summary>
    Custom
}

/// <summary>
/// 分片策略类
/// </summary>
public class ShardingStrategy
{
    /// <summary>
    /// 分片类型
    /// </summary>
    public ShardingType Type { get; set; }
    
    /// <summary>
    /// 分片键（属性名）
    /// </summary>
    public string ShardingKey { get; set; }
    
    /// <summary>
    /// 分片数量
    /// </summary>
    public int ShardCount { get; set; }
    
    /// <summary>
    /// 分片规则配置
    /// </summary>
    public Dictionary<string, object> Rules { get; set; } = new Dictionary<string, object>();
    
    /// <summary>
    /// 自定义分片函数
    /// </summary>
    public Func<object, int> CustomShardingFunc { get; set; }
}

/// <summary>
/// 分片配置类
/// </summary>
public class ShardingConfig
{
    /// <summary>
    /// 分片策略字典
    /// Key: 实体类型名称
    /// Value: 分片策略
    /// </summary>
    public Dictionary<string, ShardingStrategy> Strategies { get; set; } = new Dictionary<string, ShardingStrategy>();
    
    /// <summary>
    /// 是否启用分片
    /// </summary>
    public bool Enabled { get; set; }
    
    /// <summary>
    /// 添加分片策略
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <param name="strategy">分片策略</param>
    public void AddStrategy<TEntity>(ShardingStrategy strategy)
    {
        var entityType = typeof(TEntity).Name;
        Strategies[entityType] = strategy;
    }
    
    /// <summary>
    /// 获取分片策略
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <returns>分片策略</returns>
    public ShardingStrategy? GetStrategy<TEntity>()
    {
        var entityType = typeof(TEntity).Name;
        return Strategies.TryGetValue(entityType, out var strategy) ? strategy : null;
    }
}