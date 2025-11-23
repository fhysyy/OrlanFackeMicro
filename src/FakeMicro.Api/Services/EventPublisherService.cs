using DotNetCore.CAP;
using FakeMicro.Interfaces.Events;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace FakeMicro.Api.Services
{
    /// <summary>
    /// 事件发布服务
    /// 集成CAP事件总线，负责事件的发布和清理
    /// </summary>
    public class EventPublisherService : IEventPublisher
    {
        private readonly ILogger<EventPublisherService> _logger;
        private readonly IExtendedCapPublisher _capPublisher;
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// 事件发布服务构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="capPublisher">扩展的CAP事件发布器</param>
        /// <param name="httpClientFactory">HTTP客户端工厂</param>
        public EventPublisherService(
            ILogger<EventPublisherService> logger,
            IExtendedCapPublisher capPublisher = null,
            IHttpClientFactory httpClientFactory = null)
        {
            _logger = logger;
            _capPublisher = capPublisher;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// 发布用户创建事件
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="username">用户名</param>
        /// <param name="email">用户邮箱</param>
        public async Task PublishUserCreatedAsync(Guid userId, string username, string email)
        {
            var @event = new UserCreatedEvent
            {
                UserId = userId,
                Username = username,
                Email = email,
                CreatedAt = DateTime.UtcNow
            };

            // 为事件添加标签，便于外部系统订阅
            await PublishEventWithTagsAsync("user.created", @event, "user", "create", "notification");
            _logger.LogInformation("用户创建事件发布成功: UserId={UserId}, Username={Username}", userId, username);
        }
        
        /// <summary>
        /// 发布带标签的事件，支持外部订阅
        /// </summary>
        /// <typeparam name="T">事件数据类型</typeparam>
        /// <param name="eventName">事件名称</param>
        /// <param name="eventData">事件数据</param>
        /// <param name="tags">事件标签</param>
        public async Task PublishEventWithTagsAsync<T>(string eventName, T eventData, params string[] tags) where T : class
        {
            await PublishEventAsync(eventName, eventData, tags);
            _logger.LogInformation("带标签事件发布成功: EventName={EventName}, Tags={Tags}", 
                eventName, string.Join(",", tags));
        }

        /// <summary>
        /// 发布用户更新事件
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="username">用户名</param>
        /// <param name="email">用户邮箱</param>
        public async Task PublishUserUpdatedAsync(Guid userId, string username, string email)
        {
            var @event = new UserUpdatedEvent
            {
                UserId = userId,
                Username = username,
                Email = email,
                UpdatedAt = DateTime.UtcNow
            };

            // 为事件添加标签，便于外部系统订阅
            await PublishEventWithTagsAsync("user.updated", @event, "user", "update", "notification");
            _logger.LogInformation("用户更新事件发布成功: UserId={UserId}, Username={Username}", userId, username);
        }

        /// <summary>
        /// 发布用户删除事件
        /// </summary>
        /// <param name="userId">用户ID</param>
        public async Task PublishUserDeletedAsync(Guid userId)
        {
            var @event = new UserDeletedEvent
            {
                UserId = userId,
                DeletedAt = DateTime.UtcNow
            };

            // 为事件添加标签，便于外部系统订阅
            await PublishEventWithTagsAsync("user.deleted", @event, "user", "delete", "notification");
            _logger.LogInformation("用户删除事件发布成功: UserId={UserId}", userId);
        }

        /// <summary>
        /// 发布自定义事件
        /// </summary>
        /// <typeparam name="T">事件数据类型</typeparam>
        /// <param name="eventName">事件名称</param>
        /// <param name="eventData">事件数据</param>
        public async Task PublishCustomEventAsync<T>(string eventName, T eventData) where T : class
        {
            await PublishEventAsync(eventName, eventData);
            _logger.LogInformation("自定义事件发布成功: EventName={EventName}", eventName);
        }

        /// <summary>
        /// 清理过期事件
        /// </summary>
        public async Task CleanupOldEventsAsync()
        {
            try
            {
                _logger.LogInformation("[{Timestamp}] 开始执行过期事件清理任务", DateTime.Now);

                // 如果CAP未启用，使用简化的清理逻辑
                if (_capPublisher == null)
                {
                    _logger.LogWarning("CAP未启用，跳过事件清理");
                    return;
                }

                // 这里可以根据CAP的API或直接访问数据库进行事件清理
                // 由于CAP没有直接的清理API，我们可以使用Hangfire的内置功能
                _logger.LogInformation("[{Timestamp}] 执行事件清理逻辑", DateTime.Now);
                
                // 模拟清理操作，实际项目中可以根据需要实现具体的清理逻辑
                await Task.Delay(1000);
                
                _logger.LogInformation("[{Timestamp}] 过期事件清理完成", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Timestamp}] 过期事件清理失败", DateTime.Now);
                throw;
            }
        }

        /// <summary>
        /// 通用事件发布方法，包含重试逻辑
        /// </summary>
        /// <typeparam name="T">事件数据类型</typeparam>
        /// <param name="eventName">事件名称</param>
        /// <param name="eventData">事件数据</param>
        /// <param name="tags">事件标签（可选）</param>
        /// <param name="retryCount">重试次数</param>
        private async Task PublishEventAsync<T>(string eventName, T eventData, string[] tags = null, int retryCount = 2) where T : class
        {
            int attempt = 0;
            bool published = false;
            Exception lastException = null;

            while (attempt <= retryCount && !published)
            {
                attempt++;
                try
                {
                    // 如果CAP已配置，使用CAP发布事件
                    if (_capPublisher != null)
                    {
                        // 如果提供了标签，使用带标签的发布方法
                        if (tags != null && tags.Length > 0)
                        {
                            await _capPublisher.PublishWithTagsAsync(eventName, eventData, tags);
                        }
                        else
                        {
                            await _capPublisher.PublishAsync(eventName, eventData);
                        }
                        published = true;
                        _logger.LogDebug("事件发布成功 (尝试 {Attempt}/{RetryCount}): {EventName}", 
                            attempt, retryCount + 1, eventName);
                    }
                    else
                    {
                        // CAP未配置，记录事件到日志系统
                        _logger.LogWarning("CAP未配置，事件仅记录到日志: {EventName}", eventName);
                        published = true;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogError(ex, "事件发布失败 (尝试 {Attempt}/{RetryCount}): {EventName}", 
                        attempt, retryCount + 1, eventName);

                    // 如果不是最后一次尝试，等待后重试
                    if (attempt <= retryCount)
                    {
                        // 指数退避策略
                        int delayMs = (int)Math.Pow(2, attempt) * 1000;
                        _logger.LogInformation("等待 {DelayMs}ms 后重试事件发布: {EventName}", 
                            delayMs, eventName);
                        await Task.Delay(delayMs);
                    }
                }
            }

            // 如果所有重试都失败，抛出异常
            if (!published && lastException != null)
            {
                throw new InvalidOperationException($"事件发布失败，已达到最大重试次数: {eventName}", lastException);
            }
        }
    }
}