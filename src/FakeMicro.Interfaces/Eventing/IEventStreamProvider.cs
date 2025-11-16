using Orleans;

namespace FakeMicro.Interfaces.Eventing
{
    /// <summary>
    /// 事件流提供程序接口
    /// </summary>
    public interface IEventStreamProvider
    {
        /// <summary>
        /// 注册事件观察者
        /// </summary>
        Task RegisterObserverAsync<TEvent>(Guid streamId, string streamNamespace, IEventObserver observer) where TEvent : class;
        
        /// <summary>
        /// 取消注册事件观察者
        /// </summary>
        Task UnregisterObserverAsync<TEvent>(Guid streamId, string streamNamespace, IEventObserver observer) where TEvent : class;
    }

    /// <summary>
    /// 事件观察者接口
    /// </summary>
    public interface IEventObserver : IGrainObserver
    {
        /// <summary>
        /// 处理事件
        /// </summary>
        Task OnEventAsync<TEvent>(TEvent @event) where TEvent : class;
    }

    /// <summary>
    /// 事件发布者接口
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        /// 发布事件
        /// </summary>
        Task PublishAsync<TEvent>(TEvent @event, Guid streamId, string streamNamespace) where TEvent : class;
    }

    /// <summary>
    /// 事件订阅者接口
    /// </summary>
    public interface IEventSubscriber
    {
        /// <summary>
        /// 订阅事件流
        /// </summary>
        Task<string> SubscribeAsync<TEvent>(Guid streamId, string streamNamespace, IEventObserver observer) where TEvent : class;

        /// <summary>
        /// 取消订阅
        /// </summary>
        Task UnsubscribeAsync<TEvent>(string subscriptionId) where TEvent : class;
    }
}