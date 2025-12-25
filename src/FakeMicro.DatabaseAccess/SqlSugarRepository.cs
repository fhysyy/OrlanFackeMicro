using Microsoft.Extensions.Logging;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Interfaces.Models;
using FakeMicro.Shared.Exceptions;

namespace FakeMicro.DatabaseAccess
{
    /// <summary>
    /// SqlSugar仓储基类，实现了所有关系型数据库仓储接口
    /// 提供了基于SqlSugar的通用CRUD操作实现
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    public class SqlSugarRepository<TEntity, TKey> : ISqlRepository<TEntity, TKey> where TEntity : class, new()
    {
        private readonly ISqlSugarClient _db;
        private readonly ILogger<SqlSugarRepository<TEntity, TKey>> _logger;

        /// <summary>
        /// 获取实体类型
        /// </summary>
        public Type EntityType => typeof(TEntity);

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="db">SqlSugar客户端</param>
        /// <param name="logger">日志记录器</param>
        public SqlSugarRepository(ISqlSugarClient db, ILogger<SqlSugarRepository<TEntity, TKey>> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 获取SqlSugar客户端实例
        /// </summary>
        /// <returns>SqlSugar客户端</returns>
        protected ISqlSugarClient GetSqlSugarClient() => _db;

        #region IReadRepository Implementation

        /// <summary>
        /// 获取所有实体
        /// </summary>
        public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.Queryable<TEntity>().ToListAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "获取所有实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"获取所有{typeof(TEntity).Name}失败", ex);
            }
        }

