using FakeMicro.Interfaces.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Interfaces;

/// <summary>
/// 基础仓储接口，定义通用的CRUD操作
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public interface IBaseRepository<TEntity, TKey> where TEntity : class
{
    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    /// <param name="id">实体ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体对象</returns>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取所有实体
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体集合</returns>
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 分页获取实体
    /// </summary>
    /// <param name="pageNumber">页码（从1开始）</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="orderBy">排序字段</param>
    /// <param name="isDescending">是否降序</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    Task<PagedResult<TEntity>> GetPagedAsync(int pageNumber, int pageSize, 
        Expression<Func<TEntity, object>>? orderBy = null, 
        bool isDescending = false,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 根据条件获取实体
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体集合</returns>
    Task<IEnumerable<TEntity>> GetByConditionAsync(Expression<Func<TEntity, bool>> predicate, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 根据条件分页获取实体
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="pageNumber">页码（从1开始）</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="orderBy">排序字段</param>
    /// <param name="isDescending">是否降序</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分页结果</returns>
    Task<PagedResult<TEntity>> GetPagedByConditionAsync(Expression<Func<TEntity, bool>> predicate,
        int pageNumber, int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool isDescending = false,
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
    /// 更新实体
    /// </summary>
    /// <param name="entity">实体对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 根据ID删除实体
    /// </summary>
    /// <param name="id">实体ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 检查实体是否存在
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否存在</returns>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, 
        CancellationToken cancellationToken = default);
}