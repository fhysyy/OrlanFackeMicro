using FakeMicro.Interfaces.Events;
using System;
using System.Threading.Tasks;

namespace FakeMicro.Tests.TestHelpers
{
    public class MemoryEventPublisher : IEventPublisher
    {
        public Task PublishUserCreatedAsync(Guid userId, string username, string email)
        {
            // 内存实现，仅记录事件，不做实际发布
            Console.WriteLine($"用户创建事件: UserId={userId}, Username={username}, Email={email}");
            return Task.CompletedTask;
        }

        public Task PublishUserUpdatedAsync(Guid userId, string username, string email)
        {
            Console.WriteLine($"用户更新事件: UserId={userId}, Username={username}, Email={email}");
            return Task.CompletedTask;
        }

        public Task PublishUserDeletedAsync(Guid userId)
        {
            Console.WriteLine($"用户删除事件: UserId={userId}");
            return Task.CompletedTask;
        }

        public Task PublishCustomEventAsync<T>(string eventName, T eventData) where T : class
        {
            Console.WriteLine($"自定义事件: EventName={eventName}, Data={eventData?.ToString()}");
            return Task.CompletedTask;
        }

        public Task PublishEventWithTagsAsync<T>(string eventName, T eventData, params string[] tags) where T : class
        {
            var tagsString = (tags != null && tags.Any()) ? string.Join(", ", tags) : "无标签";
            Console.WriteLine($"带标签的事件: EventName={eventName}, Data={eventData?.ToString()}, Tags={tagsString}");
            return Task.CompletedTask;
        }
    }
}