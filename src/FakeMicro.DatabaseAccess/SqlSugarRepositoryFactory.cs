using FakeMicro.DatabaseAccess.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess
{
    /// <summary>
    /// SqlSugar仓储工厂实现（已优化为构造函数注入）
    /// </summary>
    public class SqlSugarRepositoryFactory : ISqlSugarRepositoryFactory, IRepositoryFactory
    {
        private readonly ISqlSugarClient _sqlSugarClient;
        private readonly ILoggerFactory _loggerFactory;

        public SqlSugarRepositoryFactory(ISqlSugarClient sqlSugarClient, ILoggerFactory loggerFactory)
        {
            _sqlSugarClient = sqlSugarClient;
            _loggerFactory = loggerFactory;
        }

        /// <summary>
        /// 创建SqlSugar仓储实例（带new()约束）
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <typeparam name="TKey">主键类型</typeparam>
        /// <returns>SqlSugar仓储实例</returns>
        public IRepository<TEntity, TKey> CreateSqlSugarRepository<TEntity, TKey>() where TEntity : class, new()
        {
            var logger = _loggerFactory.CreateLogger<SqlSugarRepository<TEntity, TKey>>();
            return new SqlSugarRepository<TEntity, TKey>(_sqlSugarClient, logger);
        }

        /// <summary>
        /// 创建SqlSugar仓储实例（实现ISqlSugarRepositoryFactory接口）
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <typeparam name="TKey">主键类型</typeparam>
        /// <returns>SqlSugar仓储实例</returns>
        IRepository<TEntity, TKey> ISqlSugarRepositoryFactory.CreateRepository<TEntity, TKey>()
        {
            return CreateSqlSugarRepository<TEntity, TKey>();
        }

        #region IRepositoryFactory 实现
        
        /// <summary>
        /// 创建通用仓储实例
        /// </summary>
        public IRepository<TEntity, TKey> CreateRepository<TEntity, TKey>() where TEntity : class, new()
        {
            return CreateSqlSugarRepository<TEntity, TKey>();
        }

        /// <summary>
        /// 创建通用仓储实例（异步）
        /// </summary>
        public Task<IRepository<TEntity, TKey>> CreateRepositoryAsync<TEntity, TKey>() where TEntity : class, new()
        {
            return Task.FromResult<IRepository<TEntity, TKey>>(CreateRepository<TEntity, TKey>());
        }

        /// <summary>
        /// 创建通用仓储实例（带数据库类型）
        /// </summary>
        public IRepository<TEntity, TKey> CreateRepository<TEntity, TKey>(DatabaseType databaseType) where TEntity : class, new()
        {
            // SqlSugar支持多种数据库类型，但这里我们只使用当前配置的数据库类型
            return CreateSqlSugarRepository<TEntity, TKey>();
        }

        /// <summary>
        /// 创建通用仓储实例（异步，带数据库类型）
        /// </summary>
        public Task<IRepository<TEntity, TKey>> CreateRepositoryAsync<TEntity, TKey>(DatabaseType databaseType) where TEntity : class, new()
        {
            return Task.FromResult<IRepository<TEntity, TKey>>(CreateRepository<TEntity, TKey>(databaseType));
        }

        /// <summary>
        /// 创建仓储实例（通过键值路由）
        /// </summary>
        public IRepository<TEntity, TKey> CreateRepository<TEntity, TKey>(TKey key) where TEntity : class, new()
        {
            // 对于SqlSugar，我们可以使用相同的实现，因为分片逻辑可能在SqlSugarClient内部处理
            return CreateSqlSugarRepository<TEntity, TKey>();
        }

        /// <summary>
        /// 创建仓储实例（异步，通过键值路由）
        /// </summary>
        public Task<IRepository<TEntity, TKey>> CreateRepositoryAsync<TEntity, TKey>(TKey key) where TEntity : class, new()
        {
            return Task.FromResult<IRepository<TEntity, TKey>>(CreateRepository<TEntity, TKey>(key));
        }

        /// <summary>
        /// 创建SQL仓储实例
        /// </summary>
        public ISqlRepository<TEntity, TKey> CreateSqlRepository<TEntity, TKey>() where TEntity : class, new()
        {
            var logger = _loggerFactory.CreateLogger<SqlSugarRepository<TEntity, TKey>>();
            return new SqlSugarRepository<TEntity, TKey>(_sqlSugarClient, logger);
        }

        /// <summary>
        /// 创建SQL仓储实例（异步）
        /// </summary>
        public Task<ISqlRepository<TEntity, TKey>> CreateSqlRepositoryAsync<TEntity, TKey>() where TEntity : class, new()
        {
            return Task.FromResult<ISqlRepository<TEntity, TKey>>(CreateSqlRepository<TEntity, TKey>());
        }

        /// <summary>
        /// 创建SQL仓储实例（通过键值路由）
        /// </summary>
        public ISqlRepository<TEntity, TKey> CreateSqlRepository<TEntity, TKey>(TKey key) where TEntity : class, new()
        {
            var logger = _loggerFactory.CreateLogger<SqlSugarRepository<TEntity, TKey>>();
            return new SqlSugarRepository<TEntity, TKey>(_sqlSugarClient, logger);
        }

        /// <summary>
        /// 创建SQL仓储实例（异步，通过键值路由）
        /// </summary>
        public Task<ISqlRepository<TEntity, TKey>> CreateSqlRepositoryAsync<TEntity, TKey>(TKey key) where TEntity : class, new()
        {
            return Task.FromResult<ISqlRepository<TEntity, TKey>>(CreateSqlRepository<TEntity, TKey>(key));
        }

        /// <summary>
        /// 创建MongoDB仓储实例
        /// </summary>
        public IMongoRepository<TEntity, TKey> CreateMongoRepository<TEntity, TKey>() where TEntity : class, new()
        {
            throw new NotSupportedException("SqlSugarRepositoryFactory does not support creating MongoDB repositories.");
        }

        /// <summary>
        /// 创建MongoDB仓储实例（异步）
        /// </summary>
        public Task<IMongoRepository<TEntity, TKey>> CreateMongoRepositoryAsync<TEntity, TKey>() where TEntity : class, new()
        {
            throw new NotSupportedException("SqlSugarRepositoryFactory does not support creating MongoDB repositories.");
        }

        /// <summary>
        /// 创建MongoDB仓储实例（带数据库名称）
        /// </summary>
        public IMongoRepository<TEntity, TKey> CreateMongoRepository<TEntity, TKey>(string? databaseName) where TEntity : class, new()
        {
            throw new NotSupportedException("SqlSugarRepositoryFactory does not support creating MongoDB repositories.");
        }

        /// <summary>
        /// 创建MongoDB仓储实例（异步，带数据库名称）
        /// </summary>
        public Task<IMongoRepository<TEntity, TKey>> CreateMongoRepositoryAsync<TEntity, TKey>(string? databaseName) where TEntity : class, new()
        {
            throw new NotSupportedException("SqlSugarRepositoryFactory does not support creating MongoDB repositories.");
        }

        /// <summary>
        /// 注册仓储创建策略
        /// </summary>
        public void RegisterStrategy<TEntity, TKey>(DatabaseType databaseType, IRepositoryCreationStrategy<TEntity, TKey> strategy) where TEntity : class, new()
        {
            throw new NotSupportedException($"SqlSugarRepositoryFactory does not support registering custom strategies.");
        }

        /// <summary>
        /// 检查是否支持指定的数据库类型
        /// </summary>
        public bool IsDatabaseTypeSupported(DatabaseType databaseType)
        {
            // SqlSugar支持多种关系型数据库
            return true;
        }
        
        #endregion
    }
}