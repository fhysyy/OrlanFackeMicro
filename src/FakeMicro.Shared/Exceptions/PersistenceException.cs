 using System;
using System.Runtime.Serialization;

namespace FakeMicro.Shared.Exceptions
{
    /// <summary>
    /// 持久化操作异常，用于封装Orleans状态存储相关的错误
    /// </summary>
    [Serializable]
    public class PersistenceException : Exception
    {
        /// <summary>
        /// 初始化PersistenceException类的新实例
        /// </summary>
        public PersistenceException() : base("持久化操作失败")
        {}

        /// <summary>
        /// 使用指定的错误消息初始化PersistenceException类的新实例
        /// </summary>
        /// <param name="message">描述错误的消息</param>
        public PersistenceException(string message) : base(message)
        {}

        /// <summary>
        /// 使用指定的错误消息和对作为此异常原因的内部异常的引用来初始化PersistenceException类的新实例
        /// </summary>
        /// <param name="message">解释异常原因的错误消息</param>
        /// <param name="innerException">导致当前异常的异常</param>
        public PersistenceException(string message, Exception innerException) : base(message, innerException)
        {}

        /// <summary>
        /// 用序列化数据初始化PersistenceException类的新实例
        /// </summary>
        /// <param name="info">包含有关序列化异常的信息</param>
        /// <param name="context">包含有关源或目标的上下文信息</param>
        protected PersistenceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }

    /// <summary>
    /// 并发更新异常，表示在尝试更新状态时发生了并发冲突
    /// </summary>
    [Serializable]
    public class ConcurrencyException : PersistenceException
    {
        /// <summary>
        /// 初始化ConcurrencyException类的新实例
        /// </summary>
        public ConcurrencyException() : base("并发更新冲突")
        {}

        /// <summary>
        /// 使用指定的错误消息初始化ConcurrencyException类的新实例
        /// </summary>
        /// <param name="message">描述错误的消息</param>
        public ConcurrencyException(string message) : base(message)
        {}

        /// <summary>
        /// 使用指定的错误消息和对作为此异常原因的内部异常的引用来初始化ConcurrencyException类的新实例
        /// </summary>
        /// <param name="message">解释异常原因的错误消息</param>
        /// <param name="innerException">导致当前异常的异常</param>
        public ConcurrencyException(string message, Exception innerException) : base(message, innerException)
        {}

        /// <summary>
        /// 用序列化数据初始化ConcurrencyException类的新实例
        /// </summary>
        /// <param name="info">包含有关序列化异常的信息</param>
        /// <param name="context">包含有关源或目标的上下文信息</param>
        protected ConcurrencyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }

    /// <summary>
    /// 状态验证异常，表示状态数据不符合预期格式或条件
    /// </summary>
    [Serializable]
    public class StateValidationException : PersistenceException
    {
        /// <summary>
        /// 初始化StateValidationException类的新实例
        /// </summary>
        public StateValidationException() : base("状态验证失败")
        {}

        /// <summary>
        /// 使用指定的错误消息初始化StateValidationException类的新实例
        /// </summary>
        /// <param name="message">描述错误的消息</param>
        public StateValidationException(string message) : base(message)
        {}

        /// <summary>
        /// 使用指定的错误消息和对作为此异常原因的内部异常的引用来初始化StateValidationException类的新实例
        /// </summary>
        /// <param name="message">解释异常原因的错误消息</param>
        /// <param name="innerException">导致当前异常的异常</param>
        public StateValidationException(string message, Exception innerException) : base(message, innerException)
        {}

        /// <summary>
        /// 用序列化数据初始化StateValidationException类的新实例
        /// </summary>
        /// <param name="info">包含有关序列化异常的信息</param>
        /// <param name="context">包含有关源或目标的上下文信息</param>
        protected StateValidationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {}
    }
}