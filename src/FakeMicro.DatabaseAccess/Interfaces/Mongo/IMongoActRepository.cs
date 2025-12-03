using FakeMicro.Entities.ManagerVersion;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Interfaces.Mongo
{
    public interface IMongoActRepository: IMongoRepository<Object, ObjectId>
    {
    }
}
