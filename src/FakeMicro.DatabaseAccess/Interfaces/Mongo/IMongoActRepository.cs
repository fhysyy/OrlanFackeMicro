using FakeMicro.Entities.ManagerVersion;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Interfaces.Mongo
{
    public interface IMongoActRepository<TEntity, TKey> : IMongoRepository<TEntity, TKey>
        where TEntity : class
        where TKey : IEquatable<TKey>
    {
    }

    public interface IMongoActRepository : IMongoActRepository<object, ObjectId>
    {
    }
}
