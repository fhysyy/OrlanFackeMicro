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
    public class SqlSugarRepositoryFactory : ISqlSugarRepositoryFactory
    {
        private readonly ISqlSugarClient _sqlSugarClient;
        private readonly ILoggerFactory _loggerFactory;

        public SqlSugarRepositoryFactory(ISqlSugarClient sqlSugarClient, ILoggerFactory loggerFactory)
        {
            _sqlSugarClient = sqlSugarClient;
            _loggerFactory = loggerFactory;
        }

        public IRepository<TEntity, TKey> CreateRepository<TEntity, TKey>() where TEntity : class, new()
        {
            var logger = _loggerFactory.CreateLogger<SqlSugarRepository<TEntity, TKey>>();
            return new SqlSugarRepository<TEntity, TKey>(_sqlSugarClient, logger);
        }
    }
}
