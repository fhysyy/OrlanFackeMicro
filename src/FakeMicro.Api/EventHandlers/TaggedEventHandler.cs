using DotNetCore.CAP;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FakeMicro.Api.EventHandlers
{
    /// <summary>
    /// 带标签的事件处理器
    /// 用于处理支持外部订阅的标签事件
    /// </summary>
    public class TaggedEventHandler : ICapSubscribe
    {
        private readonly ILogger<TaggedEventHandler> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        public TaggedEventHandler(ILogger<TaggedEventHandler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 处理所有带notification标签的事件
        /// </summary>
        /// <param name="eventData">事件数据</param>
        /// <param name="headers">事件头信息</param>
        [CapSubscribe("*")]
        public async Task HandleTaggedEventAsync(dynamic eventData, CapHeader headers)
        {
            try
            {
                // 获取事件名称
                var eventName = headers["cap-msg-name"];
                
                // 检查是否包含标签信息
                if (headers.TryGetValue("Event-Tags", out var tagsValue))
                {
                    var tags = tagsValue.Split(',');
                    _logger.LogInformation("接收到带标签事件: {EventName}, 标签: {Tags}", 
                        eventName, tagsValue);

                    // 可以根据不同的标签执行不同的处理逻辑
                    foreach (var tag in tags)
                    {
                        switch (tag.Trim())
                        {
                            case "notification":
                                await HandleNotificationEventAsync(eventName, eventData);
                                break;
                            case "audit":
                                await HandleAuditEventAsync(eventName, eventData);
                                break;
                            default:
                                _logger.LogInformation("处理带标签 {Tag} 的事件: {EventName}", 
                                    tag, eventName);
                                break;
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("接收到无标签事件: {EventName}", eventName);
                }

                // 模拟异步处理
                await Task.CompletedTask;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "处理带标签事件时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 处理通知类事件
        /// </summary>
        private async Task HandleNotificationEventAsync(string eventName, dynamic eventData)
        {
            _logger.LogInformation("处理通知类事件: {EventName}", eventName);
            // 这里可以实现通知逻辑，如发送邮件、推送消息等
            await Task.CompletedTask;
        }

        /// <summary>
        /// 处理审计类事件
        /// </summary>
        private async Task HandleAuditEventAsync(string eventName, dynamic eventData)
        {
            _logger.LogInformation("处理审计类事件: {EventName}", eventName);
            // 这里可以实现审计日志记录逻辑
            await Task.CompletedTask;
        }
    }
}