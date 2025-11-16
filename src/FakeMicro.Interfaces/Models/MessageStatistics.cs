using FakeMicro.Entities.Enums;
using Orleans;

namespace FakeMicro.Interfaces.Models
{
    /// <summary>
    /// 消息统计信息
    /// </summary>
    [GenerateSerializer]
    public class MessageStatistics
    {
        [Id(0)] public int TotalMessages { get; set; }
        [Id(1)] public int SentMessages { get; set; }
        [Id(2)] public int ReadMessages { get; set; }
        [Id(3)] public int FailedMessages { get; set; }
        [Id(4)] public int PendingMessages { get; set; }
        [Id(5)] public DateTime StartTime { get; set; }
        [Id(6)] public DateTime EndTime { get; set; }
        
        [Id(7)] public Dictionary<MessageType, int> TypeStatistics { get; set; } = new();
        [Id(8)] public Dictionary<MessageChannel, int> ChannelStatistics { get; set; } = new();
        [Id(9)] public Dictionary<MessageStatus, int> StatusStatistics { get; set; } = new();
    }
}