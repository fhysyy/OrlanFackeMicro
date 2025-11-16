using FakeMicro.Entities;
using FakeMicro.Interfaces.Events;
using FakeMicro.Entities.Enums;
using FakeMicro.Interfaces.Models;

namespace FakeMicro.DatabaseAccess
{
    /// <summary>
    /// 消息仓储接口
    /// </summary>
    public interface IMessageRepository
    {
        /// <summary>
        /// 根据ID获取消息
        /// </summary>
        Task<Message?> GetByIdAsync(long id);
        
        /// <summary>
        /// 获取用户的消息列表
        /// </summary>
        Task<List<Message>> GetUserMessagesAsync(long userId, int page = 1, int pageSize = 20);
        
        /// <summary>
        /// 获取待发送的消息
        /// </summary>
        Task<List<Message>> GetPendingMessagesAsync(int batchSize = 100);
        
        /// <summary>
        /// 添加消息
        /// </summary>
        Task<Message> AddAsync(Message message);
        
        /// <summary>
        /// 更新消息
        /// </summary>
        Task<Message> UpdateAsync(Message message);
        
        /// <summary>
        /// 更新消息状态
        /// </summary>
        Task<bool> UpdateStatusAsync(long messageId, MessageStatus status, string? errorMessage = null);
        
        /// <summary>
        /// 标记消息为已投递
        /// </summary>
        Task<bool> MarkAsDeliveredAsync(long messageId, string deliveryInfo);
        
        /// <summary>
        /// 标记消息为已阅读
        /// </summary>
        Task<bool> MarkAsReadAsync(long messageId, string readBy);
        
        /// <summary>
        /// 获取消息统计
        /// </summary>
        Task<MessageStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        
        /// <summary>
        /// 清理过期消息
        /// </summary>
        Task<int> CleanupExpiredMessagesAsync();
    }

    /// <summary>
    /// 消息模板仓储接口
    /// </summary>
    public interface IMessageTemplateRepository
    {
        /// <summary>
        /// 根据ID获取模板
        /// </summary>
        Task<MessageTemplate?> GetByIdAsync(Guid id);
        
        /// <summary>
        /// 根据代码获取模板
        /// </summary>
        Task<MessageTemplate?> GetByCodeAsync(string code);
        
        /// <summary>
        /// 获取所有模板
        /// </summary>
        Task<List<MessageTemplate>> GetAllAsync();
        
        /// <summary>
        /// 添加模板
        /// </summary>
        Task<MessageTemplate> AddAsync(MessageTemplate template);
        
        /// <summary>
        /// 更新模板
        /// </summary>
        Task<MessageTemplate> UpdateAsync(MessageTemplate template);
        
        /// <summary>
        /// 删除模板
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
    }


}