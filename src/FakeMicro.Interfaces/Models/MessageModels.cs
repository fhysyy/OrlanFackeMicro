using FakeMicro.Entities.Enums;
using Orleans;
using System.ComponentModel.DataAnnotations;

namespace FakeMicro.Interfaces.Models
{
    /// <summary>
    /// 发送消息请求
    /// </summary>
    [GenerateSerializer]
    public class SendMessageRequest
    {
        [Id(0)]
        public long? ReceiverId { get; set; }
        
        [Id(1)]
        public string? ReceiverEmail { get; set; }
        
        [Id(2)]
        public string? ReceiverPhone { get; set; }
        
        [Id(3)]
        [Required(ErrorMessage = "消息标题不能为空")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Id(4)]
        [Required(ErrorMessage = "消息内容不能为空")]
        public string Content { get; set; } = string.Empty;
        
        [Id(5)]
        public MessageType MessageType { get; set; }
        
        [Id(6)]
        public MessageChannel Channel { get; set; }
        
        [Id(7)]
        public DateTime? ScheduledAt { get; set; }
        
        [Id(8)]
        public DateTime? ExpiresAt { get; set; }
        
        [Id(9)]
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// 控制器消息请求
    /// </summary>
    [GenerateSerializer]
    public class ControllerMessageRequest
    {
        [Id(0)]
        public long SenderId { get; set; }
        
        [Id(1)]
        public long ReceiverId { get; set; }
        
        [Id(2)]
        public string? ReceiverEmail { get; set; }
        
        [Id(3)]
        public string? ReceiverPhone { get; set; }
        
        [Id(4)]
        public string Title { get; set; } = string.Empty;
        
        [Id(5)]
        public string Content { get; set; } = string.Empty;
        
        [Id(6)]
        public MessageType MessageType { get; set; }
        
        [Id(7)]
        public MessageChannel Channel { get; set; }
        
        [Id(8)]
        public DateTime? ScheduledAt { get; set; }
        
        [Id(9)]
        public DateTime? ExpiresAt { get; set; }
        
        [Id(10)]
        public Dictionary<string, string>? Metadata { get; set; }
    }

    /// <summary>
    /// 消息响应
    /// </summary>
    [GenerateSerializer]
    public class MessageResponse
    {
        [Id(0)]
        public bool Success { get; set; }
        
        [Id(1)]
        public string MessageId { get; set; } = string.Empty;
        
        [Id(2)]
        public string Status { get; set; } = string.Empty;
        
        [Id(3)]
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 消息统计请求
    /// </summary>
    [GenerateSerializer]
    public class MessageStatisticsRequest
    {
        [Id(0)]
        public long UserId { get; set; }
        
        [Id(1)]
        public DateTime? StartDate { get; set; }
        
        [Id(2)]
        public DateTime? EndDate { get; set; }
    }

    /// <summary>
    /// 批量消息响应
    /// </summary>
    [GenerateSerializer]
    public class BatchMessageResponse
    {
        [Id(0)]
        public bool Success { get; set; }
        
        [Id(1)]
        public List<Guid> MessageIds { get; set; } = [];
        
        [Id(2)]
        public int TotalCount { get; set; }
        
        [Id(3)]
        public int FailedCount { get; set; }
    }

    /// <summary>
    /// 处理待发送消息响应
    /// </summary>
    [GenerateSerializer]
    public class ProcessPendingResponse
    {
        [Id(0)]
        public bool Success { get; set; }
        
        [Id(1)]
        public int ProcessedCount { get; set; }
        
        [Id(2)]
        public string Message { get; set; } = string.Empty;
    }
}