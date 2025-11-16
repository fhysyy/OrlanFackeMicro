using FakeMicro.DatabaseAccess.Entities;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Entities;
using System.Linq.Expressions;
using System.Threading;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// 审计日志数据访问接口
/// </summary>
public interface IAuditLogRepository : IRepository<AuditLog, int>
{
    /// <summary>
    /// 获取审计日志
    /// </summary>
    Task<List<AuditLog>> GetAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null, long? userId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取用户审计日志
    /// </summary>
    Task<List<AuditLog>> GetAuditLogsByUserAsync(long userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取审计日志统计
    /// </summary>
    Task<AuditLogStatistics> GetAuditLogStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

    //Task<bool> AddAsync(AuditLog auditLog);

    ///// <summary>
    ///// 保存更改
    ///// </summary>
    //Task SaveChangesAsync(CancellationToken cancellationToken = default);
}