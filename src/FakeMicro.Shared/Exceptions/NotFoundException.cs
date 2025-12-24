using System;
using System.Runtime.Serialization;

namespace FakeMicro.Shared.Exceptions
{
    /// <summary>
    /// 未找到异常类，用于表示资源不存在
    /// </summary>
    [Serializable]
    public class NotFoundException : CustomException
    {
        /// <summary>
        /// 获取或设置资源名称
        /// </summary>
        public string ResourceName { get; set; }

        /// <summary>
        /// 获取或设置资源ID
        /// </summary>
        public string ResourceId { get; set; }

        /// <summary>
        /// 初始化NotFoundException类的新实例
        /// </summary>
        public NotFoundException()
            : base("资源不存在", "not_found", 404)
        {
        }

        /// <summary>
        /// 使用指定的错误消息初始化NotFoundException类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        public NotFoundException(string message)
            : base(message, "not_found", 404)
        {
        }

        /// <summary>
        /// 使用指定的资源名称和ID初始化NotFoundException类的新实例
        /// </summary>
        /// <param name="resourceName">资源名称</param>
        /// <param name="resourceId">资源ID</param>
        public NotFoundException(string resourceName, string resourceId)
            : base($"资源 '{resourceName}' (ID: {resourceId}) 不存在", "not_found", 404)
        {
            ResourceName = resourceName;
            ResourceId = resourceId;
        }

        /// <summary>
        /// 使用指定的错误消息和内部异常初始化NotFoundException类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public NotFoundException(string message, Exception innerException)
            : base(message, "not_found", innerException)
        {
            HttpStatusCode = 404;
        }

        /// <summary>
        /// 使用指定的错误消息、错误代码和HTTP状态码初始化NotFoundException类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="errorCode">错误代码</param>
        /// <param name="httpStatusCode">HTTP状态码</param>
        public NotFoundException(string message, string errorCode, int httpStatusCode)
            : base(message, errorCode, httpStatusCode)
        {
        }

        /// <summary>
        /// 使用指定的错误消息、错误代码、HTTP状态码和内部异常初始化NotFoundException类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="errorCode">错误代码</param>
        /// <param name="httpStatusCode">HTTP状态码</param>
        /// <param name="innerException">内部异常</param>
        public NotFoundException(string message, string errorCode, int httpStatusCode, Exception innerException)
            : base(message, errorCode, httpStatusCode, innerException)
        {
        }

        /// <summary>
        /// 使用序列化数据初始化NotFoundException类的新实例
        /// </summary>
        /// <param name="info">序列化信息</param>
        /// <param name="context">流上下文</param>
        protected NotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ResourceName = info.GetString(nameof(ResourceName))!;
            ResourceId = info.GetString(nameof(ResourceId))!;
        }

        /// <summary>
        /// 设置序列化所需的数据
        /// </summary>
        /// <param name="info">序列化信息</param>
        /// <param name="context">流上下文</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(ResourceName), ResourceName);
            info.AddValue(nameof(ResourceId), ResourceId);
            base.GetObjectData(info, context);
        }
    }
}