using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Threading;
using FakeMicro.Interfaces.Models;

namespace FakeMicro.DatabaseAccess.Interfaces
{
    /// <summary>
    /// 只读仓储接口
    /// 提供只读查询操作，支持性能优化
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    public interface IReadRepository<TEntity, TKey> where TEntity : class
    {
        /// <summary>
        /// 获取所有实体
        /// </summary>
        Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 获取分页实体
        /// </summary>
        Task<PagedResult<TEntity>> GetPagedAsync(int pageNumber, int pageSize, 
            Expression<Func<TEntity, object>>? orderBy = null, 
            bool isDescending = false,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 根据主键获取实体
        /// </summary>
        Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 根据条件获取实体
        /// </summary>
        Task<IEnumerable<TEntity>> GetByConditionAsync(Expression<Func<TEntity, bool>> predicate, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 根据条件获取分页实体
        /// </summary>
        Task<PagedResult<TEntity>> GetPagedByConditionAsync(Expression<Func<TEntity, bool>> predicate,
            int pageNumber, int pageSize,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool isDescending = false,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 获取实体数量
        /// </summary>
        Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 检查实体是否存在
        /// </summary>
        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 获取第一个符合条件的实体
        /// </summary>
        Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 获取单个符合条件的实体
        /// </summary>
        Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, 
            CancellationToken cancellationToken = default);
    }
}