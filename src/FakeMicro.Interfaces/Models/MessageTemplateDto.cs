using FakeMicro.Entities.Enums;
using Orleans;

namespace FakeMicro.Interfaces.Models
{
    /// <summary>
    /// 消息模板数据传输对象
    /// </summary>
    [GenerateSerializer]
    public class MessageTemplateDto
    {
        [Id(0)] public long Id { get; set; }
        [Id(1)] public string Name { get; set; } = string.Empty;
        [Id(2)] public string Code { get; set; } = string.Empty;
        [Id(3)] public string Title { get; set; } = string.Empty;
        [Id(4)] public string Content { get; set; } = string.Empty;
        [Id(5)] public MessageType MessageType { get; set; }
        [Id(6)] public Dictionary<string, string> Variables { get; set; } = new();
        [Id(7)] public bool IsEnabled { get; set; }
        [Id(8)] public DateTime CreatedAt { get; set; }
        [Id(9)] public DateTime UpdatedAt { get; set; }
    }
}