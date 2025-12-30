using System;
using System.Text.Json.Serialization;
using Orleans;

namespace FakeMicro.Entities
{
    /// <summary>
    /// 用户会话信息
    /// </summary>
    [GenerateSerializer]
    public class UserSession
    {
        [Id(0)]
        public string SessionId { get; set; } = string.Empty;
        [Id(1)]
        public DateTime LoginTime { get; set; } = DateTime.UtcNow;
        [Id(2)]
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
        [Id(3)]
        public string? IpAddress { get; set; }
        [Id(4)]
        public string? UserAgent { get; set; }
        [Id(5)]
        public bool IsCurrent { get; set; } = true;
    }
}