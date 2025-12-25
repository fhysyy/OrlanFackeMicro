using FakeMicro.Interfaces.Models;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Interfaces;

/// <summary>
/// 仓库接口标记，用于依赖注入标识
/// </summary>
public interface IRepository { }

/// <summary>
/// 通用仓储接口基础，定义了所有仓储必须实现的核心方法
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public interface IRepository<TEntity, TKey> : IBaseRepository<TEntity, TKey>, IReadRepository<TEntity, TKey>, IWriteRepository<TEntity, TKey>, ITransactionalRepository where TEntity : class
{
    /// <summary>
    /// 获取实体数量
    /// </summary>
    /// <param name="predicate">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体数量</returns>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 分批添加实体（适用于大量数据）
    /// </summary>
    /// <param name="entities">实体集合</param>
    /// <param name="batchSize">每批大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task AddBatchedAsync(IEnumerable<TEntity> entities, int batchSize = 1000, 
        CancellationToken cancellationToken = default);
    
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
}