using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.DatabaseAccess.Interfaces.Mongo;
using FakeMicro.Entities.ManagerVersion;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Repositories.Mongo
{

    public class MongoActRepository : MongoRepository<Object, string>, IMongoActRepository
    {
        private readonly ILogger<MongoActRepository> _logger;

        /// <summary>
        /// ManagerVersion MongoDB仓储构造函数
        /// </summary>
        /// <param name="db">SqlSugar客户端</param>
        /// <param name="logger">日志记录器</param>
        public MongoActRepository(ISqlSugarClient db, ILogger<MongoActRepository> logger)
            : base(db, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
    }

}