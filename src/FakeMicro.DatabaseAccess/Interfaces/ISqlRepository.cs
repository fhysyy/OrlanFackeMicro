using System; using System.Collections.Generic; using System.Linq.Expressions; using System.Threading; using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Interfaces;

/// <summary>
    /// SQL关系型数据库仓储接口，提供RDB特有的CRUD操作和高级查询功能
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    public interface ISqlRepository<TEntity, TKey> : IBaseRepository<TEntity, TKey> where TEntity : class
{
    /// <summary>
    /// 获取所有实体，并加载指定的导航属性
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="includes">导航属性表达式</param>
    /// <returns>实体集合</returns>
    Task<IEnumerable<TEntity>> GetAllWithIncludesAsync(
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includes);
    
    /// <summary>
    /// 根据ID获取实体，并加载指定的导航属性
    /// </summary>
    /// <param name="id">实体ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="includes">导航属性表达式</param>
    /// <returns>实体对象</returns>
    Task<TEntity?> GetByIdWithIncludesAsync(TKey id, 
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includes);
    
    /// <summary>
    /// 开始事务
    /// </summary>
    Task BeginTransactionAsync();
    
    /// <summary>
    /// 提交事务
    /// </summary>
    Task CommitTransactionAsync();
    
    /// <summary>
    /// 回滚事务
    /// </summary>
    Task RollbackTransactionAsync();
    
    /// <summary>
    /// 执行原生SQL语句
    /// </summary>
    /// <param name="sql">SQL语句</param>
    /// <param name="parameters">参数</param>
    Task<int> ExecuteSqlAsync(string sql, params object[] parameters);
    
    /// <summary>
    /// 执行原生SQL查询
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="sql">SQL语句</param>
    /// <param name="parameters">参数</param>
    /// <returns>查询结果</returns>
    Task<IEnumerable<T>> QuerySqlAsync<T>(string sql, params object[] parameters);
}