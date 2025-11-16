using FakeMicro.Entities.Enums;
using Orleans;

namespace FakeMicro.Interfaces.Models
{
    /// <summary>
    /// 消息数据传输对象
    /// </summary>
    [GenerateSerializer]
    public class MessageDto
    {
        [Id(0)] public long Id { get; set; }
        [Id(1)] public long SenderId { get; set; }
        [Id(2)] public long? ReceiverId { get; set; }
        [Id(3)] public string? ReceiverEmail { get; set; }
        [Id(4)] public string? ReceiverPhone { get; set; }
        [Id(5)] public string Title { get; set; } = string.Empty;
        [Id(6)] public string Content { get; set; } = string.Empty;
        [Id(7)] public MessageType MessageType { get; set; }
        [Id(8)] public MessageChannel Channel { get; set; }
        [Id(9)] public MessageStatus Status { get; set; }
        [Id(10)] public DateTime? SentAt { get; set; }
        [Id(11)] public DateTime? DeliveredAt { get; set; }
        [Id(12)] public DateTime? ReadAt { get; set; }
        [Id(13)] public DateTime? FailedAt { get; set; }
        [Id(14)] public int RetryCount { get; set; }
        [Id(15)] public string? ErrorMessage { get; set; }
        [Id(16)] public Dictionary<string, object>? Metadata { get; set; }
        [Id(17)] public DateTime? ScheduledAt { get; set; }
        [Id(18)] public DateTime? ExpiresAt { get; set; }
        [Id(19)] public DateTime CreatedAt { get; set; }
        [Id(20)] public DateTime UpdatedAt { get; set; }
    }
}