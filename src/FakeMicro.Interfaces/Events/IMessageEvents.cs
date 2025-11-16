using FakeMicro.Interfaces;
using FakeMicro.Entities.Enums;
using Orleans;

namespace FakeMicro.Interfaces.Events
{
    /// <summary>
    /// 消息事件契约接口
    /// </summary>
    public interface IMessageEvents
    {
        /// <summary>
        /// 消息发送事件
        /// </summary>
        Task HandleMessageSentAsync(MessageSentEvent @event);

        /// <summary>
        /// 消息投递事件
        /// </summary>
        Task HandleMessageDeliveredAsync(MessageDeliveredEvent @event);

        /// <summary>
        /// 消息阅读事件
        /// </summary>
        Task HandleMessageReadAsync(MessageReadEvent @event);

        /// <summary>
        /// 消息发送失败事件
        /// </summary>
        Task HandleMessageFailedAsync(MessageFailedEvent @event);
    }

    /// <summary>
    /// 消息发送事件
    /// </summary>
    [GenerateSerializer]
    public class MessageSentEvent
    {
        [Id(0)] public long MessageId { get; set; }
        [Id(1)] public long SenderId { get; set; }
        [Id(2)] public long? ReceiverId { get; set; }
        [Id(3)] public string ReceiverEmail { get; set; } = string.Empty;
        [Id(4)] public string ReceiverPhone { get; set; } = string.Empty;
        [Id(5)] public string Title { get; set; } = string.Empty;
        [Id(6)] public string Content { get; set; } = string.Empty;
        [Id(7)] public MessageType MessageType { get; set; }
        [Id(8)] public MessageChannel Channel { get; set; }
        [Id(9)] public DateTime SentAt { get; set; } = DateTime.UtcNow;
        [Id(10)] public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// 消息投递事件
    /// </summary>
    [GenerateSerializer]
    public class MessageDeliveredEvent
    {
        [Id(0)] public long MessageId { get; set; }
        [Id(1)] public DateTime DeliveredAt { get; set; } = DateTime.UtcNow;
        [Id(2)] public string DeliveryInfo { get; set; } = string.Empty;
    }

    /// <summary>
    /// 消息阅读事件
    /// </summary>
    [GenerateSerializer]
    public class MessageReadEvent
    {
        [Id(0)] public long MessageId { get; set; }
        [Id(1)] public DateTime ReadAt { get; set; } = DateTime.UtcNow;
        [Id(2)] public string ReadBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// 消息发送失败事件
    /// </summary>
    [GenerateSerializer]
    public class MessageFailedEvent
    {
        [Id(0)] public Guid MessageId { get; set; }
        [Id(1)] public string ErrorMessage { get; set; } = string.Empty;
        [Id(2)] public DateTime FailedAt { get; set; } = DateTime.UtcNow;
        [Id(3)] public int RetryCount { get; set; }
    }


}