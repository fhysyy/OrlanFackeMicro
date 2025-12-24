using System;
using System.Runtime.Serialization;

namespace FakeMicro.Shared.Exceptions
{
    /// <summary>
    /// 断路器打开异常，表示服务处于熔断状态，拒绝请求
    /// </summary>
    [Serializable]
    public class CircuitBreakerOpenException : Exception
    {
        /// <summary>
        /// 初始化CircuitBreakerOpenException类的新实例
        /// </summary>
        public CircuitBreakerOpenException() : base("断路器处于打开状态，拒绝请求")
        {}

        /// <summary>
        /// 使用指定的错误消息初始化CircuitBreakerOpenException类的新实例
        /// </summary>
        /// <param name="message">描述错误的消息</param>
        public CircuitBreakerOpenException(string message) : base(message)
        {}

        /// <summary>
        /// 使用指定的错误消息和对作为此异常原因的内部异常的引用来初始化CircuitBreakerOpenException类的新实例
        /// </summary>
        /// <param name="message">解释异常原因的错误消息</param>
        /// <param name="innerException">导致当前异常的异常</param>
        public CircuitBreakerOpenException(string message, Exception innerException) : base(message, innerException)
        {}

        /// <summary>
        /// 用序列化数据初始化CircuitBreakerOpenException类的新实例
        /// </summary>
        /// <param name="info">包含有关序列化异常的信息</param>
        /// <param name="context">包含有关源或目标的上下文信息</param>
        protected CircuitBreakerOpenException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
}