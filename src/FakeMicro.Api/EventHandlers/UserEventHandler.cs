using DotNetCore.CAP;
using FakeMicro.Interfaces.Events;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FakeMicro.Api.EventHandlers
{
    /// <summary>
    /// 用户事件处理器
    /// 处理CAP发布的用户相关事件
    /// </summary>
    public class UserEventHandler : ICapSubscribe
    {
        private readonly ILogger<UserEventHandler> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public UserEventHandler(ILogger<UserEventHandler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 处理用户创建事件
        /// </summary>
        /// <param name="@event">用户创建事件</param>
        [CapSubscribe("user.created", Group = "fake-micro-api")]
        public async Task HandleUserCreated(UserCreatedEvent @event)
        {
            var activity = new Activity("HandleUserCreated");
            activity.Start();
            
            try
            {
                _logger.LogInformation("[事件处理] 开始处理用户创建事件: UserId={UserId}, Username={Username}, Email={Email}, CreatedAt={CreatedAt}", 
                    @event.UserId, @event.Username, @event.Email, @event.CreatedAt);

                // 1. 验证事件数据完整性
                if (string.IsNullOrEmpty(@event.Username) || string.IsNullOrEmpty(@event.Email))
                {
                    _logger.LogWarning("[事件处理] 用户创建事件数据不完整: UserId={UserId}", @event.UserId);
                    return;
                }

                // 2. 执行事件处理逻辑
                // 这里可以添加实际的业务逻辑，如发送欢迎邮件、创建用户配置等
                await Task.Delay(500); // 模拟处理时间
                
                _logger.LogInformation("[事件处理] 用户创建事件处理完成: UserId={UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[事件处理] 用户创建事件处理失败: UserId={UserId}", @event.UserId);
                // 抛出异常让CAP重试，或者记录失败并继续
                throw; // CAP会自动重试失败的事件
            }
            finally
            {
                activity.Stop();
                _logger.LogDebug("[事件处理] 用户创建事件处理耗时: {Duration}ms", activity.Duration.TotalMilliseconds);
            }
        }

        /// <summary>
        /// 处理用户更新事件
        /// </summary>
        /// <param name="@event">用户更新事件</param>
        [CapSubscribe("user.updated", Group = "fake-micro-api")]
        public async Task HandleUserUpdated(UserUpdatedEvent @event)
        {
            var activity = new Activity("HandleUserUpdated");
            activity.Start();
            
            try
            {
                _logger.LogInformation("[事件处理] 开始处理用户更新事件: UserId={UserId}, Username={Username}, Email={Email}, UpdatedAt={UpdatedAt}", 
                    @event.UserId, @event.Username, @event.Email, @event.UpdatedAt);

                // 执行用户更新后的业务逻辑
                // 例如：更新缓存、同步到其他服务、记录审计日志等
                await Task.Delay(300); // 模拟处理时间
                
                _logger.LogInformation("[事件处理] 用户更新事件处理完成: UserId={UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[事件处理] 用户更新事件处理失败: UserId={UserId}", @event.UserId);
                throw;
            }
            finally
            {
                activity.Stop();
                _logger.LogDebug("[事件处理] 用户更新事件处理耗时: {Duration}ms", activity.Duration.TotalMilliseconds);
            }
        }

        /// <summary>
        /// 处理用户删除事件
        /// </summary>
        /// <param name="@event">用户删除事件</param>
        [CapSubscribe("user.deleted", Group = "fake-micro-api")]
        public async Task HandleUserDeleted(UserDeletedEvent @event)
        {
            var activity = new Activity("HandleUserDeleted");
            activity.Start();
            
            try
            {
                _logger.LogInformation("[事件处理] 开始处理用户删除事件: UserId={UserId}, DeletedAt={DeletedAt}", 
                    @event.UserId, @event.DeletedAt);

                // 执行用户删除后的清理逻辑
                // 例如：清理相关数据、删除缓存、通知其他服务等
                await Task.Delay(200); // 模拟处理时间
                
                _logger.LogInformation("[事件处理] 用户删除事件处理完成: UserId={UserId}", @event.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[事件处理] 用户删除事件处理失败: UserId={UserId}", @event.UserId);
                throw;
            }
            finally
            {
                activity.Stop();
                _logger.LogDebug("[事件处理] 用户删除事件处理耗时: {Duration}ms", activity.Duration.TotalMilliseconds);
            }
        }
    }
}