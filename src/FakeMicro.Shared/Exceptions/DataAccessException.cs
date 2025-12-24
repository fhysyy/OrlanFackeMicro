using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace FakeMicro.Shared.Exceptions
{
    /// <summary>
    /// 数据访问异常，用于封装数据库操作相关的错误
    /// </summary>
    [Serializable]
    public class DataAccessException : CustomException
    {
        /// <summary>
        /// 初始化DataAccessException类的新实例
        /// </summary>
        public DataAccessException() : base("数据访问操作失败")
        {
            HttpStatusCode = 500;
        }

        /// <summary>
        /// 使用指定的错误消息初始化DataAccessException类的新实例
        /// </summary>
        /// <param name="message">描述错误的消息</param>
        public DataAccessException(string message) : base(message)
        {
            HttpStatusCode = 500;
        }

        /// <summary>
        /// 使用指定的错误消息和对作为此异常原因的内部异常的引用来初始化DataAccessException类的新实例
        /// </summary>
        /// <param name="message">解释异常原因的错误消息</param>
        /// <param name="innerException">导致当前异常的异常</param>
        public DataAccessException(string message, Exception innerException) : base(message, innerException)
        {
            HttpStatusCode = 500;
        }

        /// <summary>
        /// 使用指定的错误消息、状态码和错误详细信息初始化DataAccessException类的新实例
        /// </summary>
        /// <param name="message">解释异常原因的错误消息</param>
        /// <param name="httpStatusCode">HTTP状态码</param>
        /// <param name="errors">验证错误列表</param>
        public DataAccessException(string message, int httpStatusCode, IDictionary<string, object> errors = null)
            : base(message)
        {
            HttpStatusCode = httpStatusCode;
            Details = errors;
        }

        /// <summary>
        /// 用序列化数据初始化DataAccessException类的新实例
        /// </summary>
        /// <param name="info">包含有关序列化异常的信息</param>
        /// <param name="context">包含有关源或目标的上下文信息</param>
        protected DataAccessException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
}