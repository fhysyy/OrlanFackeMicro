#nullable enable
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// MongoDB仓储实现
/// 直接基于MongoDB.Driver实现通用仓储接口和MongoDB特定接口
/// 遵循Orleans最佳实践
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public partial class MongoRepository<TEntity, TKey>:IMongoRepository<TEntity, TKey> where TEntity : class
{
    private readonly MongoClient _mongoClient;
    private readonly ILogger<MongoRepository<TEntity, TKey>> _logger;
    private readonly string? _defaultDatabaseName;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="mongoClient">MongoDB客户端</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="defaultDatabaseName">默认数据库名称</param>
    public MongoRepository(MongoClient mongoClient, ILogger<MongoRepository<TEntity, TKey>> logger, string? defaultDatabaseName = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mongoClient = mongoClient ?? throw new ArgumentNullException(nameof(mongoClient));
        _defaultDatabaseName = defaultDatabaseName ?? "FakeMicroDB";
    }

    /// <summary>
    /// 获取实体类型
    /// </summary>
    public Type EntityType => typeof(TEntity);

    /// <summary>
    /// 获取MongoDB集合
    /// </summary>
    /// <param name="databaseName">数据库名称，未提供时使用默认数据库名称</param>
    /// <param name="collectionName">集合名称，未提供时使用实体类名</param>
    /// <returns>MongoDB集合</returns>
    public IMongoCollection<TEntity> GetCollection(string? databaseName = null, string? collectionName = null)
    {
        // 优先级：方法参数 > 类构造函数参数
        var dbName = databaseName ?? _defaultDatabaseName;
        var database = _mongoClient.GetDatabase(dbName);
        // 优先级：方法参数 > 实体类名
        var collName = collectionName ?? typeof(TEntity).Name;
        return database.GetCollection<TEntity>(collName);
    }

    /// <summary>
    /// 获取MongoDB集合（泛型版本，支持指定不同的实体类型）
    /// </summary>
    /// <typeparam name="TOtherEntity">集合中的实体类型</typeparam>
    /// <param name="databaseName">数据库名称，未提供时使用默认数据库名称</param>
    /// <param name="collectionName">集合名称，未提供时使用实体类名</param>
    /// <returns>MongoDB集合</returns>
    public IMongoCollection<TOtherEntity> GetCollection<TOtherEntity>(string? databaseName = null, string? collectionName = null) where TOtherEntity : class
    {
        // 优先级：方法参数 > 类构造函数参数
        var dbName = databaseName ?? _defaultDatabaseName;
        var database = _mongoClient.GetDatabase(dbName);
        // 优先级：方法参数 > 实体类名
        var collName = collectionName ?? typeof(TOtherEntity).Name;
        return database.GetCollection<TOtherEntity>(collName);
    }

    #region 查询方法（支持collectionName）

    /// <summary>
    /// 获取所有实体（指定数据库和集合）
    /// </summary>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体集合</returns>
    public async Task<IEnumerable<TEntity>> GetAllAsync(string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName, collectionName);
        return await collection.Find(_ => true).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取所有实体（指定数据库）
    /// </summary>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体集合</returns>
    public async Task<IEnumerable<TEntity>> GetAllAsync(string? databaseName, CancellationToken cancellationToken = default)
    {
        return await GetAllAsync(databaseName, null, cancellationToken);
    }

    /// <summary>
    /// 获取所有实体
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体集合</returns>
    public async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await GetAllAsync(null, null, cancellationToken);
    }

    /// <summary>
    /// 根据主键获取实体（指定数据库和集合）
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体对象，如果不存在则返回null</returns>
    public async Task<TEntity?> GetByIdAsync(TKey id, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName, collectionName);
        var filter = Builders<TEntity>.Filter.Eq("_id", id);
        return await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// 根据主键获取实体（指定数据库）
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体对象，如果不存在则返回null</returns>
    public async Task<TEntity?> GetByIdAsync(TKey id, string? databaseName, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync(id, databaseName, null, cancellationToken);
    }

    /// <summary>
    /// 根据主键获取实体
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体对象，如果不存在则返回null</returns>
    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync(id, null, null, cancellationToken);
    }

    /// <summary>
    /// 根据条件获取实体（指定数据库和集合）
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合条件的实体集合</returns>
    public async Task<IEnumerable<TEntity>> GetByConditionAsync(Expression<Func<TEntity, bool>> predicate, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName, collectionName);
        return await collection.Find(predicate).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据条件获取实体（指定数据库）
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合条件的实体集合</returns>
    public async Task<IEnumerable<TEntity>> GetByConditionAsync(Expression<Func<TEntity, bool>> predicate, string? databaseName, CancellationToken cancellationToken = default)
    {
        return await GetByConditionAsync(predicate, databaseName, null, cancellationToken);
    }

    /// <summary>
    /// 根据条件获取实体
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合条件的实体集合</returns>
    public async Task<IEnumerable<TEntity>> GetByConditionAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await GetByConditionAsync(predicate, null, null, cancellationToken);
    }

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
        public async Task<PagedResult<TEntity>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<TEntity, object>>? orderBy = null, bool isDescending = false, string? databaseName = null, string? collectionName = null, CancellationToken cancellationToken = default)
        {
            var collection = GetCollection(databaseName, collectionName);
            var totalCount = await collection.CountDocumentsAsync(_ => true, cancellationToken: cancellationToken);

            var query = collection.Find(_ => true);

            // 应用排序
            if (orderBy != null)
            {
                query = isDescending ? query.SortByDescending(orderBy) : query.SortBy(orderBy);
            }

            // 应用分页
            var items = await query
                .Skip((pageIndex - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync(cancellationToken);

            return PagedResult<TEntity>.SuccessResult(items, (int)totalCount, pageIndex, pageSize);
        }

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
    public async Task<PagedResult<TEntity>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<TEntity, object>>? orderBy = null, bool isDescending = false, string? databaseName = null, CancellationToken cancellationToken = default)
    {
        return await GetPagedAsync(pageIndex, pageSize, orderBy, isDescending, databaseName, null, cancellationToken);
    }

    /// <summary>
    /// 获取分页实体
    /// </summary>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="orderBy">排序表达式</param>
    /// <param name="isDescending">是否降序</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    public async Task<PagedResult<TEntity>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<TEntity, object>>? orderBy = null, bool isDescending = false, CancellationToken cancellationToken = default)
    {
        return await GetPagedAsync(pageIndex, pageSize, orderBy, isDescending, null, null, cancellationToken);
    }

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
        public async Task<PagedResult<TEntity>> GetPagedByConditionAsync(Expression<Func<TEntity, bool>> predicate, int pageIndex, int pageSize, Expression<Func<TEntity, object>>? orderBy = null, bool isDescending = false, string? databaseName = null, string? collectionName = null, CancellationToken cancellationToken = default)
        {
            var collection = GetCollection(databaseName, collectionName);
            var totalCount = await collection.CountDocumentsAsync(predicate, cancellationToken: cancellationToken);

            var query = collection.Find(predicate);

            // 应用排序
            if (orderBy != null)
            {
                query = isDescending ? query.SortByDescending(orderBy) : query.SortBy(orderBy);
            }

            // 应用分页
            var items = await query
                .Skip((pageIndex - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync(cancellationToken);

            return PagedResult<TEntity>.SuccessResult(items, (int)totalCount, pageIndex, pageSize);
        }

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
    public async Task<PagedResult<TEntity>> GetPagedByConditionAsync(Expression<Func<TEntity, bool>> predicate, int pageIndex, int pageSize, Expression<Func<TEntity, object>>? orderBy = null, bool isDescending = false, string? databaseName = null, CancellationToken cancellationToken = default)
    {
        return await GetPagedByConditionAsync(predicate, pageIndex, pageSize, orderBy, isDescending, databaseName, null, cancellationToken);
    }

    /// <summary>
    /// 根据条件获取分页实体
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="orderBy">排序表达式</param>
    /// <param name="isDescending">是否降序</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    public async Task<PagedResult<TEntity>> GetPagedByConditionAsync(Expression<Func<TEntity, bool>> predicate, int pageIndex, int pageSize, Expression<Func<TEntity, object>>? orderBy = null, bool isDescending = false, CancellationToken cancellationToken = default)
    {
        return await GetPagedByConditionAsync(predicate, pageIndex, pageSize, orderBy, isDescending, null, null, cancellationToken);
    }

    /// <summary>
    /// 检查实体是否存在（指定数据库和集合）
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName, collectionName);
        return await collection.Find(predicate).AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 检查实体是否存在（指定数据库）
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, string? databaseName, CancellationToken cancellationToken = default)
    {
        return await ExistsAsync(predicate, databaseName, null, cancellationToken);
    }

    /// <summary>
    /// 检查实体是否存在
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await ExistsAsync(predicate, null, null, cancellationToken);
    }

    /// <summary>
    /// 获取实体数量（指定数据库和集合）
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体数量</returns>
    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName, collectionName);
        var filter = predicate != null ? Builders<TEntity>.Filter.Where(predicate) : Builders<TEntity>.Filter.Empty;
        return (int)await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 获取实体数量（指定数据库）
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体数量</returns>
    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate, string? databaseName, CancellationToken cancellationToken = default)
    {
        return await CountAsync(predicate, databaseName, null, cancellationToken);
    }

    /// <summary>
    /// 获取实体数量
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体数量</returns>
    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate, CancellationToken cancellationToken = default)
    {
        return await CountAsync(predicate, null, null, cancellationToken);
    }

    #endregion

    #region 写操作方法

    /// <summary>
    /// 添加实体（指定数据库和集合）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task AddAsync(TEntity entity, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName, collectionName);
        await collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 添加实体（指定数据库）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task AddAsync(TEntity entity, string? databaseName, CancellationToken cancellationToken = default)
    {
        await AddAsync(entity, databaseName, null, cancellationToken);
    }

    /// <summary>
    /// 添加实体
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await AddAsync(entity, null, null, cancellationToken);
    }

    /// <summary>
    /// 批量添加实体（指定数据库和集合）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task AddRangeAsync(IEnumerable<TEntity> entities, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName, collectionName);
        await collection.InsertManyAsync(entities, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 批量添加实体（指定数据库）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task AddRangeAsync(IEnumerable<TEntity> entities, string? databaseName, CancellationToken cancellationToken = default)
    {
        await AddRangeAsync(entities, databaseName, null, cancellationToken);
    }

    /// <summary>
    /// 批量添加实体
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await AddRangeAsync(entities, null, null, cancellationToken);
    }

    /// <summary>
    /// 分批添加实体（适用于大量数据，指定数据库）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="batchSize">每批大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task AddBatchedAsync(IEnumerable<TEntity> entities, string? databaseName, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        await AddBatchedAsync(entities, databaseName, null, batchSize, cancellationToken);
    }

    /// <summary>
    /// 分批添加实体（适用于大量数据，指定数据库和集合）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="batchSize">每批大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task AddBatchedAsync(IEnumerable<TEntity> entities, string? databaseName, string? collectionName, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName, collectionName);
        var entityList = entities.ToList();
        
        for (int i = 0; i < entityList.Count; i += batchSize)
        {
            var batch = entityList.Skip(i).Take(batchSize);
            await collection.InsertManyAsync(batch, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// 分批添加实体（适用于大量数据）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="batchSize">每批大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task AddBatchedAsync(IEnumerable<TEntity> entities, int batchSize = 1000, CancellationToken cancellationToken = default)
    {
        await AddBatchedAsync(entities, null, null, batchSize, cancellationToken);
    }

    /// <summary>
    /// 部分更新实体（指定数据库和集合）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="properties">要更新的属性</param>
    public void UpdatePartial(TEntity entity, string? databaseName, string? collectionName, params Expression<Func<TEntity, object>>[] properties)
    {
        UpdatePartialAsync(entity, databaseName, collectionName, CancellationToken.None, properties).Wait();
    }

    /// <summary>
    /// 部分更新实体（指定数据库）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="properties">要更新的属性</param>
    public void UpdatePartial(TEntity entity, string? databaseName, params Expression<Func<TEntity, object>>[] properties)
    {
        UpdatePartialAsync(entity, databaseName, null, CancellationToken.None, properties).Wait();
    }

    /// <summary>
    /// 部分更新实体
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="properties">要更新的属性</param>
    public void UpdatePartial(TEntity entity, params Expression<Func<TEntity, object>>[] properties)
    {
        UpdatePartialAsync(entity, null, null, CancellationToken.None, properties).Wait();
    }

    /// <summary>
    /// 部分更新实体（异步，指定数据库和集合）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="properties">要更新的属性</param>
    public async Task UpdatePartialAsync(TEntity entity, string? databaseName, string? collectionName, CancellationToken cancellationToken, params Expression<Func<TEntity, object>>[] properties)
    {
        var collection = GetCollection(databaseName, collectionName);
        var idProperty = typeof(TEntity).GetProperty("Id");
        if (idProperty == null)
        {
            throw new InvalidOperationException("实体必须包含Id属性");
        }

        var idValue = (TKey)idProperty.GetValue(entity);
        var filter = Builders<TEntity>.Filter.Eq("_id", idValue);

        // 创建更新定义
        var update = Builders<TEntity>.Update.Combine();

        // 应用要更新的属性
        foreach (var property in properties)
        {
            var propertyName = ExtractPropertyName(property);
            var propertyValue = GetPropertyValue(entity, propertyName);
            update = update.Set(propertyName, propertyValue);
        }

        await collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 部分更新实体（异步，指定数据库）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="properties">要更新的属性</param>
    public async Task UpdatePartialAsync(TEntity entity, string? databaseName, CancellationToken cancellationToken, params Expression<Func<TEntity, object>>[] properties)
    {
        await UpdatePartialAsync(entity, databaseName, null, cancellationToken, properties);
    }

    /// <summary>
    /// 部分更新实体（异步）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="properties">要更新的属性</param>
    public async Task UpdatePartialAsync(TEntity entity, CancellationToken cancellationToken, params Expression<Func<TEntity, object>>[] properties)
    {
        await UpdatePartialAsync(entity, null, null, cancellationToken, properties);
    }

    /// <summary>
    /// 部分更新实体（异步，仅更新指定属性）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="properties">要更新的属性</param>
    public async Task UpdatePartialAsync(TEntity entity, params Expression<Func<TEntity, object>>[] properties)
    {
        await UpdatePartialAsync(entity, null, null, CancellationToken.None, properties);
    }

    /// <summary>
    /// 部分更新实体（仅更新指定属性，指定数据库）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="properties">要更新的属性</param>
    public async Task UpdatePartialAsync(TEntity entity, string? databaseName, params Expression<Func<TEntity, object>>[] properties)
    {
        await UpdatePartialAsync(entity, databaseName, null, CancellationToken.None, properties);
    }

    /// <summary>
    /// 部分更新实体（仅更新指定属性，指定数据库和集合）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="properties">要更新的属性</param>
    public async Task UpdatePartialAsync(TEntity entity, string? databaseName, string? collectionName, params Expression<Func<TEntity, object>>[] properties)
    {
        await UpdatePartialAsync(entity, databaseName, collectionName, CancellationToken.None, properties);
    }

    /// <summary>
    /// 从表达式中提取属性名
    /// </summary>
    /// <param name="propertyExpression">属性表达式</param>
    /// <returns>属性名</returns>
    private string ExtractPropertyName(Expression<Func<TEntity, object>> propertyExpression)
    {
        if (propertyExpression.Body is UnaryExpression unary && unary.Operand is MemberExpression member)
        {
            return member.Member.Name;
        }
        else if (propertyExpression.Body is MemberExpression memberExp)
        {
            return memberExp.Member.Name;
        }

        throw new ArgumentException("表达式必须是属性访问表达式", nameof(propertyExpression));
    }

    /// <summary>
    /// 获取实体的属性值
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="propertyName">属性名</param>
    /// <returns>属性值</returns>
    private object GetPropertyValue(TEntity entity, string propertyName)
    {
        var property = typeof(TEntity).GetProperty(propertyName);
        if (property == null)
        {
            throw new ArgumentException($"属性 {propertyName} 不存在于类型 {typeof(TEntity).Name} 中");
        }

        return property.GetValue(entity);
    }

    /// <summary>
    /// 更新实体（指定数据库和集合）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    public void Update(TEntity entity, string? databaseName, string? collectionName)
    {
        UpdateAsync(entity, databaseName, collectionName, CancellationToken.None).Wait();
    }

    /// <summary>
    /// 更新实体（指定数据库）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    public void Update(TEntity entity, string? databaseName)
    {
        UpdateAsync(entity, databaseName, null, CancellationToken.None).Wait();
    }

    /// <summary>
    /// 更新实体
    /// </summary>
    /// <param name="entity">实体对象</param>
    public void Update(TEntity entity)
    {
        UpdateAsync(entity, null, null, CancellationToken.None).Wait();
    }

    /// <summary>
    /// 更新实体（异步，指定数据库和集合）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateAsync(TEntity entity, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName, collectionName);
        var idProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperty("id") ?? throw new InvalidOperationException("实体必须包含Id或id属性");
        var idValue = (TKey)idProperty.GetValue(entity);

        var filter = Builders<TEntity>.Filter.Eq("_id", idValue);
        await collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 更新实体（异步，指定数据库）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateAsync(TEntity entity, string? databaseName, CancellationToken cancellationToken = default)
    {
        await UpdateAsync(entity, databaseName, null, cancellationToken);
    }

    /// <summary>
    /// 更新实体（异步）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await UpdateAsync(entity, null, null, cancellationToken);
    }

    /// <summary>
    /// 更新实体（直接指定主键值）
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="entity">实体对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateAsync(TKey id, TEntity entity, CancellationToken cancellationToken = default)
    {
        await UpdateAsync(id, entity, null, null, cancellationToken);
    }

    /// <summary>
    /// 更新实体（直接指定主键值，指定数据库）
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateAsync(TKey id, TEntity entity, string? databaseName, CancellationToken cancellationToken = default)
    {
        await UpdateAsync(id, entity, databaseName, null, cancellationToken);
    }

    /// <summary>
    /// 更新实体（直接指定主键值，指定数据库和集合）
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateAsync(TKey id, TEntity entity, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName, collectionName);
        var filter = Builders<TEntity>.Filter.Eq("_id", id);
        await collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 批量更新实体（指定数据库和集合）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    public void UpdateRange(IEnumerable<TEntity> entities, string? databaseName, string? collectionName)
    {
        UpdateRangeAsync(entities, databaseName, collectionName, CancellationToken.None).Wait();
    }

    /// <summary>
    /// 批量更新实体（指定数据库）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    public void UpdateRange(IEnumerable<TEntity> entities, string? databaseName)
    {
        UpdateRangeAsync(entities, databaseName, null, CancellationToken.None).Wait();
    }

    /// <summary>
    /// 批量更新实体
    /// </summary>
    /// <param name="entities">实体集合</param>
    public void UpdateRange(IEnumerable<TEntity> entities)
    {
        UpdateRangeAsync(entities, null, null, CancellationToken.None).Wait();
    }

    /// <summary>
    /// 批量更新实体（异步，指定数据库和集合）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateRangeAsync(IEnumerable<TEntity> entities, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName, collectionName);
        var idProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperty("id") ?? throw new InvalidOperationException("实体必须包含Id或id属性");

        foreach (var entity in entities)
        {
            var idValue = (TKey)idProperty.GetValue(entity);
            var filter = Builders<TEntity>.Filter.Eq("_id", idValue);
            await collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// 批量更新实体（异步，指定数据库）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateRangeAsync(IEnumerable<TEntity> entities, string? databaseName, CancellationToken cancellationToken = default)
    {
        await UpdateRangeAsync(entities, databaseName, null, cancellationToken);
    }

    /// <summary>
    /// 批量更新实体（异步）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await UpdateRangeAsync(entities, null, null, cancellationToken);
    }

    /// <summary>
    /// 删除实体（指定数据库和集合）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    public void Delete(TEntity entity, string? databaseName, string? collectionName)
    {
        DeleteAsync(entity, databaseName, collectionName, CancellationToken.None).Wait();
    }

    /// <summary>
    /// 删除实体（指定数据库）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    public void Delete(TEntity entity, string? databaseName)
    {
        DeleteAsync(entity, databaseName, null, CancellationToken.None).Wait();
    }

    /// <summary>
    /// 删除实体
    /// </summary>
    /// <param name="entity">实体对象</param>
    public void Delete(TEntity entity)
    {
        DeleteAsync(entity, null, null, CancellationToken.None).Wait();
    }

    /// <summary>
    /// 删除实体（异步，指定数据库和集合）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(TEntity entity, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName, collectionName);
        var idProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperty("id") ?? throw new InvalidOperationException("实体必须包含Id或id属性");
        var idValue = (TKey)idProperty.GetValue(entity);

        var filter = Builders<TEntity>.Filter.Eq("_id", idValue);
        await collection.DeleteOneAsync(filter, cancellationToken);
    }

    /// <summary>
    /// 删除实体（异步，指定数据库）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(TEntity entity, string? databaseName, CancellationToken cancellationToken = default)
    {
        await DeleteAsync(entity, databaseName, null, cancellationToken);
    }

    /// <summary>
    /// 删除实体（异步）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await DeleteAsync(entity, null, null, cancellationToken);
    }

    /// <summary>
    /// 根据主键删除实体（指定数据库和集合）
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteByIdAsync(TKey id, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName, collectionName);
        var filter = Builders<TEntity>.Filter.Eq("_id", id);
        await collection.DeleteOneAsync(filter, cancellationToken);
    }

    /// <summary>
    /// 根据主键删除实体（指定数据库）
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteByIdAsync(TKey id, string? databaseName, CancellationToken cancellationToken = default)
    {
        await DeleteByIdAsync(id, databaseName, null, cancellationToken);
    }

    /// <summary>
    /// 根据主键删除实体
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        await DeleteByIdAsync(id, null, null, cancellationToken);
    }

    /// <summary>
    /// 批量删除实体（指定数据库和集合）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    public void DeleteRange(IEnumerable<TEntity> entities, string? databaseName, string? collectionName)
    {
        DeleteRangeAsync(entities, databaseName, collectionName, CancellationToken.None).Wait();
    }

    /// <summary>
    /// 批量删除实体（指定数据库）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    public void DeleteRange(IEnumerable<TEntity> entities, string? databaseName)
    {
        DeleteRangeAsync(entities, databaseName, null, CancellationToken.None).Wait();
    }

    /// <summary>
    /// 批量删除实体
    /// </summary>
    /// <param name="entities">实体集合</param>
    public void DeleteRange(IEnumerable<TEntity> entities)
    {
        DeleteRangeAsync(entities, null, null, CancellationToken.None).Wait();
    }

    /// <summary>
    /// 批量删除实体（异步，指定数据库和集合）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteRangeAsync(IEnumerable<TEntity> entities, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName, collectionName);
        var idProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperty("id") ?? throw new InvalidOperationException("实体必须包含Id或id属性");
        var ids = entities.Select(e => idProperty.GetValue(e)).ToList();

        var filter = Builders<TEntity>.Filter.In("_id", ids);
        await collection.DeleteManyAsync(filter, cancellationToken);
    }

    /// <summary>
    /// 批量删除实体（异步，指定数据库）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteRangeAsync(IEnumerable<TEntity> entities, string? databaseName, CancellationToken cancellationToken = default)
    {
        await DeleteRangeAsync(entities, databaseName, null, cancellationToken);
    }

    /// <summary>
    /// 批量删除实体（异步）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await DeleteRangeAsync(entities, null, null, cancellationToken);
    }

    /// <summary>
    /// 分批删除实体（适用于大量数据，指定数据库）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="batchSize">每批大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteBatchedAsync(IEnumerable<TEntity> entities, string? databaseName, int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        await DeleteBatchedAsync(entities, databaseName, null, batchSize, cancellationToken);
    }

    /// <summary>
    /// 分批删除实体（适用于大量数据，指定数据库和集合）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="batchSize">每批大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteBatchedAsync(IEnumerable<TEntity> entities, string? databaseName, string? collectionName, int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName, collectionName);
        var idProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperty("id") ?? throw new InvalidOperationException("实体必须包含Id或id属性");
        var entityList = entities.ToList();
        
        for (int i = 0; i < entityList.Count; i += batchSize)
        {
            var batch = entityList.Skip(i).Take(batchSize).ToList();
            var ids = batch.Select(e => idProperty.GetValue(e)).ToList();
            var filter = Builders<TEntity>.Filter.In("_id", ids);
            await collection.DeleteManyAsync(filter, cancellationToken);
        }
    }

    /// <summary>
    /// 分批删除实体（适用于大量数据）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="batchSize">每批大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteBatchedAsync(IEnumerable<TEntity> entities, int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        await DeleteBatchedAsync(entities, null, null, batchSize, cancellationToken);
    }

    /// <summary>
    /// 根据条件删除实体（指定数据库和集合）
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的实体数量</returns>
    public async Task<int> DeleteByConditionAsync(Expression<Func<TEntity, bool>> predicate, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName, collectionName);
        var filter = Builders<TEntity>.Filter.Where(predicate);
        var result = await collection.DeleteManyAsync(filter, cancellationToken);
        return (int)result.DeletedCount;
    }

    /// <summary>
    /// 根据条件删除实体（指定数据库）
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的实体数量</returns>
    public async Task<int> DeleteByConditionAsync(Expression<Func<TEntity, bool>> predicate, string? databaseName, CancellationToken cancellationToken = default)
    {
        return await DeleteByConditionAsync(predicate, databaseName, null, cancellationToken);
    }

    /// <summary>
    /// 根据条件删除实体
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的实体数量</returns>
    public async Task<int> DeleteByConditionAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await DeleteByConditionAsync(predicate, null, null, cancellationToken);
    }

    /// <summary>
    /// 软删除实体
    /// </summary>
    public async Task SoftDeleteAsync(TKey id, string deletedBy = "system", CancellationToken cancellationToken = default)
    {
        // 软删除通常是更新IsDeleted字段为true，并设置DeletedBy和DeletedTime
        // 假设实体有这些字段，如果没有则需要调整
        var collection = GetCollection();
        var filter = Builders<TEntity>.Filter.Eq("_id", id);
        var update = Builders<TEntity>.Update
            .Set("IsDeleted", true)
            .Set("DeletedBy", deletedBy)
            .Set("DeletedTime", DateTime.UtcNow);
        
        await collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }
    
    /// <summary>
    /// 批量软删除实体
    /// </summary>
    public async Task SoftDeleteRangeAsync(IEnumerable<TKey> ids, string deletedBy = "system", CancellationToken cancellationToken = default)
    {
        var collection = GetCollection();
        var filter = Builders<TEntity>.Filter.In("_id", ids);
        var update = Builders<TEntity>.Update
            .Set("IsDeleted", true)
            .Set("DeletedBy", deletedBy)
            .Set("DeletedTime", DateTime.UtcNow);
        
        await collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 保存更改
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>影响的行数</returns>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // MongoDB没有SaveChanges的概念，因为它是无会话的
        return Task.FromResult(0);
    }

    /// <summary>
    /// 执行批量插入（高性能）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>插入的行数</returns>
    public async Task<int> BulkInsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        // MongoDB的InsertMany已经是批量操作
        await AddRangeAsync(entities, cancellationToken);
        return entities.Count();
    }

    /// <summary>
    /// 执行批量更新（高性能）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新的行数</returns>
    public async Task<int> BulkUpdateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        // MongoDB没有内置的批量更新，我们需要循环更新
        await UpdateRangeAsync(entities, cancellationToken);
        return entities.Count();
    }

    #endregion

    #region MongoDB特有的方法

    /// <summary>
    /// 批量更新符合条件的文档
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="update">更新定义</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新的文档数量</returns>
    public async Task<long> UpdateManyAsync(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, CancellationToken cancellationToken = default)
    {
        return await UpdateManyAsync(filter, update, null, cancellationToken);
    }

    /// <summary>
    /// 批量更新符合条件的文档（指定数据库）
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="update">更新定义</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新的文档数量</returns>
    public async Task<long> UpdateManyAsync(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, string? databaseName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName);
        var result = await collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount;
    }

    /// <summary>
    /// 批量删除符合条件的文档
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的文档数量</returns>
    public async Task<long> DeleteManyAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken = default)
    {
        return await DeleteManyAsync(filter, null, cancellationToken);
    }

    /// <summary>
    /// 批量删除符合条件的文档（指定数据库）
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的文档数量</returns>
    public async Task<long> DeleteManyAsync(FilterDefinition<TEntity> filter, string? databaseName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName);
        var result = await collection.DeleteManyAsync(filter, cancellationToken: cancellationToken);
        return result.DeletedCount;
    }

    /// <summary>
    /// 使用FilterDefinition查询文档
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合条件的文档集合</returns>
    public async Task<List<TEntity>> FindAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken = default)
    {
        return await FindAsync(filter, null, cancellationToken);
    }

    /// <summary>
    /// 使用FilterDefinition查询文档（指定数据库）
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合条件的文档集合</returns>
    public async Task<List<TEntity>> FindAsync(FilterDefinition<TEntity> filter, string? databaseName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName);
        return await collection.Find(filter).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 使用FilterDefinition查询单个文档
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合条件的单个文档</returns>
    public async Task<TEntity> FindOneAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken = default)
    {
        return await FindOneAsync(filter, null, cancellationToken);
    }

    /// <summary>
    /// 使用FilterDefinition查询单个文档（指定数据库）
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合条件的单个文档</returns>
    public async Task<TEntity> FindOneAsync(FilterDefinition<TEntity> filter, string? databaseName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName);
        var result = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return result ?? throw new InvalidOperationException("未找到符合条件的文档");
    }

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
    public async Task<PagedResult<TEntity>> GetPagedByConditionAsync(FilterDefinition<BsonDocument> filter, int pageIndex, int pageSize, Expression<Func<TEntity, object>>? orderBy = null, bool isDescending = false, string? databaseName = null, string? collectionName = null, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName, collectionName);
        
        // 将BsonDocument过滤器转换为TEntity过滤器
        var entityFilter = Builders<TEntity>.Filter.Where(_ => true);
        
        // 由于MongoDB的Find方法不直接支持BsonDocument过滤器用于泛型集合，我们需要使用BsonDocument集合进行查询
        var bsonCollection = collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);
        var totalCount = await bsonCollection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        
        // 使用BsonDocument查询，然后转换为TEntity
        var bsonQuery = bsonCollection.Find(filter);
        var bsonItems = await bsonQuery
            .Skip((pageIndex - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);
        
        // 将BsonDocument转换为TEntity
        var items = bsonItems.Select(doc => BsonSerializer.Deserialize<TEntity>(doc)).ToList();

        return PagedResult<TEntity>.SuccessResult(items, (int)totalCount, pageIndex, pageSize);
    }

    /// <summary>
    /// 批量更新符合条件的文档（指定数据库和集合）
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="update">更新定义</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新的文档数量</returns>
    public async Task<long> UpdateManyAsync(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName, collectionName);
        var result = await collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount;
    }

    /// <summary>
    /// 批量删除符合条件的文档（指定数据库和集合）
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的文档数量</returns>
    public async Task<long> DeleteManyAsync(FilterDefinition<TEntity> filter, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName, collectionName);
        var result = await collection.DeleteManyAsync(filter, cancellationToken: cancellationToken);
        return result.DeletedCount;
    }

    /// <summary>
    /// 使用FilterDefinition查询文档（指定数据库和集合）
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合条件的文档集合</returns>
    public async Task<List<TEntity>> FindAsync(FilterDefinition<TEntity> filter, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName, collectionName);
        return await collection.Find(filter).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 使用FilterDefinition查询单个文档（指定数据库和集合）
    /// </summary>
    /// <param name="filter">筛选条件</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合条件的单个文档</returns>
    public async Task<TEntity> FindOneAsync(FilterDefinition<TEntity> filter, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName, collectionName);
        var result = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return result ?? throw new InvalidOperationException("未找到符合条件的文档");
    }

    #endregion

    #region 跟踪相关方法（MongoDB不支持，仅为接口实现）

    /// <summary>
    /// 禁用实体跟踪（MongoDB不支持，仅为接口实现）
    /// </summary>
    public void DisableTracking()
    {
        // MongoDB是无会话的，不支持实体跟踪
    }

    /// <summary>
    /// 启用实体跟踪（MongoDB不支持，仅为接口实现）
    /// </summary>
    public void EnableTracking()
    {
        // MongoDB是无会话的，不支持实体跟踪
    }

    /// <summary>
    /// 清除实体跟踪缓存（MongoDB不支持，仅为接口实现）
    /// </summary>
    public void ClearTracker()
    {
        // MongoDB是无会话的，不支持实体跟踪
    }

    #endregion

    #region 只读查询方法

    /// <summary>
    /// 获取第一个符合条件的实体
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合条件的第一个实体，如果不存在则返回null</returns>
    public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection();
        return await collection.Find(predicate).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// 获取单个符合条件的实体
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合条件的单个实体，如果不存在则返回null，如果存在多个则抛出异常</returns>
    public async Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection();
        return await collection.Find(predicate).SingleOrDefaultAsync(cancellationToken);
    }

    #endregion

    #region 事务相关方法

    /// <summary>
    /// 执行事务
    /// </summary>
    /// <param name="action">事务内执行的操作</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        // MongoDB的事务需要会话支持，这里简化实现
        await action();
    }

    /// <summary>
    /// 执行事务并返回结果
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="action">事务内执行的操作</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken = default)
    {
        // MongoDB的事务需要会话支持，这里简化实现
        return await action();
    }

    #endregion
}
