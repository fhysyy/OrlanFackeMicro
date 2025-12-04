using System;
using System.Threading.Tasks;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.DatabaseAccess.Repositories.Mongo;
using Microsoft.Extensions.DependencyInjection;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// MongoDB仓储创建策略
/// 用于创建基于MongoDB的仓储实例
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public class MongoDbRepositoryCreationStrategy<TEntity, TKey> : IRepositoryCreationStrategy<TEntity, TKey>
    where TEntity : class, new()
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider">服务提供器</param>
    public MongoDbRepositoryCreationStrategy(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 获取支持的数据库类型
    /// </summary>
    /// <returns>数据库类型</returns>
    public DatabaseType GetDatabaseType()
    {
        return DatabaseType.MongoDB;
    }

    /// <summary>
    /// 创建MongoDB仓储实例
    /// </summary>
    /// <returns>MongoDB仓储实例</returns>
    public IRepository<TEntity, TKey> CreateRepository()
    {
        // 使用服务提供器创建MongoRepository实例
        // 特别处理TEntity为Object类型的情况
        if (typeof(TEntity) == typeof(object))
        {
            // 使用反射创建仓储实例，绕过泛型约束
            var repositoryType = typeof(MongoRepository<,>).MakeGenericType(typeof(TEntity), typeof(TKey));
            return (IRepository<TEntity, TKey>)ActivatorUtilities.CreateInstance(_serviceProvider, repositoryType);
        }

        // 正常创建仓储实例
        var repository = ActivatorUtilities.CreateInstance<MongoRepository<TEntity, TKey>>(_serviceProvider);
        return repository;
    }

    /// <summary>
    /// 异步创建MongoDB仓储实例
    /// </summary>
    /// <returns>MongoDB仓储实例</returns>
    public async Task<IRepository<TEntity, TKey>> CreateRepositoryAsync()
    {
        // 对于MongoDB，同步和异步创建逻辑相同
        return await Task.FromResult(CreateRepository());
    }
}