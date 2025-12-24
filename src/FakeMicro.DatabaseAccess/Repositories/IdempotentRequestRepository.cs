using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Threading;
using System.Threading.Tasks;
using FakeMicro.DatabaseAccess.Entities;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Shared.Exceptions;

namespace FakeMicro.DatabaseAccess.Repositories;

/// <summary>
/// 幂等性请求仓储实现
/// 基于SqlSugar实现幂等性请求的数据库操作
/// </summary>
public class IdempotentRequestRepository : SqlSugarRepository<IdempotentRequest, long>, IIdempotentRequestRepository
{
    private readonly ILogger<IdempotentRequestRepository> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="db">SqlSugar客户端</param>
    /// <param name="logger">日志记录器</param>
    public IdempotentRequestRepository(ISqlSugarClient db, ILogger<IdempotentRequestRepository> logger)
        : base(db, logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 根据幂等性键获取请求记录
    /// </summary>
    /// <param name="idempotencyKey">幂等性键</param>
    /// <param name="userId">用户ID</param>
    /// <param name="httpMethod">HTTP方法</param>
    /// <param name="requestPath">请求路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>幂等性请求记录</returns>
    public async Task<IdempotentRequest?> GetByIdempotencyKeyAsync(string idempotencyKey, long? userId = null, string httpMethod = null, string requestPath = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var queryable = GetSqlSugarClient().Queryable<IdempotentRequest>()
                .With(SqlWith.NoLock)
                .Where(x => x.IdempotencyKey == idempotencyKey && x.UserId == userId && x.ExpiresAt > DateTime.Now);
    
            // 如果提供了HTTP方法，则添加条件
            if (!string.IsNullOrEmpty(httpMethod))
            {
                queryable = queryable.Where(x => x.HttpMethod == httpMethod);
            }
    
            // 如果提供了请求路径，则添加条件
            if (!string.IsNullOrEmpty(requestPath))
            {
                queryable = queryable.Where(x => x.RequestPath == requestPath);
            }
    
            return await queryable.FirstAsync(cancellationToken);
        }
        catch (SqlSugarException ex)
        {
            _logger.LogError(ex, "获取幂等性请求记录失败: {IdempotencyKey}", idempotencyKey);
            throw new DataAccessException($"获取幂等性请求记录失败: {idempotencyKey}", ex);
        }
    }

    /// <summary>
    /// 删除过期的幂等性请求记录
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>删除的记录数</returns>
    public async Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return await GetSqlSugarClient().Deleteable<IdempotentRequest>()
                .Where(x => x.ExpiresAt <= DateTime.Now)
                .ExecuteCommandAsync(cancellationToken);
        }
        catch (SqlSugarException ex)
        {
            _logger.LogError(ex, "删除过期幂等性请求记录失败");
            throw new DataAccessException("删除过期幂等性请求记录失败", ex);
        }
    }
}
