#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FakeMicro.DatabaseAccess.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using SqlSugar;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// MongoDB仓储实现
/// 基于SqlSugar.MongoDbCore实现通用仓储接口和MongoDB特定接口
/// 遵循Orleans最佳实践
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public class MongoRepository<TEntity, TKey> : IMongoRepository<TEntity, TKey> where TEntity : class
{
    private readonly ISqlSugarClient _db;
    private readonly ILogger<MongoRepository<TEntity, TKey>> _logger;
    private readonly string? _defaultDatabaseName;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="db">SqlSugar客户端（配置为MongoDB）</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="defaultDatabaseName">默认数据库名称，未提供时使用连接字符串中的数据库名称</param>
    public MongoRepository(ISqlSugarClient db, ILogger<MongoRepository<TEntity, TKey>> logger, string? defaultDatabaseName = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _defaultDatabaseName = defaultDatabaseName??"FakeMicroDB";
        
        // 确保SqlSugar配置为MongoDB
        if (_db.CurrentConnectionConfig.DbType != DbType.MongoDb)
        {
            throw new InvalidOperationException("SqlSugar客户端必须配置为MongoDB");
        }
    }

    /// <summary>
    /// 获取MongoDB集合
    /// </summary>
    /// <param name="databaseName">数据库名称，未提供时使用默认数据库名称</param>
    /// <param name="collectionName">集合名称，未提供时使用实体类名</param>
    /// <returns>MongoDB集合</returns>
    public IMongoCollection<TEntity> GetCollection(string? databaseName = null, string? collectionName = null)
    {
        // SqlSugar.MongoDbCore不直接提供GetCollection方法
        // 我们需要使用MongoDB.Driver直接获取集合
        var connectionString = _db.CurrentConnectionConfig.ConnectionString;
        var mongoClient = new MongoClient(connectionString);
        
        // 优先级：方法参数 > 类构造函数参数 > 连接字符串中的数据库名称
        var dbName = databaseName ?? _defaultDatabaseName ?? _db.CurrentConnectionConfig.ConnectionString.Split('/').Last().Split('?').First();
        var database = mongoClient.GetDatabase(dbName);
        // 优先级：方法参数 > 实体类名
        var collName = collectionName ?? typeof(TEntity).Name;
        return database.GetCollection<TEntity>(collName);
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
        /// 获取所有实体（带导航属性，指定数据库和集合）
        /// </summary>
        /// <param name="databaseName">数据库名称</param>
        /// <param name="collectionName">集合名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="includes">要包含的导航属性（MongoDB忽略）</param>
        /// <returns>实体集合</returns>
        public async Task<IEnumerable<TEntity>> GetAllWithIncludesAsync(string? databaseName, string? collectionName,
            CancellationToken cancellationToken = default,
            params Expression<Func<TEntity, object>>[] includes)
        {
            // MongoDB是文档数据库，导航属性通常嵌入在文档中，因此忽略includes参数
            var collection = GetCollection(databaseName, collectionName);
            return await collection.Find(_ => true).ToListAsync(cancellationToken);
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
        public async Task<PagedResult<TEntity>> GetPagedAsync(int pageIndex, int pageSize,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool isDescending = false,
            string? databaseName = null,
            string? collectionName = null,
            CancellationToken cancellationToken = default)
        {
            var collection = GetCollection(databaseName, collectionName);
            var totalCount = await collection.CountDocumentsAsync(FilterDefinition<TEntity>.Empty, cancellationToken: cancellationToken);
            var query = collection.Find(_ => true);
            
            // 应用排序
            if (orderBy != null)
            {
                query = isDescending 
                    ? query.SortByDescending(orderBy)
                    : query.SortBy(orderBy);
            }
            
            var items = await query
                .Skip((pageIndex - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<TEntity>
            {
                Items = items,
                TotalCount = (int)totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// 根据主键获取实体（带导航属性，指定数据库和集合）
        /// </summary>
        /// <param name="id">主键值</param>
        /// <param name="databaseName">数据库名称</param>
        /// <param name="collectionName">集合名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="includes">要包含的导航属性（MongoDB忽略）</param>
        /// <returns>实体对象，如果不存在则返回null</returns>
        public async Task<TEntity?> GetByIdWithIncludesAsync(TKey id, string? databaseName, string? collectionName,
            CancellationToken cancellationToken = default,
            params Expression<Func<TEntity, object>>[] includes)
        {
            // MongoDB是文档数据库，导航属性通常嵌入在文档中，因此忽略includes参数
            return await GetByIdAsync(id, databaseName, collectionName, cancellationToken);
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
        public async Task<PagedResult<TEntity>> GetPagedByConditionAsync(Expression<Func<TEntity, bool>> predicate,
            int pageIndex, int pageSize,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool isDescending = false,
            string? databaseName = null,
            string? collectionName = null,
            CancellationToken cancellationToken = default)
        {
            var collection = GetCollection(databaseName, collectionName);
            var totalCount = await collection.CountDocumentsAsync(predicate, cancellationToken: cancellationToken);
            var query = collection.Find(predicate);
            
            // 应用排序
            if (orderBy != null)
            {
                query = isDescending
                    ? query.SortByDescending(orderBy)
                    : query.SortBy(orderBy);
            }
            
            var items = await query
                .Skip((pageIndex - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync(cancellationToken);
            
            return new PagedResult<TEntity>
            {
                Items = items,
                TotalCount = (int)totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// 根据主键获取实体（指定数据库和集合）
        /// </summary>
        /// <param name="id">主键值</param>
        /// <param name="databaseName">数据库名称</param>
        /// <param name="collectionName">集合名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>实体对象</returns>
        public async Task<TEntity> GetByIdAsync(TKey id, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
        {
            var collection = GetCollection(databaseName, collectionName);
            return await collection.Find(Builders<TEntity>.Filter.Eq("_id", id)).FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// 根据主键获取实体（指定数据库和集合）
        /// </summary>
        /// <param name="id">主键值</param>
        /// <param name="includes">包含的导航属性</param>
        /// <param name="databaseName">数据库名称</param>
        /// <param name="collectionName">集合名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>实体对象</returns>
        public async Task<TEntity> GetByIdWithIncludesAsync(TKey id, List<Expression<Func<TEntity, object>>> includes, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
        {
            var collection = GetCollection(databaseName, collectionName);
            return await collection.Find(Builders<TEntity>.Filter.Eq("_id", id)).FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// 根据条件获取实体（指定数据库和集合）
        /// </summary>
        /// <param name="predicate">查询条件</param>
        /// <param name="databaseName">数据库名称</param>
        /// <param name="collectionName">集合名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>实体集合</returns>
        public async Task<IEnumerable<TEntity>> GetByConditionAsync(Expression<Func<TEntity, bool>> predicate, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
        {
            var collection = GetCollection(databaseName, collectionName);
            return await collection.Find(predicate).ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 根据条件分页获取实体（指定数据库和集合）
        /// </summary>
        /// <param name="predicate">查询条件</param>
        /// <param name="pageIndex">页码（从1开始）</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="databaseName">数据库名称</param>
        /// <param name="collectionName">集合名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>分页结果</returns>
        public async Task<PagedResult<TEntity>> GetPagedByConditionAsync(Expression<Func<TEntity, bool>> predicate, int pageIndex, int pageSize, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
        {
            var collection = GetCollection(databaseName, collectionName);
            var totalCount = await collection.CountDocumentsAsync(predicate, cancellationToken: cancellationToken);
            var items = await collection.Find(predicate)
                .Skip((pageIndex - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<TEntity>
            {
                Items = items,
                TotalCount = (int)totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
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
        /// 获取实体数量（指定数据库和集合）
        /// </summary>
        /// <param name="predicate">查询条件</param>
        /// <param name="databaseName">数据库名称</param>
        /// <param name="collectionName">集合名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>实体数量</returns>
        public async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
        {
            var collection = GetCollection(databaseName, collectionName);
            return (int)await collection.CountDocumentsAsync(predicate, cancellationToken: cancellationToken);
        }
        #endregion

        #region 增删改方法（支持collectionName）

        /// <summary>
        /// 添加实体（指定数据库和集合）
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="databaseName">数据库名称</param>
        /// <param name="collectionName">集合名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>添加的实体</returns>
        public async Task AddAsync(TEntity entity, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
        {
            var collection = GetCollection(databaseName, collectionName);
            await collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
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
            var batchList = entities.ToList();
            for (int i = 0; i < batchList.Count; i += batchSize)
            {
                var batch = batchList.Skip(i).Take(batchSize);
                await collection.InsertManyAsync(batch, cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// 更新实体（指定数据库和集合）
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="databaseName">数据库名称</param>
        /// <param name="collectionName">集合名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新的实体</returns>
        public async Task UpdateAsync(TEntity entity, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
        {
            var collection = GetCollection(databaseName, collectionName);
            var idProperty = typeof(TEntity).GetProperty("Id");
            if (idProperty == null)
            {
                throw new InvalidOperationException("实体必须包含Id属性");
            }
            
            var idValue = (TKey)idProperty.GetValue(entity);
            var filter = Builders<TEntity>.Filter.Eq("_id", ObjectId.Parse(idValue.ToString()));
            
            await collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 部分更新实体（指定数据库和集合）
        /// </summary>
        /// <param name="id">主键值</param>
        /// <param name="updateDefinition">更新定义</param>
        /// <param name="databaseName">数据库名称</param>
        /// <param name="collectionName">集合名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task UpdatePartialAsync(TKey id, UpdateDefinition<TEntity> updateDefinition, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
        {
            var collection = GetCollection(databaseName, collectionName);
            var filter = Builders<TEntity>.Filter.Eq("_id", id);
            await collection.UpdateOneAsync(filter, updateDefinition, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 部分更新实体（指定数据库和集合）
        /// </summary>
        /// <param name="predicate">更新条件</param>
        /// <param name="updateDefinition">更新定义</param>
        /// <param name="databaseName">数据库名称</param>
        /// <param name="collectionName">集合名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task UpdatePartialAsync(Expression<Func<TEntity, bool>> predicate, UpdateDefinition<TEntity> updateDefinition, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
        {
            var collection = GetCollection(databaseName, collectionName);
            await collection.UpdateManyAsync(predicate, updateDefinition, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 批量更新实体（指定数据库和集合）
        /// </summary>
        /// <param name="entities">实体集合</param>
        /// <param name="databaseName">数据库名称</param>
        /// <param name="collectionName">集合名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task UpdateRangeAsync(IEnumerable<TEntity> entities, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
        {
            var collection = GetCollection(databaseName, collectionName);
            var idProperty = typeof(TEntity).GetProperty("Id");
            if (idProperty == null)
            {
                throw new InvalidOperationException("实体必须包含Id属性");
            }

            foreach (var entity in entities)
            {
                var idValue = (TKey)idProperty.GetValue(entity);
                var filter = Builders<TEntity>.Filter.Eq("_id",ObjectId.Parse(idValue.ToString()));
                await collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// 删除实体（指定数据库和集合）
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="databaseName">数据库名称</param>
        /// <param name="collectionName">集合名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task DeleteAsync(TEntity entity, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
        {
            var collection = GetCollection(databaseName, collectionName);
            var idProperty = typeof(TEntity).GetProperty("Id");
            if (idProperty == null)
            {
                throw new InvalidOperationException("实体必须包含Id属性");
            }

            var idValue = (TKey)idProperty.GetValue(entity);
            var filter = Builders<TEntity>.Filter.Eq("_id", ObjectId.Parse(idValue.ToString()));
            await collection.DeleteOneAsync(filter, cancellationToken: cancellationToken);
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
            await collection.DeleteOneAsync(filter, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 批量删除实体（指定数据库和集合）
        /// </summary>
        /// <param name="entities">实体集合</param>
        /// <param name="databaseName">数据库名称</param>
        /// <param name="collectionName">集合名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task DeleteRangeAsync(IEnumerable<TEntity> entities, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
        {
            var collection = GetCollection(databaseName, collectionName);
            var idProperty = typeof(TEntity).GetProperty("Id");
            if (idProperty == null)
            {
                throw new InvalidOperationException("实体必须包含Id属性");
            }

            foreach (var entity in entities)
            {
                var idValue = (TKey)idProperty.GetValue(entity);
                var filter = Builders<TEntity>.Filter.Eq("_id", idValue);
                await collection.DeleteOneAsync(filter, cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// 分批删除实体（适用于大量数据，指定数据库和集合）
        /// </summary>
        /// <param name="entities">实体集合</param>
        /// <param name="databaseName">数据库名称</param>
        /// <param name="collectionName">集合名称</param>
        /// <param name="batchSize">每批大小</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task DeleteBatchedAsync(IEnumerable<TEntity> entities, string? databaseName, string? collectionName, int batchSize = 1000, CancellationToken cancellationToken = default)
        {
            var collection = GetCollection(databaseName, collectionName);
            var idProperty = typeof(TEntity).GetProperty("Id");
            if (idProperty == null)
            {
                throw new InvalidOperationException("实体必须包含Id属性");
            }

            var batchList = entities.ToList();
            for (int i = 0; i < batchList.Count; i += batchSize)
            {
                var batch = batchList.Skip(i).Take(batchSize);
                foreach (var entity in batch)
                {
                    var idValue = (TKey)idProperty.GetValue(entity);
                    var filter = Builders<TEntity>.Filter.Eq("_id", idValue);
                    await collection.DeleteOneAsync(filter, cancellationToken: cancellationToken);
                }
            }
        }

        /// <summary>
        /// 根据条件删除实体（指定数据库和集合）
        /// </summary>
        /// <param name="predicate">删除条件</param>
        /// <param name="databaseName">数据库名称</param>
        /// <param name="collectionName">集合名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>删除的实体数量</returns>
        public async Task<int> DeleteByConditionAsync(Expression<Func<TEntity, bool>> predicate, string? databaseName, string? collectionName, CancellationToken cancellationToken = default)
        {
            var collection = GetCollection(databaseName, collectionName);
            var result = await collection.DeleteManyAsync(predicate, cancellationToken: cancellationToken);
            return (int)result.DeletedCount;
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

        #region IRepository实现

    /// <summary>
    /// 获取所有实体
    /// </summary>
    public async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var collection = GetCollection();
        return await collection.Find(Builders<TEntity>.Filter.Empty).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取所有实体（带导航属性）
    /// MongoDB不支持导航属性，直接返回所有实体
    /// </summary>
    public async Task<IEnumerable<TEntity>> GetAllWithIncludesAsync(
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includes)
    {
        // MongoDB不支持导航属性，忽略includes参数
        return await GetAllAsync(cancellationToken);
    }

    /// <summary>
    /// 获取分页实体
    /// </summary>
    public async Task<PagedResult<TEntity>> GetPagedAsync(int pageNumber, int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool isDescending = false,
        CancellationToken cancellationToken = default)
    {
        return await GetPagedByConditionAsync(it => true, pageNumber, pageSize, orderBy, isDescending, cancellationToken);
    }

    /// <summary>
    /// 根据主键获取实体
    /// </summary>
    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection();
        var filter = Builders<TEntity>.Filter.Eq("Id", id);
        return await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// 根据主键获取实体（带导航属性）
    /// MongoDB不支持导航属性，直接根据主键获取
    /// </summary>
    public async Task<TEntity?> GetByIdWithIncludesAsync(TKey id,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includes)
    {
        // MongoDB不支持导航属性，忽略includes参数
        return await GetByIdAsync(id, cancellationToken);
    }

    /// <summary>
    /// 根据条件获取实体
    /// </summary>
    public async Task<IEnumerable<TEntity>> GetByConditionAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var collection = GetCollection();
        var filter = Builders<TEntity>.Filter.Where(predicate);
        return await collection.Find(filter).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据条件获取分页实体
    /// </summary>
    public async Task<PagedResult<TEntity>> GetPagedByConditionAsync(Expression<Func<TEntity, bool>> predicate,
        int pageNumber, int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool isDescending = false,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 1000) pageSize = 1000;

        var collection = GetCollection();
        var filter = Builders<TEntity>.Filter.Where(predicate);
        var totalCount = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        IFindFluent<TEntity, TEntity> findFluent = collection.Find(filter);

        // 排序
        if (orderBy != null)
        {
            findFluent = isDescending
                ? findFluent.SortByDescending(orderBy)
                : findFluent.SortBy(orderBy);
        }
        else
        {
            // 默认按Id排序
            findFluent = findFluent.SortBy(x => x.GetType().GetProperty("Id").GetValue(x));
        }

        // 分页
        var items = await findFluent
            .Skip((pageNumber - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TEntity>
        {
            Items = items,
            TotalCount = (int)totalCount,
            PageIndex = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    /// <summary>
    /// 检查实体是否存在
    /// </summary>
    public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var collection = GetCollection();
        var filter = Builders<TEntity>.Filter.Where(predicate);
        return await collection.Find(filter).AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 获取实体数量
    /// </summary>
    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var collection = GetCollection();
        var filter = predicate != null ? Builders<TEntity>.Filter.Where(predicate) : Builders<TEntity>.Filter.Empty;
        return (int)await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 添加实体
    /// </summary>
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection();
        await collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 批量添加实体
    /// </summary>
    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection();
        await collection.InsertManyAsync(entities, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 分批添加实体（适用于大量数据）
    /// </summary>
    public async Task AddBatchedAsync(IEnumerable<TEntity> entities, int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        var collection = GetCollection();
        var batches = entities.Chunk(batchSize);
        foreach (var batch in batches)
        {
            await collection.InsertManyAsync(batch, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// 更新实体
    /// </summary>
    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection();
        var idProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperty("id") ?? throw new InvalidOperationException("实体必须包含Id或id属性");
        var idValue = idProperty.GetValue(entity);
        
        var filter = Builders<TEntity>.Filter.Eq(idProperty.Name, idValue);
        await collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 部分更新实体（仅更新指定属性）
    /// </summary>
    public async Task UpdatePartialAsync(TEntity entity, params Expression<Func<TEntity, object>>[] properties)
    {
        await UpdatePartialAsync(entity, CancellationToken.None, properties);
    }

    /// <summary>
    /// 部分更新实体（仅更新指定属性，带取消令牌）
    /// </summary>
    public async Task UpdatePartialAsync(TEntity entity, CancellationToken cancellationToken, params Expression<Func<TEntity, object>>[] properties)
    {
        // 使用MongoDB.Driver直接更新，因为SqlSugar.MongoDbCore的SetColumns方法不支持这种用法
        var collection = GetCollection();
        var idProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperty("id") ?? throw new InvalidOperationException("实体必须包含Id或id属性");
        var idValue = idProperty.GetValue(entity);
        
        var filter = Builders<TEntity>.Filter.Eq(idProperty.Name, idValue);
        var update = Builders<TEntity>.Update.Combine();
        
        // 构建更新定义
        foreach (var property in properties)
        {
            var memberExpression = property.Body as MemberExpression;
            if (memberExpression != null)
            {
                var propertyName = memberExpression.Member.Name;
                var propertyInfo = memberExpression.Member as System.Reflection.PropertyInfo;
                if (propertyInfo != null)
                {
                    var value = propertyInfo.GetValue(entity);
                    update = update.Set(propertyName, value);
                }
            }
        }
        
        await collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 批量更新实体
    /// </summary>
    public async Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection();
        foreach (var entity in entities)
        {
            var idProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperty("id") ?? throw new InvalidOperationException("实体必须包含Id或id属性");
            var idValue = idProperty.GetValue(entity);
            
            var filter = Builders<TEntity>.Filter.Eq(idProperty.Name, idValue);
            await collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// 删除实体
    /// </summary>
    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection();
        var idProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperty("id") ?? throw new InvalidOperationException("实体必须包含Id或id属性");
        var idValue = idProperty.GetValue(entity);
        
        var filter = Builders<TEntity>.Filter.Eq(idProperty.Name, idValue);
        await collection.DeleteOneAsync(filter, cancellationToken);
    }

    /// <summary>
    /// 根据主键删除实体
    /// </summary>
    public async Task DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection();
        var filter = Builders<TEntity>.Filter.Eq("Id", id);
        await collection.DeleteOneAsync(filter, cancellationToken);
    }

    /// <summary>
    /// 批量删除实体
    /// </summary>
    public async Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection();
        var idProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperty("id") ?? throw new InvalidOperationException("实体必须包含Id或id属性");
        var ids = entities.Select(e => idProperty.GetValue(e)).ToList();
        
        var filter = Builders<TEntity>.Filter.In(idProperty.Name, ids);
        await collection.DeleteManyAsync(filter, cancellationToken);
    }

    /// <summary>
    /// 分批删除实体（适用于大量数据）
    /// </summary>
    public async Task DeleteBatchedAsync(IEnumerable<TEntity> entities, int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        var collection = GetCollection();
        var idProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperty("id") ?? throw new InvalidOperationException("实体必须包含Id或id属性");
        
        var batches = entities.Chunk(batchSize);
        foreach (var batch in batches)
        {
            var ids = batch.Select(e => idProperty.GetValue(e)).ToList();
            var filter = Builders<TEntity>.Filter.In(idProperty.Name, ids);
            await collection.DeleteManyAsync(filter, cancellationToken);
        }
    }

    /// <summary>
    /// 根据条件删除实体
    /// </summary>
    public async Task<int> DeleteByConditionAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var collection = GetCollection();
        var filter = Builders<TEntity>.Filter.Where(predicate);
        var result = await collection.DeleteManyAsync(filter, cancellationToken);
        return (int)result.DeletedCount;
    }

    /// <summary>
    /// 保存更改
    /// MongoDB是立即持久化的，此方法仅返回1
    /// </summary>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(1);
    }

    /// <summary>
    /// 执行事务
    /// MongoDB 4.0+支持事务，但需要在复制集环境中
    /// </summary>
    public async Task ExecuteInTransactionAsync(Func<Task> action,
        CancellationToken cancellationToken = default)
    {
        // 使用MongoDB.Driver直接执行事务
        var collection = GetCollection();
        var mongoClient = collection.Database.Client;
        
        // 使用MongoDB事务API
        using (var session = await mongoClient.StartSessionAsync(cancellationToken: cancellationToken))
        {
            session.StartTransaction();
            
            try
            {
                // 执行事务操作
                await action();
                
                // 提交事务
                await session.CommitTransactionAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // 回滚事务
                await session.AbortTransactionAsync(cancellationToken);
                throw;
            }
        }
    }

    /// <summary>
    /// 执行事务并返回结果
    /// </summary>
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        // 使用MongoDB.Driver直接执行事务
        var collection = GetCollection();
        var mongoClient = collection.Database.Client;
        
        // 使用MongoDB事务API
        using (var session = await mongoClient.StartSessionAsync(cancellationToken: cancellationToken))
        {
            session.StartTransaction();
            TResult result;
            
            try
            {
                // 执行事务操作并获取结果
                result = await action();
                
                // 提交事务
                await session.CommitTransactionAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // 回滚事务
                await session.AbortTransactionAsync(cancellationToken);
                throw;
            }
            
            return result;
        }
    }

    /// <summary>
    /// 禁用实体跟踪
    /// MongoDB.Driver不使用实体跟踪，此方法为空实现
    /// </summary>
    public void DisableTracking()
    {
        // MongoDB.Driver不使用实体跟踪，无需实现
    }

    /// <summary>
    /// 启用实体跟踪
    /// MongoDB.Driver不使用实体跟踪，此方法为空实现
    /// </summary>
    public void EnableTracking()
    {
        // MongoDB.Driver不使用实体跟踪，无需实现
    }

    /// <summary>
    /// 清除实体跟踪缓存
    /// MongoDB.Driver不使用实体跟踪，此方法为空实现
    /// </summary>
    public void ClearTracker()
    {
        // MongoDB.Driver不使用实体跟踪，无需实现
    }

    #endregion

    #region IMongoRepository实现

    /// <summary>
    /// 获取所有实体（指定数据库）
    /// </summary>
    public async Task<IEnumerable<TEntity>> GetAllAsync(string? databaseName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName);
        return await collection.Find(Builders<TEntity>.Filter.Empty).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取所有实体（带导航属性，指定数据库）
    /// MongoDB不支持导航属性，直接返回所有实体
    /// </summary>
    public async Task<IEnumerable<TEntity>> GetAllWithIncludesAsync(string? databaseName,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includes)
    {
        // MongoDB不支持导航属性，忽略includes参数
        return await GetAllAsync(databaseName, cancellationToken);
    }

    /// <summary>
    /// 获取分页实体（指定数据库）
    /// </summary>
    public async Task<PagedResult<TEntity>> GetPagedAsync(int pageNumber, int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool isDescending = false,
        string? databaseName = null,
        CancellationToken cancellationToken = default)
    {
        return await GetPagedByConditionAsync(it => true, pageNumber, pageSize, orderBy, isDescending, databaseName, cancellationToken);
    }

    /// <summary>
    /// 根据主键获取实体（指定数据库）
    /// </summary>
    public async Task<TEntity?> GetByIdAsync(TKey id, string? databaseName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName);
        // 使用字符串直接比较，避免使用ObjectId类型
        var filter = Builders<TEntity>.Filter.Eq("_id", id.ToString());
        return await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// 根据主键获取实体（带导航属性，指定数据库）
    /// MongoDB不支持导航属性，直接根据主键获取
    /// </summary>
    public async Task<TEntity?> GetByIdWithIncludesAsync(TKey id, string? databaseName,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includes)
    {
        // MongoDB不支持导航属性，忽略includes参数
        return await GetByIdAsync(id, databaseName, cancellationToken);
    }

    /// <summary>
    /// 根据条件获取实体（指定数据库）
    /// </summary>
    public async Task<IEnumerable<TEntity>> GetByConditionAsync(Expression<Func<TEntity, bool>> predicate, string? databaseName,
        CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName);
        var filter = Builders<TEntity>.Filter.Where(predicate);
        return await collection.Find(filter).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 根据条件获取分页实体（指定数据库）
    /// </summary>
    public async Task<PagedResult<TEntity>> GetPagedByConditionAsync(Expression<Func<TEntity, bool>> predicate,
        int pageNumber, int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool isDescending = false,
        string? databaseName = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 1000) pageSize = 1000;

        var collection = GetCollection(databaseName);
        var filter = Builders<TEntity>.Filter.Where(predicate);
        var totalCount = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        IFindFluent<TEntity, TEntity> findFluent = collection.Find(filter);

        // 排序
        if (orderBy != null)
        {
            findFluent = isDescending
                ? findFluent.SortByDescending(orderBy)
                : findFluent.SortBy(orderBy);
        }
        else
        {
            // 默认按Id排序
            findFluent = findFluent.SortBy(x => x.GetType().GetProperty("Id").GetValue(x));
        }

        // 分页
        var items = await findFluent
            .Skip((pageNumber - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TEntity>
        {
            Items = items,
            TotalCount = (int)totalCount,
            PageIndex = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    /// <summary>
    /// 检查实体是否存在（指定数据库）
    /// </summary>
    public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, string? databaseName,
        CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName);
        var filter = Builders<TEntity>.Filter.Where(predicate);
        return await collection.Find(filter).AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 获取实体数量（指定数据库）
    /// </summary>
    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate, string? databaseName,
        CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName);
        var filter = predicate != null ? Builders<TEntity>.Filter.Where(predicate) : Builders<TEntity>.Filter.Empty;
        return (int)await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 添加实体（指定数据库）
    /// </summary>
    public async Task AddAsync(TEntity entity, string? databaseName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName);
        await collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 批量添加实体（指定数据库）
    /// </summary>
    public async Task AddRangeAsync(IEnumerable<TEntity> entities, string? databaseName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName);
        await collection.InsertManyAsync(entities, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 分批添加实体（适用于大量数据，指定数据库）
    /// </summary>
    public async Task AddBatchedAsync(IEnumerable<TEntity> entities, string? databaseName,
        int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName);
        var batches = entities.Chunk(batchSize);
        foreach (var batch in batches)
        {
            await collection.InsertManyAsync(batch, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// 更新实体（指定数据库）
    /// </summary>
    public async Task UpdateAsync(TEntity entity, string? databaseName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName);
        var idProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperty("id") ?? throw new InvalidOperationException("实体必须包含Id或id属性");
        var idValue = idProperty.GetValue(entity);
        
        var filter = Builders<TEntity>.Filter.Eq(idProperty.Name, idValue);
        await collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 部分更新实体（仅更新指定属性，指定数据库）
    /// </summary>
    public async Task UpdatePartialAsync(TEntity entity, string? databaseName, params Expression<Func<TEntity, object>>[] properties)
    {
        await UpdatePartialAsync(entity, databaseName, CancellationToken.None, properties);
    }

    /// <summary>
    /// 部分更新实体（仅更新指定属性，带取消令牌，指定数据库）
    /// </summary>
    public async Task UpdatePartialAsync(TEntity entity, string? databaseName, CancellationToken cancellationToken, params Expression<Func<TEntity, object>>[] properties)
    {
        // 使用MongoDB.Driver直接更新，因为SqlSugar.MongoDbCore的SetColumns方法不支持这种用法
        var collection = GetCollection(databaseName);
        var idProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperty("id") ?? throw new InvalidOperationException("实体必须包含Id或id属性");
        var idValue = idProperty.GetValue(entity);
        
        var filter = Builders<TEntity>.Filter.Eq(idProperty.Name, idValue);
        var update = Builders<TEntity>.Update.Combine();
        
        // 构建更新定义
        foreach (var property in properties)
        {
            var memberExpression = property.Body as MemberExpression;
            if (memberExpression != null)
            {
                var propertyName = memberExpression.Member.Name;
                var propertyInfo = memberExpression.Member as System.Reflection.PropertyInfo;
                if (propertyInfo != null)
                {
                    var value = propertyInfo.GetValue(entity);
                    update = update.Set(propertyName, value);
                }
            }
        }
        
        await collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 批量更新实体（指定数据库）
    /// </summary>
    public async Task UpdateRangeAsync(IEnumerable<TEntity> entities, string? databaseName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName);
        foreach (var entity in entities)
        {
            var idProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperty("id") ?? throw new InvalidOperationException("实体必须包含Id或id属性");
            var idValue = idProperty.GetValue(entity);
            
            var filter = Builders<TEntity>.Filter.Eq(idProperty.Name, idValue);
            await collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// 删除实体（指定数据库）
    /// </summary>
    public async Task DeleteAsync(TEntity entity, string? databaseName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName);
        var idProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperty("id") ?? throw new InvalidOperationException("实体必须包含Id或id属性");
        var idValue = idProperty.GetValue(entity);
        
        var filter = Builders<TEntity>.Filter.Eq(idProperty.Name, idValue);
        await collection.DeleteOneAsync(filter, cancellationToken);
    }

    /// <summary>
    /// 根据主键删除实体（指定数据库）
    /// </summary>
    public async Task DeleteByIdAsync(TKey id, string? databaseName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName);
        var filter = Builders<TEntity>.Filter.Eq("Id", id);
        await collection.DeleteOneAsync(filter, cancellationToken);
    }

    /// <summary>
    /// 批量删除实体（指定数据库）
    /// </summary>
    public async Task DeleteRangeAsync(IEnumerable<TEntity> entities, string? databaseName, CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName);
        var idProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperty("id") ?? throw new InvalidOperationException("实体必须包含Id或id属性");
        var ids = entities.Select(e => idProperty.GetValue(e)).ToList();
        
        var filter = Builders<TEntity>.Filter.In(idProperty.Name, ids);
        await collection.DeleteManyAsync(filter, cancellationToken);
    }

    /// <summary>
    /// 分批删除实体（适用于大量数据，指定数据库）
    /// </summary>
    public async Task DeleteBatchedAsync(IEnumerable<TEntity> entities, string? databaseName,
        int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName);
        var idProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperty("id") ?? throw new InvalidOperationException("实体必须包含Id或id属性");
        
        var batches = entities.Chunk(batchSize);
        foreach (var batch in batches)
        {
            var ids = batch.Select(e => idProperty.GetValue(e)).ToList();
            var filter = Builders<TEntity>.Filter.In(idProperty.Name, ids);
            await collection.DeleteManyAsync(filter, cancellationToken);
        }
    }

    /// <summary>
    /// 根据条件删除实体（指定数据库）
    /// </summary>
    public async Task<int> DeleteByConditionAsync(Expression<Func<TEntity, bool>> predicate, string? databaseName,
        CancellationToken cancellationToken = default)
    {
        var collection = GetCollection(databaseName);
        var filter = Builders<TEntity>.Filter.Where(predicate);
        var result = await collection.DeleteManyAsync(filter, cancellationToken);
        return (int)result.DeletedCount;
    }


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
    /// 部分更新实体（指定数据库和集合，通过要更新的属性表达式）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="properties">要更新的属性表达式</param>
    public async Task UpdatePartialAsync(TEntity entity, string? databaseName, string? collectionName, params Expression<Func<TEntity, object>>[] properties)
    {
        await UpdatePartialAsync(entity, databaseName, collectionName, default, properties);
    }

    /// <summary>
    /// 部分更新实体（指定数据库和集合，通过要更新的属性表达式）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="databaseName">数据库名称</param>
    /// <param name="collectionName">集合名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="properties">要更新的属性表达式</param>
    public async Task UpdatePartialAsync(TEntity entity, string? databaseName, string? collectionName,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] properties)
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
    #endregion
}