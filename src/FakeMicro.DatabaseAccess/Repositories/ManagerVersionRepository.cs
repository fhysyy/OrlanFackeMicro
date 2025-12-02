using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Entities;
using FakeMicro.Entities.ManagerVersion;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Repositories
{
    public class ManagerVersionRepository: MongoRepository<ManagerVersion,string>, IManagerVersionRepository
    {
        private readonly ILogger<ManagerVersionRepository> _logger;

        /// <summary>
        /// ManagerVersion MongoDB仓储构造函数
        /// </summary>
        /// <param name="db">SqlSugar客户端</param>
        /// <param name="logger">日志记录器</param>
        public ManagerVersionRepository(ISqlSugarClient db, ILogger<ManagerVersionRepository> logger)
            : base(db, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
    }
}
