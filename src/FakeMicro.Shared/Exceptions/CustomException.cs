using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace FakeMicro.Shared.Exceptions
{
    /// <summary>
    /// 自定义异常基类，用于实现更精确的错误类型处理
    /// </summary>
    [Serializable]
    public class CustomException : Exception
    {
        /// <summary>
        /// 获取或设置错误代码
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        /// 获取或设置HTTP状态码
        /// </summary>
        public int HttpStatusCode { get; set; }

        /// <summary>
        /// 获取或设置错误详情
        /// </summary>
        public IDictionary<string, object> Details { get; set; }

        /// <summary>
        /// 初始化CustomException类的新实例
        /// </summary>
        public CustomException()
            : base("发生了错误")
        {
            ErrorCode = "error";
            HttpStatusCode = 500;
            Details = new Dictionary<string, object>();
        }

        /// <summary>
        /// 使用指定的错误消息初始化CustomException类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        public CustomException(string message)
            : base(message)
        {
            ErrorCode = "error";
            HttpStatusCode = 500;
            Details = new Dictionary<string, object>();
        }

        /// <summary>
        /// 使用指定的错误消息和错误代码初始化CustomException类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="errorCode">错误代码</param>
        public CustomException(string message, string errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
            HttpStatusCode = 500;
            Details = new Dictionary<string, object>();
        }

        /// <summary>
        /// 使用指定的错误消息、错误代码和HTTP状态码初始化CustomException类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="errorCode">错误代码</param>
        /// <param name="httpStatusCode">HTTP状态码</param>
        public CustomException(string message, string errorCode, int httpStatusCode)
            : base(message)
        {
            ErrorCode = errorCode;
            HttpStatusCode = httpStatusCode;
            Details = new Dictionary<string, object>();
        }

        /// <summary>
        /// 使用指定的错误消息和内部异常初始化CustomException类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public CustomException(string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = "error";
            HttpStatusCode = 500;
            Details = new Dictionary<string, object>();
        }

        /// <summary>
        /// 使用指定的错误消息、错误代码和内部异常初始化CustomException类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="errorCode">错误代码</param>
        /// <param name="innerException">内部异常</param>
        public CustomException(string message, string errorCode, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            HttpStatusCode = 500;
            Details = new Dictionary<string, object>();
        }

        /// <summary>
        /// 使用指定的错误消息、错误代码、HTTP状态码和内部异常初始化CustomException类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="errorCode">错误代码</param>
        /// <param name="httpStatusCode">HTTP状态码</param>
        /// <param name="innerException">内部异常</param>
        public CustomException(string message, string errorCode, int httpStatusCode, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            HttpStatusCode = httpStatusCode;
            Details = new Dictionary<string, object>();
        }

        /// <summary>
        /// 使用序列化数据初始化CustomException类的新实例
        /// </summary>
        /// <param name="info">序列化信息</param>
        /// <param name="context">流上下文</param>
        protected CustomException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ErrorCode = info.GetString(nameof(ErrorCode))!;
            HttpStatusCode = info.GetInt32(nameof(HttpStatusCode));
            Details = (IDictionary<string, object>)info.GetValue(nameof(Details), typeof(IDictionary<string, object>))!;
        }

        /// <summary>
        /// 设置序列化所需的数据
        /// </summary>
        /// <param name="info">序列化信息</param>
        /// <param name="context">流上下文</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(ErrorCode), ErrorCode);
            info.AddValue(nameof(HttpStatusCode), HttpStatusCode);
            info.AddValue(nameof(Details), Details);
            base.GetObjectData(info, context);
        }
    }
}