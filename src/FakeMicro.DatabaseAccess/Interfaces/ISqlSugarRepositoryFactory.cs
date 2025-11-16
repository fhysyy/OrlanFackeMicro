using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Interfaces
{

    /// <summary>
    /// SqlSugar仓储工厂接口
    /// </summary>

    public interface ISqlSugarRepositoryFactory
    {
        /// <summary>
        /// 创建指定实体类型的仓储
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <typeparam name="TKey">主键类型</typeparam>
        /// <returns>仓储实例</returns>
        IRepository<TEntity, TKey> CreateRepository<TEntity, TKey>() where TEntity : class, new();
    }
}
