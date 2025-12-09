using SqlSugar;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Entities;
using FakeMicro.Entities.Enums;
using FakeMicro.Interfaces.Events;
using FakeMicro.Interfaces.Models;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Threading;

namespace FakeMicro.DatabaseAccess
{
    /// <summary>
    /// 消息仓储实现
    /// </summary>
    public class MessageRepository : SqlSugarRepository<Message, long>, IMessageRepository
    {
        public MessageRepository(ISqlSugarClient db, ILogger<MessageRepository> logger)
            : base(db, logger)
        {}


        public async Task<Message?> GetByIdAsync(long id)
        {
            return await GetSqlSugarClient().Queryable<Message>()
                .Where(m => m.id == id)
                .FirstAsync();
        }

        public async Task<List<Message>> GetUserMessagesAsync(long userId, int page = 1, int pageSize = 20)
        {
            var skip = (page - 1) * pageSize;
            
            return await GetSqlSugarClient().Queryable<Message>()
                .Where(m => m.receiver_id == userId || m.sender_id == userId)
                .OrderBy(m => m.CreatedAt, OrderByType.Desc)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Message>> GetPendingMessagesAsync(int batchSize = 100)
        {
            var now = DateTime.UtcNow;
            
            return await GetSqlSugarClient().Queryable<Message>()
                .Where(m => m.status == MessageStatus.Pending.ToString() && 
                           (m.scheduled_at == null || m.scheduled_at <= now))
                .OrderBy(m => m.CreatedAt)
                .Take(batchSize)
                .ToListAsync();
        }

        public async Task<Message> AddAsync(Message message)
        {
            // 通过AOP已经记录SQL，这里不再额外记录
            await GetSqlSugarClient().Insertable(message).ExecuteCommandAsync();
            return message;
        }

        public async Task<Message> UpdateAsync(Message message)
        {
            message.UpdatedAt = DateTime.UtcNow;
            await GetSqlSugarClient().Updateable(message).ExecuteCommandAsync();
            return message;
        }

        public async Task<bool> UpdateStatusAsync(long messageId, MessageStatus status, string? errorMessage = null)
        {
            var updateable = GetSqlSugarClient().Updateable<Message>()
                .SetColumns(m => m.status == status.ToString())
                .SetColumns(m => m.error_message == errorMessage)
                .SetColumns(m => m.UpdatedAt == DateTime.UtcNow)
                .Where(m => m.id == messageId);
            
            switch (status)
            {
                case MessageStatus.Sent:
                    updateable.SetColumns(m => m.sent_at == DateTime.UtcNow);
                    break;
                case MessageStatus.Failed:
                    updateable.SetColumns(m => m.failed_at == DateTime.UtcNow)
                              .SetColumns(m => m.retry_count == m.retry_count + 1);
                    break;
            }
            
            var affectedRows = await updateable.ExecuteCommandAsync();
            return affectedRows > 0;
        }

        public async Task<bool> MarkAsDeliveredAsync(long messageId, string deliveryInfo)
        {
            var affectedRows = await GetSqlSugarClient().Updateable<Message>()
                .SetColumns(m => m.status == MessageStatus.Delivered.ToString())
                  .SetColumns(m => m.delivered_at == DateTime.UtcNow)
                .SetColumns(m => m.UpdatedAt == DateTime.UtcNow)
                .Where(m => m.id == messageId)
                .ExecuteCommandAsync();
            
            return affectedRows > 0;
        }

        public async Task<bool> MarkAsReadAsync(long messageId, string readBy)
        {
            var affectedRows = await GetSqlSugarClient().Updateable<Message>()
                .SetColumns(m => m.status == MessageStatus.Read.ToString())
                  .SetColumns(m => m.read_at == DateTime.UtcNow)
                .SetColumns(m => m.UpdatedAt == DateTime.UtcNow)
                .Where(m => m.id == messageId)
                .ExecuteCommandAsync();
            
            return affectedRows > 0;
        }

        public async Task<MessageStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var queryable = GetSqlSugarClient().Queryable<Message>();
            
            if (startDate.HasValue)
            {
                queryable = queryable.Where(m => m.CreatedAt >= startDate.Value);
            }
            
            if (endDate.HasValue)
            {
                queryable = queryable.Where(m => m.CreatedAt <= endDate.Value);
            }
            
            var totalMessages = await queryable.CountAsync();
            
            var statistics = new MessageStatistics
            {
                TotalMessages = totalMessages,
                StartTime = startDate ?? DateTime.MinValue,
                EndTime = endDate ?? DateTime.UtcNow
            };
            
            return statistics;
        }

        public async Task<int> CleanupExpiredMessagesAsync()
        {
            var now = DateTime.UtcNow;
            var deletedCount = await GetSqlSugarClient().Deleteable<Message>()
                .Where(m => m.expires_at != null && m.expires_at < now)
                .ExecuteCommandAsync();
            
            // 通过AOP已经记录SQL，不再额外记录日志
            
            return deletedCount;
        }
    }

    /// <summary>
    /// 消息模板仓储实现
    /// </summary>
    public class MessageTemplateRepository : SqlSugarRepository<MessageTemplate, Guid>, IMessageTemplateRepository
    {
        public MessageTemplateRepository(ISqlSugarClient db, ILogger<MessageTemplateRepository> logger)
            : base(db, logger)
        {}

        public async Task<MessageTemplate?> GetByIdAsync(Guid id)
        {
            return await GetSqlSugarClient().Queryable<MessageTemplate>()
                .Where(mt => mt.id.Equals(id))
                .FirstAsync();
        }

        public async Task<MessageTemplate?> GetByCodeAsync(string code)
        {
            return await GetSqlSugarClient().Queryable<MessageTemplate>()
                .Where(mt => mt.code == code && mt.is_enabled)
                .FirstAsync();
        }

        public async Task<List<MessageTemplate>> GetAllAsync()
        {
            return await GetSqlSugarClient().Queryable<MessageTemplate>()
                .Where(mt => mt.is_enabled)
                .OrderBy(mt => mt.name)
                .ToListAsync();
        }

        public async Task<MessageTemplate> AddAsync(MessageTemplate template)
        {
            // 通过AOP已经记录SQL，不再额外记录日志
            await GetSqlSugarClient().Insertable(template).ExecuteCommandAsync();
            return template;
        }

        public async Task<MessageTemplate> UpdateAsync(MessageTemplate template)
        {
            template.UpdatedAt = DateTime.UtcNow;
            await GetSqlSugarClient().Updateable(template).ExecuteCommandAsync();
            return template;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var affectedRows = await GetSqlSugarClient().Deleteable<MessageTemplate>()
                .Where(mt => mt.id.Equals(id))
                .ExecuteCommandAsync();
            return affectedRows > 0;
        }
    }
}