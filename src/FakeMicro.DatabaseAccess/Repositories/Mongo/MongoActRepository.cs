using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.DatabaseAccess.Interfaces.Mongo;
using FakeMicro.Entities.ManagerVersion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Repositories.Mongo
{
    public class MongoActRepository<TEntity, TKey> : MongoRepository<TEntity, TKey>, IMongoActRepository<TEntity, TKey>
        where TEntity : class
        where TKey : IEquatable<TKey>
    {
        private readonly ILogger<MongoActRepository<TEntity, TKey>> _logger;

        /// <summary>
        /// MongoDB仓储构造函数
        /// </summary>
        /// <param name="db">SqlSugar客户端</param>
        /// <param name="logger">日志记录器</param>
        public MongoActRepository([FromKeyedServices("MongoDB")] ISqlSugarClient db, ILogger<MongoActRepository<TEntity, TKey>> logger)
            : base(db, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
    }

    public class MongoActRepository : MongoActRepository<object, ObjectId>, IMongoActRepository
    {
        public MongoActRepository([FromKeyedServices("MongoDB")] ISqlSugarClient db, ILogger<MongoActRepository> logger)
            : base(db, logger)
        {
        }
    }
}