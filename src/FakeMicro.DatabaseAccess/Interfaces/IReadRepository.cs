using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Threading;

namespace FakeMicro.DatabaseAccess
{
    /// <summary>
    /// 分页结果类
    /// </summary>
    public class PaginatedResult<T>
    {
        /// <summary>
        /// 数据项集合
        /// </summary>
        public IEnumerable<T>? Items { get; set; }
        
        /// <summary>
        /// 总记录数
        /// </summary>
        public int TotalCount { get; set; }
        
        /// <summary>
        /// 当前页码
        /// </summary>
        public int PageNumber { get; set; }
        
        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize { get; set; }
        
        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages => TotalCount > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    }
    
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
        /// 获取所有实体（带导航属性）
        /// </summary>
        Task<IEnumerable<TEntity>> GetAllWithIncludesAsync(
            CancellationToken cancellationToken = default,
            params Expression<Func<TEntity, object>>[] includes);
        
        /// <summary>
        /// 获取分页实体
        /// </summary>
        Task<PaginatedResult<TEntity>> GetPagedAsync(int pageNumber, int pageSize, 
            Expression<Func<TEntity, object>>? orderBy = null, 
            bool isDescending = false,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 根据主键获取实体
        /// </summary>
        Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 根据主键获取实体（带导航属性）
        /// </summary>
        Task<TEntity?> GetByIdWithIncludesAsync(TKey id,
            CancellationToken cancellationToken = default,
            params Expression<Func<TEntity, object>>[] includes);
        
        /// <summary>
        /// 根据条件获取实体
        /// </summary>
        Task<IEnumerable<TEntity>> GetByConditionAsync(Expression<Func<TEntity, bool>> predicate, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 根据条件获取分页实体
        /// </summary>
        Task<PaginatedResult<TEntity>> GetPagedByConditionAsync(Expression<Func<TEntity, bool>> predicate,
            int pageNumber, int pageSize,
            Expression<Func<TEntity, object>>? orderBy = null,
            bool isDescending = false,
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 检查实体是否存在
        /// </summary>
        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, 
            CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 获取实体数量
        /// </summary>
        Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, 
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
            
        /// <summary>
        /// 创建查询（用于复杂查询场景）
        /// </summary>
        IQueryable<TEntity> CreateQuery();
        
        /// <summary>
        /// 创建查询（带筛选条件）
        /// </summary>
        IQueryable<TEntity> CreateQuery(Expression<Func<TEntity, bool>> predicate);
        
        /// <summary>
        /// 禁用实体跟踪（用于只读查询优化）
        /// </summary>
        void DisableTracking();
        
        /// <summary>
        /// 启用实体跟踪
        /// </summary>
        void EnableTracking();
    }
}