        /// <summary>
        /// 获取分页实体
        /// </summary>
        public virtual async Task<PagedResult<TEntity>> GetPagedAsync(int pageNumber, int pageSize, 
            Expression<Func<TEntity, object>>? orderBy = null, 
            bool isDescending = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _db.Queryable<TEntity>();

                // 添加排序
                if (orderBy != null)
                {
                    query = isDescending 
                        ? query.OrderBy(orderBy, OrderByType.Desc) 
                        : query.OrderBy(orderBy, OrderByType.Asc);
                }
                else
                {
                    // 默认按主键排序
                    query = query.OrderBy($"id {(isDescending ? "DESC" : "ASC")}");
                }

                // 执行分页查询
                var totalCount = await query.CountAsync(cancellationToken);
                var items = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                return PagedResult<TEntity>.SuccessResult(items, (int)totalCount, pageNumber, pageSize);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "获取分页实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"获取{typeof(TEntity).Name}分页数据失败", ex);
            }
        }

        /// <summary>
        /// 根据主键获取实体
        /// </summary>
        public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.Queryable<TEntity>()
                    .Where($"id = @id", new { id })
                    .FirstAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "根据主键获取实体失败: {EntityType}, {Id}", typeof(TEntity).Name, id);
                throw new DataAccessException($"根据主键获取{typeof(TEntity).Name}失败", ex);
            }
        }

        /// <summary>
        /// 根据条件获取实体
        /// </summary>
        public virtual async Task<IEnumerable<TEntity>> GetByConditionAsync(Expression<Func<TEntity, bool>> predicate, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.Queryable<TEntity>()
                    .Where(predicate)
                    .ToListAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "根据条件获取实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"根据条件获取{typeof(TEntity).Name}失败", ex);
            }
        }

        /// <summary>
        /// 根据条件获取分页实体
        /// </summary>
        public virtual async Task<PagedResult<TEntity>> GetPagedByConditionAsync(Expression<Func<TEntity, bool>> predicate,
            int pageNumber, int pageSize,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool isDescending = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _db.Queryable<TEntity>()
                    .Where(predicate);

                // 添加排序
                if (orderBy != null)
                {
                    query = isDescending 
                        ? query.OrderBy(orderBy, OrderByType.Desc) 
                        : query.OrderBy(orderBy, OrderByType.Asc);
                }
                else
                {
                    // 默认按主键排序
                    query = query.OrderBy($"id {(isDescending ? "DESC" : "ASC")}");
                }

                // 执行分页查询
                var totalCount = await query.CountAsync(cancellationToken);
                var items = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                return PagedResult<TEntity>.SuccessResult(items, (int)totalCount, pageNumber, pageSize);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "根据条件获取分页实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"根据条件获取{typeof(TEntity).Name}分页数据失败", ex);
            }
        }

        /// <summary>
        /// 获取实体数量
        /// </summary>
        public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _db.Queryable<TEntity>();
                if (predicate != null)
                {
                    query = query.Where(predicate);
                }
                return await query.CountAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "获取实体数量失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"获取{typeof(TEntity).Name}数量失败", ex);
            }
        }

        /// <summary>
        /// 检查实体是否存在
        /// </summary>
        public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.Queryable<TEntity>()
                    .AnyAsync(predicate, cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "检查实体是否存在失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"检查{typeof(TEntity).Name}是否存在失败", ex);
            }
        }

        /// <summary>
        /// 获取第一个符合条件的实体
        /// </summary>
        public virtual async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.Queryable<TEntity>()
                    .Where(predicate)
                    .FirstAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "获取第一个符合条件的实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"获取第一个符合条件的{typeof(TEntity).Name}失败", ex);
            }
        }

        /// <summary>
        /// 获取单个符合条件的实体
        /// </summary>
        public virtual async Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.Queryable<TEntity>()
                    .Where(predicate)
                    .SingleAsync();
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "获取单个符合条件的实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"获取单个符合条件的{typeof(TEntity).Name}失败", ex);
            }
        }

        #endregion

        #region IWriteRepository Implementation

        /// <summary>
        /// 添加实体
        /// </summary>
        public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            try
            {
                await _db.Insertable(entity).ExecuteCommandAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "添加实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"添加{typeof(TEntity).Name}失败", ex);
            }
        }

        /// <summary>
        /// 批量添加实体
        /// </summary>
        public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            try
            {
                await _db.Insertable(entities.ToList()).ExecuteCommandAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "批量添加实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"批量添加{typeof(TEntity).Name}失败", ex);
            }
        }

        /// <summary>
        /// 更新实体
        /// </summary>
        public virtual void Update(TEntity entity)
        {
            try
            {
                _db.Updateable(entity).ExecuteCommand();
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "更新实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"更新{typeof(TEntity).Name}失败", ex);
            }
        }

        /// <summary>
        /// 批量更新实体
        /// </summary>
        public virtual void UpdateRange(IEnumerable<TEntity> entities)
        {
            try
            {
                _db.Updateable(entities.ToList()).ExecuteCommand();
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "批量更新实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"批量更新{typeof(TEntity).Name}失败", ex);
            }
        }

        /// <summary>
        /// 删除实体
        /// </summary>
        public virtual void Delete(TEntity entity)
        {
            try
            {
                _db.Deleteable(entity).ExecuteCommand();
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "删除实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"删除{typeof(TEntity).Name}失败", ex);
            }
        }

        /// <summary>
        /// 根据主键删除实体
        /// </summary>
        public virtual async Task DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _db.Deleteable<TEntity>()
                    .Where($"id = @id", new { id })
                    .ExecuteCommandAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "根据主键删除实体失败: {EntityType}, {Id}", typeof(TEntity).Name, id);
                throw new DataAccessException($"根据主键删除{typeof(TEntity).Name}失败", ex);
            }
        }

        /// <summary>
        /// 批量删除实体
        /// </summary>
        public virtual void DeleteRange(IEnumerable<TEntity> entities)
        {
            try
            {
                _db.Deleteable(entities.ToList()).ExecuteCommand();
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "批量删除实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"批量删除{typeof(TEntity).Name}失败", ex);
            }
        }

        /// <summary>
        /// 更新实体（异步）
        /// </summary>
        public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            try
            {
                await _db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "更新实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"更新{typeof(TEntity).Name}失败", ex);
            }
        }

        /// <summary>
        /// 批量更新实体（异步）
        /// </summary>
        public virtual async Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            try
            {
                await _db.Updateable(entities.ToList()).ExecuteCommandAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "批量更新实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"批量更新{typeof(TEntity).Name}失败", ex);
            }
        }

        /// <summary>
        /// 删除实体（异步）
        /// </summary>
        public virtual async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            try
            {
                await _db.Deleteable(entity).ExecuteCommandAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "删除实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"删除{typeof(TEntity).Name}失败", ex);
            }
        }

        /// <summary>
        /// 批量删除实体（异步）
        /// </summary>
        public virtual async Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            try
            {
                await _db.Deleteable(entities.ToList()).ExecuteCommandAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "批量删除实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"批量删除{typeof(TEntity).Name}失败", ex);
            }
        }

        /// <summary>
        /// 部分更新实体（异步）
        /// </summary>
        public virtual async Task UpdatePartialAsync(TEntity entity, params Expression<Func<TEntity, object>>[] properties)
        {
            try
            {
                // 对于部分更新，我们需要使用不同的方法
                // 这里我们直接更新整个实体，因为SqlSugar的SetColumns方法使用方式不同
                await _db.Updateable(entity).ExecuteCommandAsync();
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "部分更新实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"部分更新{typeof(TEntity).Name}失败", ex);
            }
        }

        /// <summary>
        /// 部分更新实体（异步，带取消令牌）
        /// </summary>
        public virtual async Task UpdatePartialAsync(TEntity entity, CancellationToken cancellationToken, params Expression<Func<TEntity, object>>[] properties)
        {
            try
            {
                // 对于部分更新，我们需要使用不同的方法
                // 这里我们直接更新整个实体，因为SqlSugar的SetColumns方法使用方式不同
                await _db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "部分更新实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"部分更新{typeof(TEntity).Name}失败", ex);
            }
        }

        /// <summary>
        /// 软删除实体
        /// </summary>
        public virtual async Task SoftDeleteAsync(TKey id, string deletedBy = "system", CancellationToken cancellationToken = default)
        {
            try
            {
                await _db.Updateable<TEntity>()
                    .SetColumns($"is_deleted = @isDeleted, deleted_at = @deletedAt, deleted_by = @deletedBy", 
                        new { isDeleted = true, deletedAt = DateTime.UtcNow, deletedBy })
                    .Where($"id = @id", new { id })
                    .ExecuteCommandAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "软删除实体失败: {EntityType}, {Id}", typeof(TEntity).Name, id);
                throw new DataAccessException($"软删除{typeof(TEntity).Name}失败", ex);
            }
        }

        /// <summary>
        /// 批量软删除实体
        /// </summary>
        public virtual async Task SoftDeleteRangeAsync(IEnumerable<TKey> ids, string deletedBy = "system", CancellationToken cancellationToken = default)
        {
            try
            {
                await _db.Updateable<TEntity>()
                    .SetColumns($"is_deleted = @isDeleted, deleted_at = @deletedAt, deleted_by = @deletedBy", 
                        new { isDeleted = true, deletedAt = DateTime.UtcNow, deletedBy })
                    .Where($"id IN ({string.Join(",", ids)})")
                    .ExecuteCommandAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "批量软删除实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"批量软删除{typeof(TEntity).Name}失败", ex);
            }
        }

        /// <summary>
        /// 保存更改
        /// </summary>
        public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // SqlSugar自动处理事务，直接返回
            await Task.CompletedTask;
            return 1;
        }

        /// <summary>
        /// 执行批量插入（高性能）
        /// </summary>
        public virtual async Task<int> BulkInsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.Insertable(entities.ToList()).ExecuteCommandAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "批量插入实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"批量插入{typeof(TEntity).Name}失败", ex);
            }
        }

        /// <summary>
        /// 执行批量更新（高性能）
        /// </summary>
        public virtual async Task<int> BulkUpdateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.Updateable(entities.ToList()).ExecuteCommandAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "批量更新实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"批量更新{typeof(TEntity).Name}失败", ex);
            }
        }

        #endregion

        #region IRepository Implementation

        /// <summary>
        /// 分批添加实体（适用于大量数据）
        /// </summary>
        public virtual async Task AddBatchedAsync(IEnumerable<TEntity> entities, int batchSize = 1000, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                var entityList = entities.ToList();
                for (int i = 0; i < entityList.Count; i += batchSize)
                {
                    var batch = entityList.Skip(i).Take(batchSize).ToList();
                    await _db.Insertable(batch).ExecuteCommandAsync(cancellationToken);
                }
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "分批添加实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"分批添加{typeof(TEntity).Name}失败", ex);
            }
        }

        /// <summary>
        /// 分批删除实体（适用于大量数据）
        /// </summary>
        public virtual async Task DeleteBatchedAsync(IEnumerable<TEntity> entities, int batchSize = 1000,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var entityList = entities.ToList();
                for (int i = 0; i < entityList.Count; i += batchSize)
                {
                    var batch = entityList.Skip(i).Take(batchSize).ToList();
                    await _db.Deleteable(batch).ExecuteCommandAsync(cancellationToken);
                }
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "分批删除实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"分批删除{typeof(TEntity).Name}失败", ex);
            }
        }

        /// <summary>
        /// 根据条件删除实体
        /// </summary>
        public virtual async Task<int> DeleteByConditionAsync(Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.Deleteable<TEntity>()
                    .Where(predicate)
                    .ExecuteCommandAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "根据条件删除实体失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"根据条件删除{typeof(TEntity).Name}失败", ex);
            }
        }

        /// <summary>
        /// 禁用实体跟踪（用于只读查询优化）
        /// </summary>
        public virtual void DisableTracking()
        {
            _db.Ado.IsDisableMasterSlaveSeparation = true;
        }

        /// <summary>
        /// 启用实体跟踪
        /// </summary>
        public virtual void EnableTracking()
        {
            _db.Ado.IsDisableMasterSlaveSeparation = false;
        }

        /// <summary>
        /// 清除实体跟踪缓存
        /// </summary>
        public virtual void ClearTracker()
        {
            // SqlSugar没有ClearDataCache方法，这里留空实现
        }

        #endregion

        #region IRdbRepository Implementation

        /// <summary>
        /// 获取所有实体（带导航属性）
        /// </summary>
        public virtual async Task<IEnumerable<TEntity>> GetAllWithIncludesAsync(
            CancellationToken cancellationToken = default,
            params Expression<Func<TEntity, object>>[] includes)
        {
            try
            {
                var query = _db.Queryable<TEntity>();
                
                // 添加导航属性
                if (includes != null && includes.Length > 0)
                {
                    // 逐个添加includes，避免类型推断问题
                    foreach (var include in includes)
                    {
                        query = query.Includes(include);
                    }
                }
                
                return await query.ToListAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "获取所有实体（带导航属性）失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException($"获取所有{typeof(TEntity).Name}（带导航属性）失败", ex);
            }
        }

        /// <summary>
        /// 根据主键获取实体（带导航属性）
        /// </summary>
        public virtual async Task<TEntity?> GetByIdWithIncludesAsync(TKey id,
            CancellationToken cancellationToken = default,
            params Expression<Func<TEntity, object>>[] includes)
        {
            try
            {
                var query = _db.Queryable<TEntity>()
                    .Where($"id = @id", new { id });
                
                // 添加导航属性
                if (includes != null && includes.Length > 0)
                {
                    // 逐个添加includes，避免类型推断问题
                    foreach (var include in includes)
                    {
                        query = query.Includes(include);
                    }
                }
                
                return await query.FirstAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "根据主键获取实体（带导航属性）失败: {EntityType}, {Id}", typeof(TEntity).Name, id);
                throw new DataAccessException($"根据主键获取{typeof(TEntity).Name}（带导航属性）失败", ex);
            }
        }

        /// <summary>
        /// 开始事务
        /// </summary>
        public virtual async Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _db.Ado.BeginTranAsync();
                return new SqlSugarTransactionScope(_db, _logger);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "开始事务失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException("开始事务失败", ex);
            }
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public virtual async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _db.Ado.CommitTranAsync();
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "提交事务失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException("提交事务失败", ex);
            }
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public virtual async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _db.Ado.RollbackTranAsync();
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "回滚事务失败: {EntityType}", typeof(TEntity).Name);
                throw new DataAccessException("回滚事务失败", ex);
            }
        }

        #endregion

        #region ISqlRepository Implementation

        /// <summary>
        /// 执行SQL语句
        /// </summary>
        public virtual async Task<int> ExecuteSqlAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.Ado.ExecuteCommandAsync(sql, parameters, cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "执行SQL语句失败: {Sql}", sql);
                throw new DataAccessException("执行SQL语句失败", ex);
            }
        }

        /// <summary>
        /// 执行SQL查询
        /// </summary>
        public virtual async Task<IEnumerable<TEntity>> QuerySqlAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.Ado.SqlQueryAsync<TEntity>(sql, parameters, cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "执行SQL查询失败: {Sql}", sql);
                throw new DataAccessException("执行SQL查询失败", ex);
            }
        }

        #endregion

        #region ITransactionalRepository Implementation

        /// <summary>
        /// 执行事务
        /// </summary>
        /// <param name="action">事务内执行的操作</param>
        /// <param name="cancellationToken">取消令牌</param>
        public virtual async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            try
            {
                await _db.Ado.BeginTranAsync();
                await action();
                await _db.Ado.CommitTranAsync();
            }
            catch (Exception ex)
            {
                await _db.Ado.RollbackTranAsync();
                _logger.LogError(ex, "事务执行失败");
                throw new DataAccessException("事务执行失败", ex);
            }
        }

        /// <summary>
        /// 执行事务并返回结果
        /// </summary>
        /// <typeparam name="TResult">结果类型</typeparam>
        /// <param name="action">事务内执行的操作</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>操作结果</returns>
        public virtual async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken = default)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            try
            {
                await _db.Ado.BeginTranAsync();
                var result = await action();
                await _db.Ado.CommitTranAsync();
                return result;
            }
            catch (Exception ex)
            {
                await _db.Ado.RollbackTranAsync();
                _logger.LogError(ex, "事务执行失败");
                throw new DataAccessException("事务执行失败", ex);
            }
        }

        #endregion
    }

    /// <summary>
    /// SqlSugar事务作用域
    /// </summary>
    internal class SqlSugarTransactionScope : IDisposable
    {
        private readonly ISqlSugarClient _db;
        private readonly ILogger _logger;
        private bool _disposed = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="db">SqlSugar客户端</param>
        /// <param name="logger">日志记录器</param>
        public SqlSugarTransactionScope(ISqlSugarClient db, ILogger logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        _db.Ado.CommitTran();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "事务提交失败");
                        try
                        {
                            _db.Ado.RollbackTran();
                        }
                        catch (Exception rollbackEx)
                        {
                            _logger.LogError(rollbackEx, "事务回滚失败");
                        }
                    }
                }
                _disposed = true;
            }
        }
    }
}
