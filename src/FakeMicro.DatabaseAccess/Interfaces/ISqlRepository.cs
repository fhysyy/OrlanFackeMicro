using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Interfaces;

/// <summary>
/// SQL数据库专用仓储接口
/// 扩展了关系型数据库仓储接口，提供SQL特有的功能
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public interface ISqlRepository<TEntity, TKey> : IRdbRepository<TEntity, TKey> where TEntity : class
{
    /// <summary>
    /// 执行SQL语句
    /// </summary>
    /// <param name="sql">SQL语句</param>
    /// <param name="parameters">参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>受影响的行数</returns>
    Task<int> ExecuteSqlAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 执行SQL查询
    /// </summary>
    /// <param name="sql">SQL语句</param>
    /// <param name="parameters">参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>查询结果</returns>
    Task<IEnumerable<TEntity>> QuerySqlAsync(string sql, object? parameters = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// SQL数据库专用只读仓储接口
/// 提供SQL特有的只读查询功能
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public interface ISqlReadRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>, IBaseRepository<TEntity, TKey> where TEntity : class
{
    /// <summary>
    /// 获取所有实体（带导航属性）
    /// </summary>
    Task<IEnumerable<TEntity>> GetAllWithIncludesAsync(
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includes);
    
    /// <summary>
    /// 根据主键获取实体（带导航属性）
    /// </summary>
    Task<TEntity?> GetByIdWithIncludesAsync(TKey id,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includes);
}

/// <summary>
/// SQL数据库专用写仓储接口
/// 提供SQL特有的写操作功能
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public interface ISqlWriteRepository<TEntity, TKey> : IWriteRepository<TEntity, TKey>, IBaseRepository<TEntity, TKey> where TEntity : class
{
    /// <summary>
    /// 开始事务
    /// </summary>
    Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 提交事务
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 回滚事务
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}