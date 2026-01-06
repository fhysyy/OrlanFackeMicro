using FakeMicro.Interfaces.Services;
using Orleans;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace FakeMicro.Grains.Eventing
{
    /// <summary>
    /// 简化版事件流提供程序（基于内存实现）
    /// </summary>
    public class SimpleEventStreamProvider : IGrainObserver, ISimpleEventPublisher, ISimpleEventSubscriber
    {
        private readonly ILoggerService _logger;
        private readonly ConcurrentDictionary<string, ConcurrentBag<ISimpleEventObserver>> _subscribers;

        public SimpleEventStreamProvider(ILoggerService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscribers = new ConcurrentDictionary<string, ConcurrentBag<ISimpleEventObserver>>();
        }

        /// <summary>
        /// 发布事件
        /// </summary>
        public async Task PublishAsync<TEvent>(TEvent @event, string streamNamespace) where TEvent : class
        {
            try
            {
                if (_subscribers.TryGetValue(streamNamespace, out var observers))
                {
                    var tasks = observers.Select(observer => observer.OnEventAsync(@event));
                    await Task.WhenAll(tasks);
                }
                
                _logger.LogInformation("事件发布成功: {EventType}, Namespace: {Namespace}", 
                    typeof(TEvent).Name, streamNamespace);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "事件发布失败: {EventType}, Namespace: {Namespace}", 
                    typeof(TEvent).Name, streamNamespace);
                throw;
            }
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        public Task SubscribeAsync<TEvent>(string streamNamespace, ISimpleEventObserver observer) where TEvent : class
        {
            try
            {
                var observers = _subscribers.GetOrAdd(streamNamespace, _ => new ConcurrentBag<ISimpleEventObserver>());
                observers.Add(observer);
                
                _logger.LogInformation("事件订阅成功: {Namespace}", streamNamespace);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "事件订阅失败: {Namespace}", streamNamespace);
                throw;
            }
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        public Task UnsubscribeAsync(string streamNamespace, ISimpleEventObserver observer)
        {
            try
            {
                if (_subscribers.TryGetValue(streamNamespace, out var observers))
                {
                    // 由于ConcurrentBag不支持直接删除，我们重新创建列表
                    var newObservers = new ConcurrentBag<ISimpleEventObserver>(observers.Where(o => o != observer));
                    _subscribers[streamNamespace] = newObservers;
                }
                
                _logger.LogInformation("事件取消订阅成功: {Namespace}", streamNamespace);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "事件取消订阅失败: {Namespace}", streamNamespace);
                throw;
            }
        }
    }

    /// <summary>
    /// 简化版事件观察者接口
    /// </summary>
    public interface ISimpleEventObserver : IGrainObserver
    {
        Task OnEventAsync<TEvent>(TEvent @event) where TEvent : class;
    }

    /// <summary>
    /// 事件发布接口（简化版）
    /// </summary>
    public interface ISimpleEventPublisher
    {
        Task PublishAsync<TEvent>(TEvent @event, string streamNamespace) where TEvent : class;
    }

    /// <summary>
    /// 事件订阅接口（简化版）
    /// </summary>
    public interface ISimpleEventSubscriber
    {
        Task SubscribeAsync<TEvent>(string streamNamespace, ISimpleEventObserver observer) where TEvent : class;
        Task UnsubscribeAsync(string streamNamespace, ISimpleEventObserver observer);
    }
}