using DotNetCore.CAP;
using FakeMicro.Interfaces.Events;
using Microsoft.Extensions.Logging;

namespace FakeMicro.Grains.Services
{
    /// <summary>
    /// 事件发布服务实现
    /// </summary>
    public class EventPublisherService : IEventPublisher
    {
        private readonly ICapPublisher _capPublisher;
        private readonly ILogger<EventPublisherService> _logger;

        public EventPublisherService(ICapPublisher capPublisher, ILogger<EventPublisherService> logger)
        {
            _capPublisher = capPublisher;
            _logger = logger;
        }

        /// <summary>
        /// 发布用户创建事件
        /// </summary>
        public async Task PublishUserCreatedAsync(Guid userId, string username, string email)
        {
            var @event = new UserCreatedEvent
            {
                UserId = userId,
                Username = username,
                Email = email
            };

            await _capPublisher.PublishAsync("user.created", @event);
            _logger.LogInformation("已发布用户创建事件: UserId={UserId}", userId);
        }

        /// <summary>
        /// 发布用户更新事件
        /// </summary>
        public async Task PublishUserUpdatedAsync(Guid userId, string username, string email)
        {
            var @event = new UserUpdatedEvent
            {
                UserId = userId,
                Username = username,
                Email = email
            };

            await _capPublisher.PublishAsync("user.updated", @event);
            _logger.LogInformation("已发布用户更新事件: UserId={UserId}", userId);
        }

        /// <summary>
        /// 发布用户删除事件
        /// </summary>
        public async Task PublishUserDeletedAsync(Guid userId)
        {
            var @event = new UserDeletedEvent
            {
                UserId = userId
            };

            await _capPublisher.PublishAsync("user.deleted", @event);
            _logger.LogInformation("已发布用户删除事件: UserId={UserId}", userId);
        }

        /// <summary>
        /// 发布自定义事件
        /// </summary>
        public async Task PublishCustomEventAsync<T>(string eventName, T eventData) where T : class
        {
            await _capPublisher.PublishAsync(eventName, eventData);
            _logger.LogInformation("已发布自定义事件: EventName={EventName}", eventName);
        }
    }
}