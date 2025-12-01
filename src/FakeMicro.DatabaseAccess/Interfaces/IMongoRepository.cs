using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace FakeMicro.DatabaseAccess.Interfaces;

/// <summary>
/// MongoDB仓储接口
/// 继承自通用仓储接口，并添加MongoDB特定方法
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public interface IMongoRepository<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class
{
    /// <summary>
    /// 获取MongoDB集合
    /// </summary>
    /// <returns>MongoDB集合</returns>
    IMongoCollection<TEntity> GetCollection();

    ///// <summary>
    ///// 使用聚合管道查询
    ///// </summary>
    ///// <param name="pipeline">聚合管道</param>
    ///// <param name="cancellationToken">取消令牌</param>
    ///// <returns>聚合结果</returns>
    //Task<List<TEntity>> AggregateAsync(IEnumerable<PipelineStageDefinition<TEntity, TEntity>> pipeline, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新符合条件的文档
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="update">更新定义</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新的文档数量</returns>
    Task<long> UpdateManyAsync(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除符合条件的文档
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的文档数量</returns>
    Task<long> DeleteManyAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用FilterDefinition查询文档
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合条件的文档集合</returns>
    Task<List<TEntity>> FindAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用FilterDefinition查询单个文档
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合条件的单个文档</returns>
    Task<TEntity> FindOneAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken = default);
}
