using System;

namespace FakeMicro.Api.Events
{
    /// <summary>
    /// 用户创建事件
    /// </summary>
    public class UserCreatedEvent
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 用户更新事件
    /// </summary>
    public class UserUpdatedEvent
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 用户删除事件
    /// </summary>
    public class UserDeletedEvent
    {
        public Guid UserId { get; set; }
        public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
    }
}