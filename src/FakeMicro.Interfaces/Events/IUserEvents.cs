using Orleans;

namespace FakeMicro.Interfaces.Events
{
    /// <summary>
    /// 用户事件契约接口
    /// </summary>
    public interface IUserEvents
    {
        /// <summary>
        /// 用户创建事件
        /// </summary>
        Task HandleUserCreatedAsync(UserCreatedEvent @event);

        /// <summary>
        /// 用户更新事件
        /// </summary>
        Task HandleUserUpdatedAsync(UserUpdatedEvent @event);

        /// <summary>
        /// 用户删除事件
        /// </summary>
        Task HandleUserDeletedAsync(UserDeletedEvent @event);
    }

    /// <summary>
    /// 用户创建事件
    /// </summary>
    [GenerateSerializer]
    public class UserCreatedEvent
    {
        [Id(0)] public Guid UserId { get; set; }
        [Id(1)] public string Username { get; set; } = string.Empty;
        [Id(2)] public string Email { get; set; } = string.Empty;
        [Id(3)] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 用户更新事件
    /// </summary>
    [GenerateSerializer]
    public class UserUpdatedEvent
    {
        [Id(0)] public Guid UserId { get; set; }
        [Id(1)] public string Username { get; set; } = string.Empty;
        [Id(2)] public string Email { get; set; } = string.Empty;
        [Id(3)] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 用户删除事件
    /// </summary>
    [GenerateSerializer]
    public class UserDeletedEvent
    {
        [Id(0)] public Guid UserId { get; set; }
        [Id(1)] public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 事件发布服务接口
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        /// 发布用户创建事件
        /// </summary>
        Task PublishUserCreatedAsync(Guid userId, string username, string email);

        /// <summary>
        /// 发布用户更新事件
        /// </summary>
        Task PublishUserUpdatedAsync(Guid userId, string username, string email);

        /// <summary>
        /// 发布用户删除事件
        /// </summary>
        Task PublishUserDeletedAsync(Guid userId);

        /// <summary>
        /// 发布自定义事件
        /// </summary>
        Task PublishCustomEventAsync<T>(string eventName, T eventData) where T : class;
        
        /// <summary>
        /// 发布带标签的事件，支持外部订阅
        /// </summary>
        /// <typeparam name="T">事件数据类型</typeparam>
        /// <param name="eventName">事件名称</param>
        /// <param name="eventData">事件数据</param>
        /// <param name="tags">事件标签</param>
        Task PublishEventWithTagsAsync<T>(string eventName, T eventData, params string[] tags) where T : class;
    }
}