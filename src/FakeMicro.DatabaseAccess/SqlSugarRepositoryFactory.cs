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

        /// <summary>
        /// 创建通用仓储实例（显式实现IRepositoryFactory接口）
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <typeparam name="TKey">主键类型</typeparam>
        /// <returns>通用仓储实例</returns>
        IRepository<TEntity, TKey> IRepositoryFactory.CreateRepository<TEntity, TKey>()
        {
            return CreateSqlSugarRepository<TEntity, TKey>();
        }

        /// <summary>
        /// 创建通用仓储实例（异步，显式实现IRepositoryFactory接口）
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <typeparam name="TKey">主键类型</typeparam>
        /// <returns>通用仓储实例</returns>
        Task<IRepository<TEntity, TKey>> IRepositoryFactory.CreateRepositoryAsync<TEntity, TKey>()
        {
            return Task.FromResult<IRepository<TEntity, TKey>>(((IRepositoryFactory)this).CreateRepository<TEntity, TKey>());
        }

        /// <summary>
        /// 创建通用仓储实例（带数据库类型，显式实现IRepositoryFactory接口）
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <typeparam name="TKey">主键类型</typeparam>
        /// <param name="databaseType">数据库类型</param>
        /// <returns>通用仓储实例</returns>
        IRepository<TEntity, TKey> IRepositoryFactory.CreateRepository<TEntity, TKey>(DatabaseType databaseType)
        {
            // SqlSugar支持多种数据库类型，但这里我们只使用当前配置的数据库类型
            return ((IRepositoryFactory)this).CreateRepository<TEntity, TKey>();
        }

        /// <summary>
        /// 创建通用仓储实例（异步，带数据库类型，显式实现IRepositoryFactory接口）
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <typeparam name="TKey">主键类型</typeparam>
        /// <param name="databaseType">数据库类型</param>
        /// <returns>通用仓储实例</returns>
        Task<IRepository<TEntity, TKey>> IRepositoryFactory.CreateRepositoryAsync<TEntity, TKey>(DatabaseType databaseType)
        {
            return Task.FromResult<IRepository<TEntity, TKey>>(((IRepositoryFactory)this).CreateRepository<TEntity, TKey>());
        }

        /// <summary>
        /// 注册仓储创建策略（显式实现IRepositoryFactory接口）
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <typeparam name="TKey">主键类型</typeparam>
        /// <param name="databaseType">数据库类型</param>
        /// <param name="strategy">创建策略</param>
        void IRepositoryFactory.RegisterStrategy<TEntity, TKey>(DatabaseType databaseType, IRepositoryCreationStrategy<TEntity, TKey> strategy)
        {
            throw new NotSupportedException($"SqlSugarRepositoryFactory does not support registering custom strategies.");
        }

        /// <summary>
        /// 检查是否支持指定的数据库类型（显式实现IRepositoryFactory接口）
        /// </summary>
        /// <param name="databaseType">数据库类型</param>
        /// <returns>是否支持</returns>
        bool IRepositoryFactory.IsDatabaseTypeSupported(DatabaseType databaseType)
        {
            // SqlSugar支持多种关系型数据库
            return true;
        }
    }
}
