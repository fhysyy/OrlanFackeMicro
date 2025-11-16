using FakeMicro.Interfaces.Events;
using FakeMicro.Entities.Enums;
using FakeMicro.Interfaces.Models;
using Orleans;

namespace FakeMicro.Interfaces
{
    /// <summary>
    /// 消息Grain接口
    /// </summary>
    public interface IMessageGrain : IGrainWithStringKey
    {
        /// <summary>
        /// 发送消息
        /// </summary>
        Task<MessageResult> SendMessageAsync(MessageRequest request);
        
        /// <summary>
        /// 获取消息详情
        /// </summary>
        Task<MessageDto?> GetMessageAsync();
        
        /// <summary>
        /// 更新消息状态
        /// </summary>
        Task<bool> UpdateStatusAsync(MessageStatus status, string? errorMessage = null);
        
        /// <summary>
        /// 标记消息为已投递
        /// </summary>
        Task<bool> MarkAsDeliveredAsync(string deliveryInfo);
        
        /// <summary>
        /// 标记消息为已阅读
        /// </summary>
        Task<bool> MarkAsReadAsync(string readBy);
        
        /// <summary>
        /// 获取消息统计
        /// </summary>
        Task<MessageStatistics> GetStatisticsAsync();
    }

    /// <summary>
    /// 消息发送请求
    /// </summary>
    [Orleans.GenerateSerializer]
    public class MessageRequest
    {
        [Orleans.Id(0)]
        public long SenderId { get; set; }
        [Orleans.Id(1)]
        public long? ReceiverId { get; set; }
        [Orleans.Id(2)]
        public string? ReceiverEmail { get; set; }
        [Orleans.Id(3)]
        public string? ReceiverPhone { get; set; }
        [Orleans.Id(4)]
        public string Title { get; set; } = string.Empty;
        [Orleans.Id(5)]
        public string Content { get; set; } = string.Empty;
        [Orleans.Id(6)]
        public MessageType MessageType { get; set; }
        [Orleans.Id(7)]
        public MessageChannel Channel { get; set; }
        [Orleans.Id(8)]
        public DateTime? ScheduledAt { get; set; }
        [Orleans.Id(9)]
        public DateTime? ExpiresAt { get; set; }
        [Orleans.Id(10)]
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// 消息发送结果
    /// </summary>
    [Orleans.GenerateSerializer]
    public class MessageResult
    {
        [Orleans.Id(0)]
        public bool Success { get; set; }
        [Orleans.Id(1)]
        public long MessageId { get; set; }
        [Orleans.Id(2)]
        public string? ErrorMessage { get; set; }
        [Orleans.Id(3)]
        public MessageStatus Status { get; set; }
    }

    /// <summary>
    /// 消息模板Grain接口
    /// </summary>
    public interface IMessageTemplateGrain : IGrainWithStringKey
    {
        /// <summary>
        /// 获取模板详情
        /// </summary>
        Task<MessageTemplateDto?> GetTemplateAsync();
        
        /// <summary>
        /// 渲染模板
        /// </summary>
        Task<string> RenderTemplateAsync(Dictionary<string, object> variables);
        
        /// <summary>
        /// 验证模板变量
        /// </summary>
        Task<bool> ValidateVariablesAsync(Dictionary<string, object> variables);
    }

    /// <summary>
    /// 消息发送服务Grain接口
    /// </summary>
    public interface IMessageServiceGrain : IGrainWithGuidKey
    {
        /// <summary>
        /// 批量发送消息
        /// </summary>
        Task<BatchMessageResult> SendBatchMessagesAsync(List<MessageRequest> requests);
        
        /// <summary>
        /// 处理待发送消息
        /// </summary>
        Task<int> ProcessPendingMessagesAsync(int batchSize = 100);
        
        /// <summary>
        /// 清理过期消息
        /// </summary>
        Task<int> CleanupExpiredMessagesAsync();
        
        /// <summary>
        /// 获取系统消息统计
        /// </summary>
        Task<SystemMessageStatistics> GetSystemStatisticsAsync();
    }

    /// <summary>
    /// 批量消息发送结果
    /// </summary>
    [Orleans.GenerateSerializer]
    public class BatchMessageResult
    {
        [Orleans.Id(0)]
        public int Total { get; set; }
        [Orleans.Id(1)]
        public int Success { get; set; }
        [Orleans.Id(2)]
        public int Failed { get; set; }
        [Orleans.Id(3)]
        public List<MessageResult> Results { get; set; } = new();
        [Orleans.Id(4)]
        public TimeSpan ProcessingTime { get; set; }
    }



    /// <summary>
    /// 系统消息统计
    /// </summary>
    [Orleans.GenerateSerializer]
    public class SystemMessageStatistics
    {
        [Orleans.Id(0)]
        public int TotalMessages { get; set; }
        [Orleans.Id(1)]
        public int MessagesToday { get; set; }
        [Orleans.Id(2)]
        public int MessagesThisWeek { get; set; }
        [Orleans.Id(3)]
        public int MessagesThisMonth { get; set; }
        [Orleans.Id(4)]
        public double AverageDeliveryTime { get; set; }
        [Orleans.Id(5)]
        public double SuccessRate { get; set; }
        [Orleans.Id(6)]
        public Dictionary<MessageType, int> TypeDistribution { get; set; } = new();
        [Orleans.Id(7)]
        public Dictionary<MessageChannel, int> ChannelDistribution { get; set; } = new();
    }
}