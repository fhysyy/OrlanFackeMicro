using FakeMicro.Interfaces.FakeMicro.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Interfaces;

/// <summary>
/// MongoDB仓储接口，提供MongoDB特有的CRUD操作和高级查询功能
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public interface IMongoRepository<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class
{
    /// <summary>
    /// 获取MongoDB集合
    /// </summary>
    /// <param name="databaseName">数据库名称，未提供时使用配置中的默认数据库</param>
    /// <param name="collectionName">集合名称，未提供时使用实体类名</param>
    /// <returns>MongoDB集合</returns>
    IMongoCollection<TEntity> GetCollection(string? databaseName = null, string? collectionName = null);

    ///// <summary>
    ///// 使用聚合管道查询
    ///// </summary>
    ///// <param name="pipeline">聚合管道</param>
    ///// <param name="cancellationToken">取消令牌</param>
    ///// <returns>聚合结果</returns>
    //Task<List<TEntity>> AggregateAsync(IEnumerable<PipelineStageDefinition<TEntity, TEntity>> pipeline, CancellationToken cancellationToken = default);

    #region 通用CRUD方法（带databaseName参数重载）

    #region 通用CRUD方法（带databaseName和collectionName参数重载）

    /// <summary>
    /// 获取所有实体（指定数据库）
    /// </summary>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体集合</returns>
    Task<IEnumerable<TEntity>> GetAllAsync(string? databaseName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有实体（指定数据库和集合）
    /// </summary>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体集合</returns>
    Task<IEnumerable<TEntity>> GetAllAsync(string? databaseName, string? collectionName, CancellationToken cancellationToken = default);



    /// <summary>
    /// 获取分页实体（指定数据库）
    /// </summary>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="orderBy">排序表达式</param>
    /// <param name="isDescending">是否降序</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    Task<PageBaseResultModel> GetPagedAsync(int pageIndex, int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool isDescending = false,
        string? databaseName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取分页实体（指定数据库和集合）
    /// </summary>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="orderBy">排序表达式</param>
    /// <param name="isDescending">是否降序</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    Task<PageBaseResultModel> GetPagedAsync(int pageIndex, int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool isDescending = false,
        string? databaseName = null,
        string? collectionName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据主键获取实体（指定数据库）
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体对象，如果不存在则返回null</returns>
    Task<TEntity?> GetByIdAsync(TKey id, string? databaseName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据主键获取实体（指定数据库和集合）
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体对象，如果不存在则返回null</returns>
    Task<TEntity?> GetByIdAsync(TKey id, string? databaseName, string? collectionName, CancellationToken cancellationToken = default);



    /// <summary>
    /// 根据条件获取实体（指定数据库）
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合条件的实体集合</returns>
    Task<IEnumerable<TEntity>> GetByConditionAsync(Expression<Func<TEntity, bool>> predicate,string? databaseName,CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据条件获取实体（指定数据库和集合）
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合条件的实体集合</returns>
    Task<IEnumerable<TEntity>> GetByConditionAsync(Expression<Func<TEntity, bool>> predicate,string? databaseName, string? collectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据条件获取分页实体（指定数据库）
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="orderBy">排序表达式</param>
    /// <param name="isDescending">是否降序</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    Task<PageBaseResultModel> GetPagedByConditionAsync(Expression<Func<TEntity, bool>> predicate, int pageIndex, int pageSize, Expression<Func<TEntity, object>>? orderBy = null, bool isDescending = false,
        string? databaseName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据条件获取分页实体（指定数据库和集合）
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="orderBy">排序表达式</param>
    /// <param name="isDescending">是否降序</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    Task<PageBaseResultModel> GetPagedByConditionAsync(Expression<Func<TEntity, bool>> predicate,  int pageIndex, int pageSize, Expression<Func<TEntity, object>>? orderBy = null,
        bool isDescending = false,
        string? databaseName = null,
        string? collectionName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据条件获取分页实体（使用BsonDocument过滤条件，指定数据库和集合）
    /// </summary>
    /// <param name="filter">MongoDB过滤条件</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="orderBy">排序表达式</param>
    /// <param name="isDescending">是否降序</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    Task<PageBaseResultModel> GetPagedByConditionAsync(FilterDefinition<BsonDocument> filter,
        int pageIndex, int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool isDescending = false,
        string? databaseName = null,
        string? collectionName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查实体是否存在（指定数据库）
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, string? databaseName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查实体是否存在（指定数据库和集合）
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, string? databaseName, string? collectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取实体数量（指定数据库）
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体数量</returns>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate, string? databaseName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取实体数量（指定数据库和集合）
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体数量</returns>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate,string? databaseName, string? collectionName,CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加实体（指定数据库）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task AddAsync(TEntity entity, string? databaseName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 添加实体（指定数据库和集合）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task AddAsync(TEntity entity, string? databaseName, string? collectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量添加实体（指定数据库）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task AddRangeAsync(IEnumerable<TEntity> entities, string? databaseName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量添加实体（指定数据库和集合）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task AddRangeAsync(IEnumerable<TEntity> entities, string? databaseName, string? collectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分批添加实体（适用于大量数据，指定数据库）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="batchSize">每批大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task AddBatchedAsync(IEnumerable<TEntity> entities, string? databaseName, int batchSize = 1000, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分批添加实体（适用于大量数据，指定数据库和集合）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="batchSize">每批大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task AddBatchedAsync(IEnumerable<TEntity> entities, string? databaseName, string? collectionName, int batchSize = 1000, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新实体（指定数据库）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateAsync(TEntity entity, string? databaseName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新实体（指定数据库和集合）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateAsync(TEntity entity, string? databaseName, string? collectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新实体（直接指定主键值）
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="entity">实体对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateAsync(TKey id, TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新实体（直接指定主键值，指定数据库）
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateAsync(TKey id, TEntity entity, string? databaseName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新实体（直接指定主键值，指定数据库和集合）
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateAsync(TKey id, TEntity entity, string? databaseName, string? collectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 部分更新实体（仅更新指定属性，指定数据库）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="properties">要更新的属性</param>
    Task UpdatePartialAsync(TEntity entity, string? databaseName, params Expression<Func<TEntity, object>>[] properties);

    /// <summary>
    /// 部分更新实体（仅更新指定属性，指定数据库和集合）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="properties">要更新的属性</param>
    Task UpdatePartialAsync(TEntity entity, string? databaseName, string? collectionName, params Expression<Func<TEntity, object>>[] properties);

    /// <summary>
    /// 部分更新实体（仅更新指定属性，带取消令牌和数据库名称）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="properties">要更新的属性</param>
    Task UpdatePartialAsync(TEntity entity, string? databaseName, CancellationToken cancellationToken, params Expression<Func<TEntity, object>>[] properties);

    /// <summary>
    /// 部分更新实体（仅更新指定属性，带取消令牌、数据库名称和集合名称）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="properties">要更新的属性</param>
    Task UpdatePartialAsync(TEntity entity, string? databaseName, string? collectionName, CancellationToken cancellationToken, params Expression<Func<TEntity, object>>[] properties);

    /// <summary>
    /// 批量更新实体（指定数据库）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, string? databaseName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新实体（指定数据库和集合）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, string? databaseName, string? collectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除实体（指定数据库）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteAsync(TEntity entity, string? databaseName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除实体（指定数据库和集合）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteAsync(TEntity entity, string? databaseName, string? collectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据主键删除实体（指定数据库）
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteByIdAsync(TKey id, string? databaseName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据主键删除实体（指定数据库和集合）
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteByIdAsync(TKey id, string? databaseName, string? collectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除实体（指定数据库）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteRangeAsync(IEnumerable<TEntity> entities, string? databaseName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除实体（指定数据库和集合）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteRangeAsync(IEnumerable<TEntity> entities, string? databaseName, string? collectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分批删除实体（适用于大量数据，指定数据库）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="batchSize">每批大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteBatchedAsync(IEnumerable<TEntity> entities, string? databaseName, int batchSize = 1000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 分批删除实体（适用于大量数据，指定数据库和集合）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="batchSize">每批大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteBatchedAsync(IEnumerable<TEntity> entities, string? databaseName, string? collectionName, int batchSize = 1000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据条件删除实体（指定数据库）
    /// </summary>
    /// <param name="predicate">删除条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的实体数量</returns>
    Task<int> DeleteByConditionAsync(Expression<Func<TEntity, bool>> predicate,
        string? databaseName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据条件删除实体（指定数据库和集合）
    /// </summary>
    /// <param name="predicate">删除条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的实体数量</returns>
    Task<int> DeleteByConditionAsync(Expression<Func<TEntity, bool>> predicate,
        string? databaseName,
        string? collectionName,
        CancellationToken cancellationToken = default);

    #endregion

    #region MongoDB特定方法

    ///// <summary>
    ///// 批量更新符合条件的文档
    ///// </summary>
    ///// <param name="filter">筛选条件</param>
    ///// <param name="update">更新定义</param>
    ///// <param name="cancellationToken">取消令牌</param>
    ///// <returns>更新的文档数量</returns>
    //Task<long> UpdateManyAsync(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新符合条件的文档（指定数据库）
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="update">更新定义</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新的文档数量</returns>
    Task<long> UpdateManyAsync(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, string? databaseName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量更新符合条件的文档（指定数据库和集合）
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="update">更新定义</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新的文档数量</returns>
    Task<long> UpdateManyAsync(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, string? databaseName, string? collectionName, CancellationToken cancellationToken = default);

    ///// <summary>
    ///// 批量删除符合条件的文档
    ///// </summary>
    ///// <param name="filter">筛选条件</param>
    ///// <param name="cancellationToken">取消令牌</param>
    ///// <returns>删除的文档数量</returns>
    //Task<long> DeleteManyAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除符合条件的文档（指定数据库）
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的文档数量</returns>
    Task<long> DeleteManyAsync(FilterDefinition<TEntity> filter, string? databaseName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除符合条件的文档（指定数据库和集合）
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的文档数量</returns>
    Task<long> DeleteManyAsync(FilterDefinition<TEntity> filter, string? databaseName, string? collectionName, CancellationToken cancellationToken = default);

    ///// <summary>
    ///// 使用FilterDefinition查询文档
    ///// </summary>
    ///// <param name="filter">筛选条件</param>
    ///// <param name="cancellationToken">取消令牌</param>
    ///// <returns>符合条件的文档集合</returns>
    //Task<List<TEntity>> FindAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用FilterDefinition查询文档（指定数据库）
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合条件的文档集合</returns>
    Task<List<TEntity>> FindAsync(FilterDefinition<TEntity> filter, string? databaseName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用FilterDefinition查询文档（指定数据库和集合）
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合条件的文档集合</returns>
    Task<List<TEntity>> FindAsync(FilterDefinition<TEntity> filter, string? databaseName, string? collectionName, CancellationToken cancellationToken = default);

    ///// <summary>
    ///// 使用FilterDefinition查询单个文档
    ///// </summary>
    ///// <param name="filter">筛选条件</param>
    ///// <param name="cancellationToken">取消令牌</param>
    ///// <returns>符合条件的单个文档</returns>
    //Task<TEntity> FindOneAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用FilterDefinition查询单个文档（指定数据库）
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合条件的单个文档</returns>
    Task<TEntity> FindOneAsync(FilterDefinition<TEntity> filter, string? databaseName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用FilterDefinition查询单个文档（指定数据库和集合）
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合条件的单个文档</returns>
    Task<TEntity> FindOneAsync(FilterDefinition<TEntity> filter, string? databaseName, string? collectionName, CancellationToken cancellationToken = default);

    #endregion

    #endregion

}
