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

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// MongoDB仓储实现
/// 基于MongoDB.Driver实现通用仓储接口和MongoDB特定接口
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public class MongoRepository<TEntity, TKey> : IMongoRepository<TEntity, TKey> where TEntity : class
{
    private readonly IMongoCollection<TEntity> _collection;
    private readonly ILogger<MongoRepository<TEntity, TKey>> _logger;
    private readonly string _collectionName;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="database">MongoDB数据库</param>
    /// <param name="logger">日志记录器</param>
    public MongoRepository(IMongoDatabase database, ILogger<MongoRepository<TEntity, TKey>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _collectionName = typeof(TEntity).Name.ToLower() + "s"; // 默认集合名称为实体名称复数形式
        _collection = database.GetCollection<TEntity>(_collectionName);
    }

    /// <summary>
    /// 获取MongoDB集合
    /// </summary>
    /// <returns>MongoDB集合</returns>
    public IMongoCollection<TEntity> GetCollection()
    {
        return _collection;
    }

    #region IRepository实现

    /// <summary>
    /// 获取所有实体
    /// </summary>
    public async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _collection.Find(Builders<TEntity>.Filter.Empty).ToListAsync(cancellationToken);
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
        var filter = Builders<TEntity>.Filter.Empty;
        return await GetPagedByConditionAsync(filter, pageNumber, pageSize, orderBy, isDescending, cancellationToken);
    }

    /// <summary>
    /// 根据主键获取实体
    /// </summary>
    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        // 尝试获取Id属性（大写I），如果不存在则尝试id属性（小写i）
        var idProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperty("id") ?? throw new InvalidOperationException("实体必须包含Id或id属性");
        var filter = Builders<TEntity>.Filter.Eq(idProperty.Name, id);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
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
        var filter = Builders<TEntity>.Filter.Where(predicate);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
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
        var filter = Builders<TEntity>.Filter.Where(predicate);
        return await GetPagedByConditionAsync(filter, pageNumber, pageSize, orderBy, isDescending, cancellationToken);
    }

    /// <summary>
    /// 检查实体是否存在
    /// </summary>
    public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<TEntity>.Filter.Where(predicate);
        return await _collection.Find(filter).AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 获取实体数量
    /// </summary>
    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var filter = predicate != null ? Builders<TEntity>.Filter.Where(predicate) : Builders<TEntity>.Filter.Empty;
        return (int)await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 添加实体
    /// </summary>
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 批量添加实体
    /// </summary>
    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        await _collection.InsertManyAsync(entities, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 分批添加实体（适用于大量数据）
    /// </summary>
    public async Task AddBatchedAsync(IEnumerable<TEntity> entities, int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        var batches = entities.Chunk(batchSize);
        foreach (var batch in batches)
        {
            await _collection.InsertManyAsync(batch, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// 更新实体
    /// </summary>
    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        // 获取实体的Id属性值（支持大写Id和小写id）
        var idProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperty("id") ?? throw new InvalidOperationException("实体必须包含Id或id属性");
        var idValue = idProperty.GetValue(entity);
        
        var filter = Builders<TEntity>.Filter.Eq(idProperty.Name, idValue);
        await _collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
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
        // 获取实体的Id属性值（支持大写Id和小写id）
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
                var propertyValue = memberExpression.Member as System.Reflection.PropertyInfo;
                if (propertyValue != null)
                {
                    var value = propertyValue.GetValue(entity);
                    update = update.Set(propertyName, value);
                }
            }
        }
        
        await _collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 批量更新实体
    /// </summary>
    public async Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
        {
            await UpdateAsync(entity, cancellationToken);
        }
    }

    /// <summary>
    /// 删除实体
    /// </summary>
    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        // 获取实体的Id属性值（支持大写Id和小写id）
        var idProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperty("id") ?? throw new InvalidOperationException("实体必须包含Id或id属性");
        var idValue = idProperty.GetValue(entity);
        
        var filter = Builders<TEntity>.Filter.Eq(idProperty.Name, idValue);
        await _collection.DeleteOneAsync(filter, cancellationToken);
    }

    /// <summary>
    /// 根据主键删除实体
    /// </summary>
    public async Task DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        // 尝试获取Id属性（大写I），如果不存在则尝试id属性（小写i）
        var idProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperty("id") ?? throw new InvalidOperationException("实体必须包含Id或id属性");
        var filter = Builders<TEntity>.Filter.Eq(idProperty.Name, id);
        await _collection.DeleteOneAsync(filter, cancellationToken);
    }

    /// <summary>
    /// 批量删除实体
    /// </summary>
    public async Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
        {
            await DeleteAsync(entity, cancellationToken);
        }
    }

    /// <summary>
    /// 分批删除实体（适用于大量数据）
    /// </summary>
    public async Task DeleteBatchedAsync(IEnumerable<TEntity> entities, int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        // 对于MongoDB，直接使用DeleteMany更高效，但需要先获取所有id
        var idProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperty("id") ?? throw new InvalidOperationException("实体必须包含Id或id属性");
        var ids = entities.Select(e => idProperty.GetValue(e)).ToList();
        
        var filter = Builders<TEntity>.Filter.In(idProperty.Name, ids);
        await _collection.DeleteManyAsync(filter, cancellationToken);
    }

    /// <summary>
    /// 根据条件删除实体
    /// </summary>
    public async Task<int> DeleteByConditionAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<TEntity>.Filter.Where(predicate);
        var result = await _collection.DeleteManyAsync(filter, cancellationToken);
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
        // 获取MongoClient实例
        var mongoClient = _collection.Database.Client;
        
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
        // 获取MongoClient实例
        var mongoClient = _collection.Database.Client;
        
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
    /// 使用聚合管道查询
    /// </summary>
    //public async Task<List<TEntity>> AggregateAsync(IEnumerable<PipelineStageDefinition<TEntity, TEntity>> pipeline, CancellationToken cancellationToken = default)
    //{
    //    var aggregate = _collection.Aggregate(pipeline);
    //    return await aggregate.ToListAsync(cancellationToken);
    //}

    /// <summary>
    /// 批量更新符合条件的文档
    /// </summary>
    public async Task<long> UpdateManyAsync(FilterDefinition<TEntity> filter, UpdateDefinition<TEntity> update, CancellationToken cancellationToken = default)
    {
        var result = await _collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount;
    }

    /// <summary>
    /// 批量删除符合条件的文档
    /// </summary>
    public async Task<long> DeleteManyAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken = default)
    {
        var result = await _collection.DeleteManyAsync(filter, cancellationToken);
        return result.DeletedCount;
    }

    /// <summary>
    /// 使用FilterDefinition查询文档
    /// </summary>
    public async Task<List<TEntity>> FindAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 使用FilterDefinition查询单个文档
    /// </summary>
    public async Task<TEntity> FindOneAsync(FilterDefinition<TEntity> filter, CancellationToken cancellationToken = default)
    {
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 根据条件获取分页实体（内部实现）
    /// </summary>
    private async Task<PagedResult<TEntity>> GetPagedByConditionAsync(FilterDefinition<TEntity> filter, int pageNumber, int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool isDescending = false,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 1000) pageSize = 1000;

        var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        IFindFluent<TEntity, TEntity> findFluent = _collection.Find(filter);

        // 排序
        if (orderBy != null)
        {
            findFluent = isDescending
                ? findFluent.SortByDescending(orderBy)
                : findFluent.SortBy(orderBy);
        }
        else
        {
            // 默认按Id或id排序
            var idProperty = typeof(TEntity).GetProperty("Id") ?? typeof(TEntity).GetProperty("id") ?? throw new InvalidOperationException("实体必须包含Id或id属性");
            findFluent = findFluent.SortBy(x => idProperty.GetValue(x));
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
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    #endregion
}
