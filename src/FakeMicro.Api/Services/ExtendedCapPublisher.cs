using DotNetCore.CAP;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Threading;
using System.Reflection;
using Serilog;

namespace FakeMicro.Api.Services
{
    /// <summary>
    /// 扩展的CAP发布器实现
    /// 支持外部订阅管理功能和标签化事件发布
    /// </summary>
    public class ExtendedCapPublisher : ICapPublisher, IExtendedCapPublisher
    {
        private readonly ICapPublisher _capPublisher;
        private readonly ILogger<ExtendedCapPublisher> _logger;
        private readonly HttpClient _httpClient;
        
        // 使用线程安全的集合替代手动锁
        private readonly ConcurrentDictionary<string, ConcurrentBag<ExternalSubscriber>> _externalSubscribers = 
            new ConcurrentDictionary<string, ConcurrentBag<ExternalSubscriber>>();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="capPublisher">CAP发布器</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="httpClient">HTTP客户端，用于通知外部订阅者</param>
        public ExtendedCapPublisher(ICapPublisher capPublisher, ILogger<ExtendedCapPublisher> logger, HttpClient httpClient)
        {
            _capPublisher = capPublisher ?? throw new ArgumentNullException(nameof(capPublisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            
            // 配置HttpClient超时时间
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        // ICapPublisher接口属性实现
        public IServiceProvider ServiceProvider => _capPublisher.ServiceProvider;
        
        public ICapTransaction Transaction => _capPublisher.Transaction;

        // ICapPublisher接口方法实现 - 异步方法
        public Task PublishAsync<T>(string name, T? content)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布事件: {EventName}", name);
            return _capPublisher.PublishAsync(name, content);
        }

        public Task PublishAsync<T>(string name, T? content, IDbTransaction? transaction)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布事件(带事务): {EventName}", name);
            return _capPublisher.PublishAsync(name, content, transaction);
        }

        public Task PublishAsync<T>(string name, T? content, IDictionary<string, string> headers = null)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布事件(带头部): {EventName}", name);
            return _capPublisher.PublishAsync(name, content, headers);
        }

        public Task PublishAsync<T>(string name, T? content, IDictionary<string, string> headers, IDbTransaction? transaction)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布事件(带事务和头部): {EventName}", name);
            return _capPublisher.PublishAsync(name, content, headers, transaction);
        }

        public Task PublishAsync<T>(string name, T? content, string? group, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布事件(带组和取消令牌): {EventName}", name);
            return _capPublisher.PublishAsync(name, content, group, cancellationToken);
        }

        public Task PublishAsync<T>(string name, T? content, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布事件(带取消令牌): {EventName}", name);
            return _capPublisher.PublishAsync(name, content, cancellationToken);
        }

        public Task PublishAsync<T>(string name, T? content, IDictionary<string, string> headers, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布事件(带头部和取消令牌): {EventName}", name);
            return _capPublisher.PublishAsync(name, content, headers, cancellationToken);
        }

