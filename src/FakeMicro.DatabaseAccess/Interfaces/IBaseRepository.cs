using System; using System.Collections.Generic; using System.Linq.Expressions; using System.Threading; using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Interfaces;

/// <summary>
/// 基础仓储接口，定义了最基本的CRUD操作契约
/// 作为所有仓储接口的根接口
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TKey">主键类型</typeparam>
public interface IBaseRepository<TEntity, TKey> : IRepository where TEntity : class
{
    /// <summary>
    /// 获取实体类型
    /// </summary>
    Type EntityType { get; }
}
