using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Interfaces;

/// <summary>
/// 统一通用仓储接口
/// 提供基本的CRUD操作和高级查询功能
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public interface IRepository<TEntity, TKey> where TEntity : class
{
    /// <summary>
    /// 获取所有实体
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体集合</returns>
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取所有实体（带导航属性）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="includes">要包含的导航属性</param>
    /// <returns>实体集合</returns>
    Task<IEnumerable<TEntity>> GetAllWithIncludesAsync(
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includes);
    
    /// <summary>
    /// 获取分页实体
    /// </summary>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="orderBy">排序表达式</param>
    /// <param name="isDescending">是否降序</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    Task<PagedResult<TEntity>> GetPagedAsync(int pageNumber, int pageSize, 
        Expression<Func<TEntity, object>>? orderBy = null, 
        bool isDescending = false,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 根据主键获取实体
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体对象，如果不存在则返回null</returns>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 根据主键获取实体（带导航属性）
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="includes">要包含的导航属性</param>
    /// <returns>实体对象，如果不存在则返回null</returns>
    Task<TEntity?> GetByIdWithIncludesAsync(TKey id, 
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includes);
    
    /// <summary>
    /// 根据条件获取实体
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>符合条件的实体集合</returns>
    Task<IEnumerable<TEntity>> GetByConditionAsync(Expression<Func<TEntity, bool>> predicate, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 根据条件获取分页实体
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="pageNumber">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="orderBy">排序表达式</param>
    /// <param name="isDescending">是否降序</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    Task<PagedResult<TEntity>> GetPagedByConditionAsync(Expression<Func<TEntity, bool>> predicate,
        int pageNumber, int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool isDescending = false,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 检查实体是否存在
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取实体数量
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体数量</returns>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 添加实体
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 批量添加实体
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 分批添加实体（适用于大量数据）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="batchSize">每批大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task AddBatchedAsync(IEnumerable<TEntity> entities, int batchSize = 1000, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 更新实体
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 部分更新实体（仅更新指定属性）
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="properties">要更新的属性</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdatePartialAsync(TEntity entity, params Expression<Func<TEntity, object>>[] properties);
    
    /// <summary>
    /// 批量更新实体
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 删除实体
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 根据主键删除实体
    /// </summary>
    /// <param name="id">主键值</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 批量删除实体
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 分批删除实体（适用于大量数据）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="batchSize">每批大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteBatchedAsync(IEnumerable<TEntity> entities, int batchSize = 1000,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 根据条件删除实体
    /// </summary>
    /// <param name="predicate">删除条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的实体数量</returns>
    Task<int> DeleteByConditionAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 保存更改
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>受影响的行数</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 执行事务
    /// </summary>
    /// <param name="action">事务内执行的操作</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ExecuteInTransactionAsync(Func<Task> action, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 执行事务并返回结果
    /// </summary>
    /// <typeparam name="TResult">结果类型</typeparam>
    /// <param name="action">事务内执行的操作</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> action,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 禁用实体跟踪（用于只读查询优化）
    /// </summary>
    void DisableTracking();
    
    /// <summary>
    /// 启用实体跟踪
    /// </summary>
    void EnableTracking();
    
    /// <summary>
    /// 清除实体跟踪缓存
    /// </summary>
    void ClearTracker();
    
    /// <summary>
    /// 更新实体
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="cancellationToken">取消令牌</param>
}