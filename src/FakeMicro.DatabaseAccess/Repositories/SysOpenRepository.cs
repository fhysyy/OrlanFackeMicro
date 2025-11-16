using FakeMicro.DatabaseAccess.Interfaces;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Repositories
{
    public class SysOpenRepository : SqlSugarRepository<dynamic,int>, ISysOpenRepository
    {
        public SysOpenRepository(ISqlSugarClient db, ILogger<SysOpenRepository> logger)
            : base(db, logger)
        {
        }
    }
}
