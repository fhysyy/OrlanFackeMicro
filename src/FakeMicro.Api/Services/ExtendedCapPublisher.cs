using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Microsoft.Extensions.Logging;

namespace FakeMicro.Api.Services
{
    public class ExtendedCapPublisher : IExtendedCapPublisher
    {
        private readonly ICapPublisher _capPublisher;
        private readonly ILogger<ExtendedCapPublisher> _logger;

        public ExtendedCapPublisher(ICapPublisher capPublisher, ILogger<ExtendedCapPublisher> logger)
        {
            _capPublisher = capPublisher;
            _logger = logger;
        }

        // ICapPublisher 接口属性
        public IServiceProvider ServiceProvider => _capPublisher.ServiceProvider;
        public ICapTransaction Transaction { get => _capPublisher.Transaction; set => _capPublisher.Transaction = value; }

        // IExtendedCapPublisher 接口方法实现
        public Task PublishWithTagsAsync<T>(string eventName, T eventData, params string[] tags) where T : class
        {
            try
            {
                if (tags?.Length > 0)
                {
                    var headers = new Dictionary<string, string>();
                    headers["tags"] = string.Join(",", tags);
                    return _capPublisher.PublishAsync(eventName, eventData, headers, CancellationToken.None);
                }
                else
                {
                    return _capPublisher.PublishAsync(eventName, eventData, (string)null, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布带标签的消息时发生错误: {Message}", ex.Message);
                throw;
            }
        }

        public Task RegisterExternalSubscriberAsync(string eventName, string callbackUrl, string description = null)
        {
            _logger.LogInformation("注册外部订阅者: {EventName}, {CallbackUrl}, {Description}", eventName, callbackUrl, description);
            // TODO: 实现外部订阅者注册逻辑
            return Task.CompletedTask;
        }

        public Task RemoveExternalSubscriberAsync(string eventName, string callbackUrl)
        {
            _logger.LogInformation("移除外部订阅者: {EventName}, {CallbackUrl}", eventName, callbackUrl);
            // TODO: 实现外部订阅者移除逻辑
            return Task.CompletedTask;
        }

        // ICapPublisher 接口方法实现
        public Task PublishAsync<T>(string name, T contentObj, string callbackName, CancellationToken cancellationToken)
        {
            return _capPublisher.PublishAsync(name, contentObj, callbackName, cancellationToken);
        }

        public Task PublishAsync<T>(string name, T contentObj, IDictionary<string, string> headers, CancellationToken cancellationToken)
        {
            return _capPublisher.PublishAsync(name, contentObj, headers, cancellationToken);
        }

        public void Publish<T>(string name, T contentObj, string callbackName)
        {
            _capPublisher.Publish(name, contentObj, callbackName);
        }

        public void Publish<T>(string name, T contentObj, IDictionary<string, string> headers)
        {
            _capPublisher.Publish(name, contentObj, headers);
        }

        public Task PublishDelayAsync<T>(TimeSpan delayTime, string name, T contentObj, IDictionary<string, string> headers, CancellationToken cancellationToken)
        {
            return _capPublisher.PublishDelayAsync(delayTime, name, contentObj, headers, cancellationToken);
        }

        public Task PublishDelayAsync<T>(TimeSpan delayTime, string name, T contentObj, string callbackName, CancellationToken cancellationToken)
        {
            return _capPublisher.PublishDelayAsync(delayTime, name, contentObj, callbackName, cancellationToken);
        }

        public void PublishDelay<T>(TimeSpan delayTime, string name, T contentObj, IDictionary<string, string> headers)
        {
            _capPublisher.PublishDelay(delayTime, name, contentObj, headers);
        }

        public void PublishDelay<T>(TimeSpan delayTime, string name, T contentObj, string callbackName)
        {
            _capPublisher.PublishDelay(delayTime, name, contentObj, callbackName);
        }
    }
}