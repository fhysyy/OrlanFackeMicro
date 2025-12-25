using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Interfaces
{
    /// <summary>
    /// 关系型数据库(RDB)仓储接口
    /// 定义了关系型数据库特有的仓储操作方法
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    public interface IRdbRepository<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class
    {
        /// <summary>
        /// 获取所有实体（带导航属性）
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="includes">导航属性</param>
        /// <returns>实体集合</returns>
        Task<IEnumerable<TEntity>> GetAllWithIncludesAsync(
            CancellationToken cancellationToken = default,
            params Expression<Func<TEntity, object>>[] includes);
        
        /// <summary>
        /// 根据主键获取实体（带导航属性）
        /// </summary>
        /// <param name="id">主键值</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="includes">导航属性</param>
        /// <returns>实体对象，如果不存在则返回null</returns>
        Task<TEntity?> GetByIdWithIncludesAsync(TKey id,
            CancellationToken cancellationToken = default,
            params Expression<Func<TEntity, object>>[] includes);
        
        /// <summary>
        /// 开始事务
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>事务对象</returns>
        Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 提交事务
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 回滚事务
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}