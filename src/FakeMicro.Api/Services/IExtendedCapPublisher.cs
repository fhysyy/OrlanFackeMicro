using DotNetCore.CAP;
using System.Threading.Tasks;

namespace FakeMicro.Api.Services
{
    /// <summary>
    /// 扩展的CAP发布器接口
    /// 提供外部订阅管理功能
    /// </summary>
    public interface IExtendedCapPublisher : ICapPublisher
    {
        /// <summary>
        /// 发布带标签的事件，便于外部系统按标签订阅
        /// </summary>
        /// <typeparam name="T">事件数据类型</typeparam>
        /// <param name="eventName">事件名称</param>
        /// <param name="eventData">事件数据</param>
        /// <param name="tags">标签列表</param>
        Task PublishWithTagsAsync<T>(string eventName, T eventData, params string[] tags) where T : class;
        
        /// <summary>
        /// 注册外部订阅者
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="callbackUrl">回调URL</param>
        /// <param name="description">订阅者描述</param>
        Task RegisterExternalSubscriberAsync(string eventName, string callbackUrl, string description = null);
        
        /// <summary>
        /// 移除外部订阅者
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="callbackUrl">回调URL</param>
        Task RemoveExternalSubscriberAsync(string eventName, string callbackUrl);
    }
}