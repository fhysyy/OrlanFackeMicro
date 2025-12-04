using System;
using System.Threading.Tasks;
using FakeMicro.DatabaseAccess.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// PostgreSQL仓储创建策略
/// 用于创建基于SqlSugar的PostgreSQL仓储实例
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public class PostgreSqlRepositoryCreationStrategy<TEntity, TKey> : IRepositoryCreationStrategy<TEntity, TKey>
    where TEntity : class, new()
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider">服务提供器</param>
    public PostgreSqlRepositoryCreationStrategy(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 获取支持的数据库类型
    /// </summary>
    /// <returns>数据库类型</returns>
    public DatabaseType GetDatabaseType()
    {
        return DatabaseType.PostgreSQL;
    }

    /// <summary>
    /// 创建PostgreSQL仓储实例
    /// </summary>
    /// <returns>PostgreSQL仓储实例</returns>
    public IRepository<TEntity, TKey> CreateRepository()
    {
        // 使用服务提供器创建SqlSugarRepository实例
        var repository = ActivatorUtilities.CreateInstance<SqlSugarRepository<TEntity, TKey>>(_serviceProvider);
        return repository;
    }

    /// <summary>
    /// 异步创建PostgreSQL仓储实例
    /// </summary>
    /// <returns>PostgreSQL仓储实例</returns>
    public async Task<IRepository<TEntity, TKey>> CreateRepositoryAsync()
    {
        // 对于PostgreSQL，同步和异步创建逻辑相同
        return await Task.FromResult(CreateRepository());
    }
}