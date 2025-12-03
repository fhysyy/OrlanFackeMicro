using FakeMicro.DatabaseAccess.Entities;
using FakeMicro.DatabaseAccess.Interfaces;
using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Threading;
using FakeMicro.Entities;

namespace FakeMicro.DatabaseAccess.Repositories
{
    public class AuditLogRepository : SqlSugarRepository<AuditLog, long>, IAuditLogRepository
    {
        public AuditLogRepository(ISqlSugarClient db, ILogger<AuditLogRepository> logger)
            : base(db, logger)
        {}

        public async Task<List<AuditLog>> GetAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null, long? userId = null, CancellationToken cancellationToken = default)
        {
            return await GetSqlSugarClient().Queryable<AuditLog>()
                .WhereIF(startDate.HasValue, a => a.created_at >= startDate.Value)
                .WhereIF(endDate.HasValue, a => a.created_at <= endDate.Value)
                .WhereIF(userId.HasValue, a => a.user_id == userId.Value)
                .OrderBy(a => a.created_at, OrderByType.Desc)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<AuditLog>> GetAuditLogsByUserAsync(long userId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
        {
            return await GetSqlSugarClient().Queryable<AuditLog>()
                .Where(a => a.user_id == userId)
                .WhereIF(startDate.HasValue, a => a.created_at >= startDate.Value)
                .WhereIF(endDate.HasValue, a => a.created_at <= endDate.Value)
                .OrderBy(a => a.created_at, OrderByType.Desc)
                .ToListAsync(cancellationToken);
        }

        public async Task<AuditLogStatistics> GetAuditLogStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
        {
            var query = GetSqlSugarClient().Queryable<AuditLog>();
            
            if (startDate.HasValue)
                query = query.Where(a => a.created_at >= startDate.Value);
            
            if (endDate.HasValue)
                query = query.Where(a => a.created_at <= endDate.Value);
            
            var totalCount = await query.CountAsync(cancellationToken);
            var uniqueUserCount = await query.GroupBy(a => a.user_id).CountAsync(cancellationToken);
            var uniqueActionCount = await query.GroupBy(a => a.action).CountAsync(cancellationToken);
            
            return new AuditLogStatistics 
            { 
                TotalCount = totalCount, 
                UniqueUserCount = uniqueUserCount, 
                UniqueActionCount = uniqueActionCount 
            };
        }
    }
}