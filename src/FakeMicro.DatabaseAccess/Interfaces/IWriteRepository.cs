using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Interfaces
{
    /// <summary>
    /// 写操作仓储接口
    /// 提供增删改操作，支持批量操作
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    public interface IWriteRepository<TEntity, TKey> where TEntity : class
    {
        /// <summary>
        /// 添加实体
        /// </summary>
        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 批量添加实体
        /// </summary>
        Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 更新实体
        /// </summary>
        void Update(TEntity entity);
        
        /// <summary>
        /// 更新实体（异步）
        /// </summary>
        Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 批量更新实体
        /// </summary>
        void UpdateRange(IEnumerable<TEntity> entities);
        
        /// <summary>
        /// 批量更新实体（异步）
        /// </summary>
        Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 删除实体
        /// </summary>
        void Delete(TEntity entity);
        
        /// <summary>
        /// 删除实体（异步）
        /// </summary>
        Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 根据主键删除实体
        /// </summary>
        Task DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 批量删除实体
        /// </summary>
        void DeleteRange(IEnumerable<TEntity> entities);
        
        /// <summary>
        /// 批量删除实体（异步）
        /// </summary>
        Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 软删除实体
        /// </summary>
        Task SoftDeleteAsync(TKey id, string deletedBy = "system", CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 批量软删除实体
        /// </summary>
        Task SoftDeleteRangeAsync(IEnumerable<TKey> ids, string deletedBy = "system", CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 保存更改
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 执行批量插入（高性能）
        /// </summary>
        Task<int> BulkInsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 执行批量更新（高性能）
        /// </summary>
        Task<int> BulkUpdateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 部分更新实体（异步）
        /// </summary>
        Task UpdatePartialAsync(TEntity entity, params Expression<Func<TEntity, object>>[] properties);
        
        /// <summary>
        /// 部分更新实体（异步，带取消令牌）
        /// </summary>
        Task UpdatePartialAsync(TEntity entity, CancellationToken cancellationToken, params Expression<Func<TEntity, object>>[] properties);
    }
}