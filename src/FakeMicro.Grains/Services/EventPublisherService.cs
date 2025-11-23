using DotNetCore.CAP;
using FakeMicro.Interfaces.Events;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

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

            // 为事件添加标签，便于外部系统订阅
            await PublishEventWithTagsAsync("user.created", @event, "user", "create", "notification");
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

            // 为事件添加标签，便于外部系统订阅
            await PublishEventWithTagsAsync("user.updated", @event, "user", "update", "notification");
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

            // 为事件添加标签，便于外部系统订阅
            await PublishEventWithTagsAsync("user.deleted", @event, "user", "delete", "notification");
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

        /// <summary>
        /// 发布带标签的事件，支持外部订阅
        /// </summary>
        public async Task PublishEventWithTagsAsync<T>(string eventName, T eventData, params string[] tags) where T : class
        {
            // 在元数据中添加标签信息
            var headers = new Dictionary<string, string>
            {
                { "Event-Tags", string.Join(",", tags) }
            };

            await _capPublisher.PublishAsync(eventName, eventData, headers);
            _logger.LogInformation("已发布带标签事件: EventName={EventName}, Tags={Tags}", 
                eventName, string.Join(",", tags));
        }
    }
}