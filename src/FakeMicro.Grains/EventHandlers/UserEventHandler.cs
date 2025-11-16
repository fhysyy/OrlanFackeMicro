using DotNetCore.CAP;
using FakeMicro.Interfaces.Events;
using Microsoft.Extensions.Logging;

namespace FakeMicro.Grains.EventHandlers
{
    /// <summary>
    /// 用户事件处理器 - 在Grain层处理事件
    /// </summary>
    public class UserEventHandler : IUserEvents
    {
        private readonly ILogger<UserEventHandler> _logger;

        public UserEventHandler(ILogger<UserEventHandler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 处理用户创建事件
        /// </summary>
        [CapSubscribe("user.created")]
        public async Task HandleUserCreatedAsync(UserCreatedEvent @event)
        {
            _logger.LogInformation("Grain层收到用户创建事件: UserId={UserId}, Username={Username}, Email={Email}", 
                @event.UserId, @event.Username, @event.Email);

            // Grain层业务逻辑，比如：
            // 1. 更新Grain状态
            // 2. 触发其他Grain操作
            // 3. 更新缓存
            // 4. 发送通知到其他微服务

            await Task.CompletedTask;
        }

        /// <summary>
        /// 处理用户更新事件
        /// </summary>
        [CapSubscribe("user.updated")]
        public async Task HandleUserUpdatedAsync(UserUpdatedEvent @event)
        {
            _logger.LogInformation("Grain层收到用户更新事件: UserId={UserId}, Username={Username}, Email={Email}", 
                @event.UserId, @event.Username, @event.Email);

            // Grain层业务逻辑，比如：
            // 1. 更新Grain状态
            // 2. 同步用户信息
            // 3. 记录审计日志

            await Task.CompletedTask;
        }

        /// <summary>
        /// 处理用户删除事件
        /// </summary>
        [CapSubscribe("user.deleted")]
        public async Task HandleUserDeletedAsync(UserDeletedEvent @event)
        {
            _logger.LogInformation("Grain层收到用户删除事件: UserId={UserId}", @event.UserId);

            // Grain层业务逻辑，比如：
            // 1. 清理Grain状态
            // 2. 删除相关数据
            // 3. 通知其他Grain

            await Task.CompletedTask;
        }
    }
}