using System;using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Sharding;

/// <summary>
/// 分片上下文接口
/// </summary>
public interface IShardingContext
{
    /// <summary>
    /// 分片配置
    /// </summary>
    ShardingConfig Config { get; }
    
    /// <summary>
    /// 根据实体和键获取分片索引
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <param name="key">键值</param>
    /// <returns>分片索引</returns>
    int GetShardIndex<TEntity, TKey>(TKey key);
    
    /// <summary>
    /// 根据实体和键获取分片名称
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">键类型</typeparam>
    /// <param name="key">键值</param>
    /// <returns>分片名称</returns>
    string GetShardName<TEntity, TKey>(TKey key);
    
    /// <summary>
    /// 获取所有分片索引
    /// </summary>
    /// <returns>分片索引数组</returns>
    int[] GetAllShardIndexes();
}