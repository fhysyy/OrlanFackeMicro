using FakeMicro.Interfaces.Eventing;
using FakeMicro.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace FakeMicro.Grains.Eventing
{
    /// <summary>
    /// Orleans流事件发布者
    /// </summary>
    public class OrleansEventPublisher : IEventPublisher
    {
        private readonly IStreamProviderFactory _streamProviderFactory;
        private readonly ILoggerService _logger;

        public OrleansEventPublisher(
            IStreamProviderFactory streamProviderFactory,
            ILoggerService logger)
        {
            _streamProviderFactory = streamProviderFactory ?? throw new ArgumentNullException(nameof(streamProviderFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task PublishAsync<TEvent>(TEvent @event, Guid streamId, string streamNamespace) where TEvent : class
        {
            try
            {
                var streamProvider = await _streamProviderFactory.GetStreamProviderAsync("DefaultStream");
                var stream = streamProvider.GetStream<TEvent>(StreamId.Create(streamNamespace, streamId));
                await stream.OnNextAsync(@event);
                
                _logger.LogInformation($"事件发布成功: {typeof(TEvent).Name}, StreamId: {streamId}, Namespace: {streamNamespace}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"事件发布失败: {typeof(TEvent).Name}, StreamId: {streamId}, Namespace: {streamNamespace}");
                throw;
            }
        }
    }

    /// <summary>
    /// Orleans流事件订阅者
    /// </summary>
    public class OrleansEventSubscriber : IEventSubscriber
    {
        private readonly IStreamProviderFactory _streamProviderFactory;
        private readonly ILoggerService _logger;
        private readonly ConcurrentDictionary<string, object> _subscriptions;

        public OrleansEventSubscriber(
            IStreamProviderFactory streamProviderFactory,
            ILoggerService logger)
        {
            _streamProviderFactory = streamProviderFactory ?? throw new ArgumentNullException(nameof(streamProviderFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriptions = new ConcurrentDictionary<string, object>();
        }

        public async Task<string> SubscribeAsync<TEvent>(Guid streamId, string streamNamespace, IEventObserver observer) where TEvent : class
        {
            try
            {
                var streamProvider = await _streamProviderFactory.GetStreamProviderAsync("DefaultStream");
                var stream = streamProvider.GetStream<TEvent>(StreamId.Create(streamNamespace, streamId));
                var subscriptionId = $"{streamId}_{streamNamespace}_{Guid.NewGuid()}";
                
                var handle = await stream.SubscribeAsync(
                    async (data, token) =>
                    {
                        try
                        {
                            await observer.OnEventAsync(data);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"事件处理失败: {typeof(TEvent).Name}");
                        }
                    });

                _subscriptions.TryAdd(subscriptionId, handle);
                
                _logger.LogInformation($"事件订阅成功: {typeof(TEvent).Name}, StreamId: {streamId}, Namespace: {streamNamespace}, SubscriptionId: {subscriptionId}");
                
                return subscriptionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"事件订阅失败: {typeof(TEvent).Name}, StreamId: {streamId}, Namespace: {streamNamespace}");
                throw;
            }
        }

        public async Task UnsubscribeAsync<TEvent>(string subscriptionId) where TEvent : class
        {
            try
            {
                if (_subscriptions.TryRemove(subscriptionId, out var handle))
                {
                    if (handle is StreamSubscriptionHandle<TEvent> typedHandle)
                    {
                        await typedHandle.UnsubscribeAsync();
                        _logger.LogInformation($"事件取消订阅成功: SubscriptionId: {subscriptionId}");
                    }
                }
                else
                {
                    _logger.LogWarning($"订阅不存在: SubscriptionId: {subscriptionId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"事件取消订阅失败: SubscriptionId: {subscriptionId}");
                throw;
            }
        }
    }

    /// <summary>
    /// 事件流提供程序
    /// </summary>
    public class OrleansEventStreamProvider : IEventStreamProvider
    {
        private readonly IStreamProviderFactory _streamProviderFactory;
        private readonly ILoggerService _logger;
        private readonly IEventPublisher _publisher;
        private readonly IEventSubscriber _subscriber;

        public OrleansEventStreamProvider(
            IStreamProviderFactory streamProviderFactory,
            ILoggerService logger,
            IEventPublisher publisher,
            IEventSubscriber subscriber)
        {
            _streamProviderFactory = streamProviderFactory ?? throw new ArgumentNullException(nameof(streamProviderFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        public Task RegisterObserverAsync<TEvent>(Guid streamId, string streamNamespace, IEventObserver observer) where TEvent : class
        {
            return _subscriber.SubscribeAsync<TEvent>(streamId, streamNamespace, observer);
        }

        public Task UnregisterObserverAsync<TEvent>(Guid streamId, string streamNamespace, IEventObserver observer) where TEvent : class
        {
            return _subscriber.UnsubscribeAsync<TEvent>($"{streamId}_{streamNamespace}");
        }

        public IEventPublisher Publisher => _publisher;
        public IEventSubscriber Subscriber => _subscriber;
    }

    /// <summary>
    /// 通用事件观察者基类
    /// </summary>
    public abstract class EventObserverBase : IEventObserver
    {
        private readonly ILoggerService _logger;

        protected EventObserverBase(ILoggerService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task OnEventAsync<TEvent>(TEvent @event) where TEvent : class
        {
            try
            {
                await HandleEventAsync(@event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"事件处理失败: {typeof(TEvent).Name}");
                throw;
            }
        }

        protected abstract Task HandleEventAsync<TEvent>(TEvent @event) where TEvent : class;
    }

    /// <summary>
    /// 事件处理程序委托
    /// </summary>
    public delegate Task EventHandlerDelegate<TEvent>(TEvent @event) where TEvent : class;

    /// <summary>
    /// 基于委托的事件观察者
    /// </summary>
    public class DelegateEventObserver : EventObserverBase
    {
        private readonly ConcurrentDictionary<Type, Delegate> _handlers;

        public DelegateEventObserver(ILoggerService logger) : base(logger)
        {
            _handlers = new ConcurrentDictionary<Type, Delegate>();
        }

        public void RegisterHandler<TEvent>(EventHandlerDelegate<TEvent> handler) where TEvent : class
        {
            _handlers.TryAdd(typeof(TEvent), handler);
        }

        protected override Task HandleEventAsync<TEvent>(TEvent @event)
        {
            if (_handlers.TryGetValue(typeof(TEvent), out var handler))
            {
                var typedHandler = (EventHandlerDelegate<TEvent>)handler;
                return typedHandler(@event);
            }

            return Task.CompletedTask;
        }
    }
}
