using FakeMicro.Entities.ManagerVersion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Interfaces
{
    public interface IManagerVersionRepository:IMongoRepository<ManagerVersion, string>
    {

    }
}
