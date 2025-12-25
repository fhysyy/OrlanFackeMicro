using System.Threading;
using System.Threading.Tasks;
using FakeMicro.DatabaseAccess.Entities;

namespace FakeMicro.DatabaseAccess.Interfaces;

/// <summary>
/// 幂等性请求仓储接口
/// 定义幂等性请求的数据库操作方法
/// </summary>
public interface IIdempotentRequestRepository : IRepository<IdempotentRequest, long>
{
    /// <summary>
    /// 根据幂等性键获取请求记录
    /// </summary>
    /// <param name="idempotencyKey">幂等性键</param>
    /// <param name="userId">用户ID</param>
    /// <param name="httpMethod">HTTP方法</param>
    /// <param name="requestPath">请求路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>幂等性请求记录</returns>
    Task<IdempotentRequest?> GetByIdempotencyKeyAsync(string idempotencyKey, long? userId = null, string httpMethod = null, string requestPath = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 删除过期的幂等性请求记录
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的记录数</returns>
    Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default);
}