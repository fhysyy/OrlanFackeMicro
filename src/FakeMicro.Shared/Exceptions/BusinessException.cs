using System;
using System.Runtime.Serialization;

namespace FakeMicro.Shared.Exceptions
{
    /// <summary>
    /// 业务异常类，用于表示业务规则违反
    /// </summary>
    [Serializable]
    public class BusinessException : CustomException
    {
        /// <summary>
        /// 初始化BusinessException类的新实例
        /// </summary>
        public BusinessException()
            : base("业务规则违反", "business_error", 400)
        {
        }

        /// <summary>
        /// 使用指定的错误消息初始化BusinessException类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        public BusinessException(string message)
            : base(message, "business_error", 400)
        {
        }

        /// <summary>
        /// 使用指定的错误消息和错误代码初始化BusinessException类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="errorCode">错误代码</param>
        public BusinessException(string message, string errorCode)
            : base(message, errorCode, 400)
        {
        }

        /// <summary>
        /// 使用指定的错误消息和内部异常初始化BusinessException类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public BusinessException(string message, Exception innerException)
            : base(message, "business_error", innerException)
        {
            HttpStatusCode = 400;
        }

        /// <summary>
        /// 使用指定的错误消息、错误代码和内部异常初始化BusinessException类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="errorCode">错误代码</param>
        /// <param name="innerException">内部异常</param>
        public BusinessException(string message, string errorCode, Exception innerException)
            : base(message, errorCode, innerException)
        {
            HttpStatusCode = 400;
        }

        /// <summary>
        /// 使用指定的错误消息、错误代码和HTTP状态码初始化BusinessException类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="errorCode">错误代码</param>
        /// <param name="httpStatusCode">HTTP状态码</param>
        public BusinessException(string message, string errorCode, int httpStatusCode)
            : base(message, errorCode, httpStatusCode)
        {
        }

        /// <summary>
        /// 使用指定的错误消息、错误代码、HTTP状态码和内部异常初始化BusinessException类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="errorCode">错误代码</param>
        /// <param name="httpStatusCode">HTTP状态码</param>
        /// <param name="innerException">内部异常</param>
        public BusinessException(string message, string errorCode, int httpStatusCode, Exception innerException)
            : base(message, errorCode, httpStatusCode, innerException)
        {
        }

        /// <summary>
        /// 使用序列化数据初始化BusinessException类的新实例
        /// </summary>
        /// <param name="info">序列化信息</param>
        /// <param name="context">流上下文</param>
        protected BusinessException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}