        public Task PublishAsync<T>(string name, T? content, string group)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布事件(带组): {EventName}", name);
            return _capPublisher.PublishAsync(name, content, group);
        }
        
        public Task PublishAsync<T>(string name, T? content, string callbackName, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(callbackName);
            
            _logger.LogInformation("发布事件(带回调): {EventName}, 回调: {CallbackName}", name, callbackName);
            return _capPublisher.PublishAsync(name, content, callbackName, cancellationToken);
        }
        
        public Task PublishAsync<T>(string name, T? content, IDictionary<string, string> headers, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布事件(带头部和取消令牌): {EventName}", name);
            return _capPublisher.PublishAsync(name, content, headers, cancellationToken);
        }

        public Task PublishAsync<T>(string name, T? content, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布事件(带事务和取消令牌): {EventName}", name);
            return _capPublisher.PublishAsync(name, content, transaction, cancellationToken);
        }

        public Task PublishAsync<T>(string name, T? content, IDictionary<string, string> headers, IDbTransaction? transaction)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布事件(带事务和头部): {EventName}", name);
            return _capPublisher.PublishAsync(name, content, headers, transaction);
        }

        public Task PublishAsync<T>(string name, T? content, IDictionary<string, string> headers, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布事件(带事务、头部和取消令牌): {EventName}", name);
            return _capPublisher.PublishAsync(name, content, headers, transaction, cancellationToken);
        }

        // 字符串版本的PublishAsync方法
        public Task PublishAsync(string name, string? content)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布字符串事件: {EventName}", name);
            return _capPublisher.PublishAsync(name, content);
        }

        public Task PublishAsync(string name, string? content, IDbTransaction? transaction)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布字符串事件(带事务): {EventName}", name);
            return _capPublisher.PublishAsync(name, content, transaction);
        }

        public Task PublishAsync(string name, string? content, IDictionary<string, string> headers)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布字符串事件(带头部): {EventName}", name);
            return _capPublisher.PublishAsync(name, content, headers);
        }

        public Task PublishAsync(string name, string? content, IDictionary<string, string> headers, IDbTransaction? transaction)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布字符串事件(带事务和头部): {EventName}", name);
            return _capPublisher.PublishAsync(name, content, headers, transaction);
        }

        public Task PublishAsync(string name, string? content, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布字符串事件(带取消令牌): {EventName}", name);
            return _capPublisher.PublishAsync(name, content, cancellationToken);
        }

        public Task PublishAsync(string name, string? content, IDictionary<string, string> headers, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布字符串事件(带头部和取消令牌): {EventName}", name);
            return _capPublisher.PublishAsync(name, content, headers, cancellationToken);
        }

        public Task PublishAsync(string name, string? content, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布字符串事件(带事务和取消令牌): {EventName}", name);
            return _capPublisher.PublishAsync(name, content, transaction, cancellationToken);
        }

        public Task PublishAsync(string name, string? content, IDictionary<string, string> headers, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布字符串事件(带事务、头部和取消令牌): {EventName}", name);
            return _capPublisher.PublishAsync(name, content, headers, transaction, cancellationToken);
        }

        // 同步Publish方法实现
        public void Publish<T>(string name, T? content)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("同步发布事件: {EventName}", name);
            _capPublisher.Publish(name, content);
        }

        public void Publish<T>(string name, T? content, IDbTransaction? transaction)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("同步发布事件(带事务): {EventName}", name);
            _capPublisher.Publish(name, content, transaction);
        }

        public void Publish<T>(string name, T? content, IDictionary<string, string> headers)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("同步发布事件(带头部): {EventName}", name);
            _capPublisher.Publish(name, content, headers);
        }

        public void Publish<T>(string name, T? content, IDictionary<string, string> headers, IDbTransaction? transaction)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("同步发布事件(带事务和头部): {EventName}", name);
            _capPublisher.Publish(name, content, headers, transaction);
        }

        public void Publish<T>(string name, T? content, string? group)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("同步发布事件(带组): {EventName}", name);
            _capPublisher.Publish(name, content, group);
        }
        
        //public Task Publish<T>(string name, T content, string callbackName)
        //{
        //    ArgumentNullException.ThrowIfNull(name);
        //    ArgumentNullException.ThrowIfNull(callbackName);
            
        //    _logger.LogInformation("发布事件(带回调): {EventName}, 回调: {CallbackName}", name, callbackName);
        //    return _capPublisher.Publish(name, content, callbackName);
        //}
        


        // 字符串版本的同步Publish方法
        public void Publish(string name, string? content)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("同步发布字符串事件: {EventName}", name);
            _capPublisher.Publish(name, content);
        }

        public void Publish(string name, string? content, IDbTransaction? transaction)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("同步发布字符串事件(带事务): {EventName}", name);
            _capPublisher.Publish(name, content, transaction);
        }

        public void Publish(string name, string? content, IDictionary<string, string> headers)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("同步发布字符串事件(带头部): {EventName}", name);
            _capPublisher.Publish(name, content, headers);
        }

        public void Publish(string name, string? content, IDictionary<string, string> headers, IDbTransaction? transaction)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("同步发布字符串事件(带事务和头部): {EventName}", name);
            _capPublisher.Publish(name, content, headers, transaction);
        }

        // 延迟发布方法实现 - 注意参数顺序
        public Task PublishDelayAsync<T>(TimeSpan delay, string name, T? content)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布延迟事件: {EventName}, 延迟: {Delay}", name, delay);
            return _capPublisher.PublishDelayAsync(delay, name, content);
        }

        public Task PublishDelayAsync<T>(TimeSpan delay, string name, T? content, IDbTransaction? transaction)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布延迟事件(带事务): {EventName}, 延迟: {Delay}", name, delay);
            return _capPublisher.PublishDelayAsync(delay, name, content, transaction);
        }

        public Task PublishDelayAsync<T>(TimeSpan delay, string name, T? content, IDictionary<string, string> headers = null)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布延迟事件(带头部): {EventName}, 延迟: {Delay}", name, delay);
            return _capPublisher.PublishDelayAsync(delay, name, content, headers);
        }

        public Task PublishDelayAsync<T>(TimeSpan delay, string name, T? content, IDictionary<string, string> headers, IDbTransaction? transaction)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布延迟事件(带事务和头部): {EventName}, 延迟: {Delay}", name, delay);
            return _capPublisher.PublishDelayAsync(delay, name, content, headers, transaction);
        }

        public Task PublishDelayAsync<T>(TimeSpan delay, string name, T? content, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布延迟事件(带取消令牌): {EventName}, 延迟: {Delay}", name, delay);
            return _capPublisher.PublishDelayAsync(delay, name, content, cancellationToken);
        }

        public Task PublishDelayAsync<T>(TimeSpan delay, string name, T? content, string? group, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布延迟事件(带组和取消令牌): {EventName}, 延迟: {Delay}", name, delay);
            return _capPublisher.PublishDelayAsync(delay, name, content, group, cancellationToken);
        }
        
        public Task PublishDelayAsync<T>(TimeSpan delay, string name, T content, string callbackName, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(callbackName);
            
            _logger.LogInformation("发布延迟事件(带回调): {EventName}, 延迟: {Delay}, 回调: {CallbackName}", name, delay, callbackName);
            return _capPublisher.PublishDelayAsync(delay, name, content, callbackName, cancellationToken);
        }
        


        public Task PublishDelayAsync<T>(TimeSpan delay, string name, T? content, IDictionary<string, string> headers, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布延迟事件(带头部和取消令牌): {EventName}, 延迟: {Delay}", name, delay);
            return _capPublisher.PublishDelayAsync(delay, name, content, headers, cancellationToken);
        }

        public Task PublishDelayAsync<T>(TimeSpan delay, string name, T? content, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布延迟事件(带事务和取消令牌): {EventName}, 延迟: {Delay}", name, delay);
            return _capPublisher.PublishDelayAsync(delay, name, content, transaction, cancellationToken);
        }

        public Task PublishDelayAsync<T>(TimeSpan delay, string name, T? content, IDictionary<string, string> headers, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布延迟事件(带事务、头部和取消令牌): {EventName}, 延迟: {Delay}", name, delay);
            return _capPublisher.PublishDelayAsync(delay, name, content, headers, transaction, cancellationToken);
        }

        // 字符串版本的延迟发布方法
        public Task PublishDelayAsync(TimeSpan delay, string name, string? content)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布字符串延迟事件: {EventName}, 延迟: {Delay}", name, delay);
            return _capPublisher.PublishDelayAsync(delay, name, content);
        }

        public Task PublishDelayAsync(TimeSpan delay, string name, string? content, IDbTransaction? transaction)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布字符串延迟事件(带事务): {EventName}, 延迟: {Delay}", name, delay);
            return _capPublisher.PublishDelayAsync(delay, name, content, transaction);
        }

        public Task PublishDelayAsync(TimeSpan delay, string name, string? content, IDictionary<string, string> headers)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布字符串延迟事件(带头部): {EventName}, 延迟: {Delay}", name, delay);
            return _capPublisher.PublishDelayAsync(delay, name, content, headers);
        }

        public Task PublishDelayAsync(TimeSpan delay, string name, string? content, IDictionary<string, string> headers, IDbTransaction? transaction)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布字符串延迟事件(带事务和头部): {EventName}, 延迟: {Delay}", name, delay);
            return _capPublisher.PublishDelayAsync(delay, name, content, headers, transaction);
        }

        public Task PublishDelayAsync(TimeSpan delay, string name, string? content, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布字符串延迟事件(带取消令牌): {EventName}, 延迟: {Delay}", name, delay);
            return _capPublisher.PublishDelayAsync(delay, name, content, cancellationToken);
        }

        public Task PublishDelayAsync(TimeSpan delay, string name, string? content, IDictionary<string, string> headers, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布字符串延迟事件(带头部和取消令牌): {EventName}, 延迟: {Delay}", name, delay);
            return _capPublisher.PublishDelayAsync(delay, name, content, headers, cancellationToken);
        }

        public Task PublishDelayAsync(TimeSpan delay, string name, string? content, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布字符串延迟事件(带事务和取消令牌): {EventName}, 延迟: {Delay}", name, delay);
            return _capPublisher.PublishDelayAsync(delay, name, content, transaction, cancellationToken);
        }

        public Task PublishDelayAsync(TimeSpan delay, string name, string? content, IDictionary<string, string> headers, IDbTransaction? transaction, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("发布字符串延迟事件(带事务、头部和取消令牌): {EventName}, 延迟: {Delay}", name, delay);
            return _capPublisher.PublishDelayAsync(delay, name, content, headers, transaction, cancellationToken);
        }

        // 同步延迟发布方法
        public void PublishDelay<T>(TimeSpan delay, string name, T? content)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("同步发布延迟事件: {EventName}, 延迟: {Delay}", name, delay);
            _capPublisher.PublishDelay(delay, name, content);
        }

        public void PublishDelay<T>(TimeSpan delay, string name, T? content, IDbTransaction? transaction)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("同步发布延迟事件(带事务): {EventName}, 延迟: {Delay}", name, delay);
            _capPublisher.PublishDelay(delay, name, content, transaction);
        }

        public void PublishDelay<T>(TimeSpan delay, string name, T? content, IDictionary<string, string> headers)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("同步发布延迟事件(带头部): {EventName}, 延迟: {Delay}", name, delay);
            _capPublisher.PublishDelay(delay, name, content, headers);
        }

        public void PublishDelay<T>(TimeSpan delay, string name, T? content, IDictionary<string, string> headers, IDbTransaction? transaction)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("同步发布延迟事件(带事务和头部): {EventName}, 延迟: {Delay}", name, delay);
            _capPublisher.PublishDelay(delay, name, content, headers, transaction);
        }

        public void PublishDelay<T>(TimeSpan delay, string name, T? content, string? group)
        {
            ArgumentNullException.ThrowIfNull(name);
            _logger.LogInformation("同步发布延迟事件(带组): {EventName}, 延迟: {Delay}", name, delay);
            _capPublisher.PublishDelay(delay, name, content, group);
        }
        
        public Task PublishDelay<T>(TimeSpan delay, string name, T content, string callbackName)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(callbackName);
            
            _logger.LogInformation("发布延迟事件(带回调): {EventName}, 延迟: {Delay}, 回调: {CallbackName}", name, delay, callbackName);
            return _capPublisher.PublishDelay(delay, name, content, callbackName);
        }
        


        // 字符串版本的同步延迟发布方法
        public void PublishDelay(TimeSpan delay, string name, string? content)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("同步发布字符串延迟事件: {EventName}, 延迟: {Delay}", name, delay);
            _capPublisher.PublishDelay(delay, name, content);
        }

        public void PublishDelay(TimeSpan delay, string name, string? content, IDbTransaction? transaction)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("同步发布字符串延迟事件(带事务): {EventName}, 延迟: {Delay}", name, delay);
            _capPublisher.PublishDelay(delay, name, content, transaction);
        }

        public void PublishDelay(TimeSpan delay, string name, string? content, IDictionary<string, string> headers)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("同步发布字符串延迟事件(带头部): {EventName}, 延迟: {Delay}", name, delay);
            _capPublisher.PublishDelay(delay, name, content, headers);
        }

        public void PublishDelay(TimeSpan delay, string name, string? content, IDictionary<string, string> headers, IDbTransaction? transaction)
        {
            ArgumentNullException.ThrowIfNull(name);
            
            _logger.LogInformation("同步发布字符串延迟事件(带事务和头部): {EventName}, 延迟: {Delay}", name, delay);
            _capPublisher.PublishDelay(delay, name, content, headers, transaction);
        }

        // ICapPublisher接口方法实现
        public object GetPublishProvider()
        {
            return _capPublisher.GetPublishProvider();
        }

        public object GetSubscribeProvider()
        {
            return _capPublisher.GetSubscribeProvider();
        }

        // IExtendedCapPublisher接口实现
        public Task PublishWithTagsAsync<T>(string name, T? content, params string[] tags) {
            ArgumentNullException.ThrowIfNull(name);
            
            // 创建头部信息
            var headers = new Dictionary<string, string>();
            if (tags != null && tags.Length > 0)
            {
                // 添加标签信息
                headers["Event-Tags"] = string.Join(",", tags);
            }
            
            // 添加事件追踪信息
            headers["Event-Timestamp"] = DateTime.UtcNow.ToString("o");
            
            _logger.LogInformation("发布带标签事件: {EventName}, 标签: {Tags}", name, string.Join(",", tags));
            return _capPublisher.PublishAsync(name, content, headers);
        }

        public Task PublishWithTagsAsync<T>(string name, T? content, IDictionary<string, string> headers, params string[] tags) {
            ArgumentNullException.ThrowIfNull(name);
            
            // 如果没有传入头部信息，则创建新的
            var finalHeaders = headers != null 
                ? new Dictionary<string, string>(headers) 
                : new Dictionary<string, string>();
            
            // 添加标签信息
            if (tags != null && tags.Length > 0)
            {
                finalHeaders["Event-Tags"] = string.Join(",", tags);
            }
            
            // 添加事件追踪信息
            finalHeaders["Event-Timestamp"] = DateTime.UtcNow.ToString("o");
            
            _logger.LogInformation("发布带标签和自定义头部事件: {EventName}, 标签: {Tags}", name, string.Join(",", tags));
            return _capPublisher.PublishAsync(name, content, finalHeaders);
        }

        public Task PublishWithTagsAsync<T>(string name, T? content, IDbTransaction? transaction, params string[] tags) {
            ArgumentNullException.ThrowIfNull(name);
            
            // 创建头部信息
            var headers = new Dictionary<string, string>();
            if (tags != null && tags.Length > 0)
            {
                // 添加标签信息
                headers["Event-Tags"] = string.Join(",", tags);
            }
            
            // 添加事件追踪信息
            headers["Event-Timestamp"] = DateTime.UtcNow.ToString("o");
            
            _logger.LogInformation("发布带标签和事务事件: {EventName}, 标签: {Tags}", name, string.Join(",", tags));
            return _capPublisher.PublishAsync(name, content, headers, transaction);
        }

        public Task PublishWithTagsAsync<T>(string name, T? content, IDictionary<string, string> headers, ITransaction? transaction, params string[] tags) {
            ArgumentNullException.ThrowIfNull(name);
            
            // 如果没有传入头部信息，则创建新的
            var finalHeaders = headers != null 
                ? new Dictionary<string, string>(headers.Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value!)) 
                : new Dictionary<string, string>();
            
            // 添加标签信息
            if (tags != null && tags.Length > 0)
            {
                finalHeaders["Event-Tags"] = string.Join(",", tags);
            }
            
            // 添加事件追踪信息
            finalHeaders["Event-Timestamp"] = DateTime.UtcNow.ToString("o");
            
            _logger.LogInformation("发布带标签、自定义头部和事务事件: {EventName}, 标签: {Tags}", name, string.Join(",", tags));
            return _capPublisher.PublishAsync(name, content, finalHeaders, transaction);
        }

        // 注册外部订阅者
        public async Task RegisterExternalSubscriberAsync(string eventName, string subscriberUrl, string subscriberId, Dictionary<string, string>? headers = null)
        {
            ArgumentNullException.ThrowIfNull(eventName);
            ArgumentNullException.ThrowIfNull(subscriberUrl);
            ArgumentNullException.ThrowIfNull(subscriberId);
            
            // 验证URL是否有效
            if (!Uri.IsWellFormedUriString(subscriberUrl, UriKind.Absolute))
            {
                throw new ArgumentException("订阅者URL格式无效", nameof(subscriberUrl));
            }
            
            // 获取或创建该事件的订阅者集合
            var subscribers = _externalSubscribers.GetOrAdd(eventName, _ => new ConcurrentBag<ExternalSubscriber>());
            
            // 创建订阅者实例
            var subscriber = new ExternalSubscriber
            {
                SubscriberId = subscriberId,
                Url = subscriberUrl,
                Headers = headers ?? new Dictionary<string, string>(),
                RegisteredAt = DateTime.UtcNow
            };
            
            // 添加到集合
            subscribers.Add(subscriber);
            
            _logger.LogInformation("成功注册外部订阅者: {SubscriberId} 到事件 {EventName}", subscriberId, eventName);
            
            // 可选：发送注册确认通知
            try
            {
                var confirmationMessage = new { eventName = eventName, registeredAt = subscriber.RegisteredAt };
                var content = new StringContent(JsonSerializer.Serialize(confirmationMessage), System.Text.Encoding.UTF8, "application/json");
                
                // 添加自定义头部
                foreach (var header in subscriber.Headers)
                {
                    content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                
                var response = await _httpClient.PostAsync($"{subscriberUrl}/registration-confirm", content);
                response.EnsureSuccessStatusCode();
                
                _logger.LogInformation("成功发送注册确认到订阅者: {SubscriberId}", subscriberId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "发送注册确认到订阅者失败: {SubscriberId}", subscriberId);
                // 不抛出异常，因为注册本身已经成功
            }
        }

        // 移除外部订阅者
        public Task RemoveExternalSubscriberAsync(string eventName, string subscriberId)
        {
            ArgumentNullException.ThrowIfNull(eventName);
            ArgumentNullException.ThrowIfNull(subscriberId);
            
            if (_externalSubscribers.TryGetValue(eventName, out var subscribers))
            {
                // 创建新的集合来存储需要保留的订阅者
                var updatedSubscribers = new ConcurrentBag<ExternalSubscriber>();
                bool found = false;
                
                // 过滤掉要移除的订阅者
                foreach (var subscriber in subscribers)
                {
                    if (subscriber.SubscriberId != subscriberId)
                    {
                        updatedSubscribers.Add(subscriber);
                    }
                    else
                    {
                        found = true;
                    }
                }
                
                if (found)
                {
                    // 如果集合为空，则移除该事件的条目
                    if (updatedSubscribers.IsEmpty)
                    {
                        _externalSubscribers.TryRemove(eventName, out _);
                    }
                    else
                    {
                        // 否则更新集合
                        _externalSubscribers[eventName] = updatedSubscribers;
                    }
                    
                    _logger.LogInformation("成功移除外部订阅者: {SubscriberId} 从事件 {EventName}", subscriberId, eventName);
                }
                else
                {
                    _logger.LogWarning("未找到要移除的外部订阅者: {SubscriberId} 从事件 {EventName}", subscriberId, eventName);
                }
            }
            else
            {
                _logger.LogWarning("未找到事件: {EventName}", eventName);
            }
            
            return Task.CompletedTask;
        }

        // 通知所有外部订阅者
        public async Task NotifyExternalSubscribersAsync(string eventName, object eventData)
        {
            ArgumentNullException.ThrowIfNull(eventName);
            ArgumentNullException.ThrowIfNull(eventData);
            
            if (!_externalSubscribers.TryGetValue(eventName, out var subscribers))
            {
                // 没有外部订阅者，直接返回
                _logger.LogDebug("事件 {EventName} 没有外部订阅者", eventName);
                return;
            }
            
            // 序列化事件数据
            var eventJson = JsonSerializer.Serialize(eventData);
            
            _logger.LogInformation("开始通知事件 {EventName} 的外部订阅者，共 {Count} 个", eventName, subscribers.Count);
            
            // 创建通知任务列表
            var notificationTasks = subscribers.Select(subscriber => 
                NotifySingleSubscriberAsync(subscriber, eventName, eventJson))
                .ToList();
            
            // 并行执行所有通知
            await Task.WhenAll(notificationTasks);
            
            _logger.LogInformation("完成通知事件 {EventName} 的所有外部订阅者", eventName);
        }

        // 通知单个外部订阅者
        private async Task NotifySingleSubscriberAsync(ExternalSubscriber subscriber, string eventName, string eventJson)
        {
            try
            {
                // 创建请求内容
                var content = new StringContent(eventJson, System.Text.Encoding.UTF8, "application/json");
                
                // 设置自定义头部
                foreach (var header in subscriber.Headers)
                {
                    content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                
                // 添加事件相关的头部
                content.Headers.TryAddWithoutValidation("X-CAP-Event-Name", eventName);
                content.Headers.TryAddWithoutValidation("X-CAP-Subscriber-Id", subscriber.SubscriberId);
                content.Headers.TryAddWithoutValidation("X-CAP-Timestamp", DateTime.UtcNow.ToString("o"));
                
                _logger.LogDebug("通知订阅者 {SubscriberId} 关于事件 {EventName}", subscriber.SubscriberId, eventName);
                
                // 发送HTTP请求
                var response = await _httpClient.PostAsync(subscriber.Url, content);
                
                // 检查响应状态
                response.EnsureSuccessStatusCode();
                
                _logger.LogInformation("成功通知订阅者 {SubscriberId} 关于事件 {EventName}", subscriber.SubscriberId, eventName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "通知订阅者 {SubscriberId} 关于事件 {EventName} 失败", subscriber.SubscriberId, eventName);
                // 不重新抛出异常，避免影响其他订阅者的通知
            }
        }

        // 内部订阅者模型类
        private class ExternalSubscriber
        {
            /// <summary>
            /// 订阅者唯一标识
            /// </summary>
            public string SubscriberId { get; set; }
            
            /// <summary>
            /// 订阅者回调URL
            /// </summary>
            public string Url { get; set; }
            
            /// <summary>
            /// 自定义头部信息
            /// </summary>
            public Dictionary<string, string> Headers { get; set; }
            
            /// <summary>
            /// 注册时间
            /// </summary>
            public DateTime RegisteredAt { get; set; }
        }








        



        

    }
}