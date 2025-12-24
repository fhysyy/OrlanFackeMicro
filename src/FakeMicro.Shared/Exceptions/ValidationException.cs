using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace FakeMicro.Shared.Exceptions
{
    /// <summary>
    /// 验证异常类，用于表示数据验证失败
    /// </summary>
    [Serializable]
    public class ValidationException : CustomException
    {
        /// <summary>
        /// 获取或设置验证错误列表
        /// </summary>
        public ICollection<ValidationError> ValidationErrors { get; private set; }

        /// <summary>
        /// 初始化ValidationException类的新实例
        /// </summary>
        public ValidationException()
            : base("数据验证失败", "validation_error", 400)
        {
            ValidationErrors = new List<ValidationError>();
        }

        /// <summary>
        /// 使用指定的错误消息初始化ValidationException类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        public ValidationException(string message)
            : base(message, "validation_error", 400)
        {
            ValidationErrors = new List<ValidationError>();
        }

        /// <summary>
        /// 使用指定的错误消息和验证错误列表初始化ValidationException类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="validationErrors">验证错误列表</param>
        public ValidationException(string message, ICollection<ValidationError> validationErrors)
            : base(message, "validation_error", 400)
        {
            ValidationErrors = validationErrors ?? new List<ValidationError>();
        }

        /// <summary>
        /// 使用指定的字段名和错误消息初始化ValidationException类的新实例
        /// </summary>
        /// <param name="fieldName">字段名</param>
        /// <param name="errorMessage">错误消息</param>
        public ValidationException(string fieldName, string errorMessage)
            : base($"字段 '{fieldName}' 验证失败: {errorMessage}", "validation_error", 400)
        {
            ValidationErrors = new List<ValidationError> { new ValidationError(fieldName, errorMessage) };
        }

        /// <summary>
        /// 使用指定的错误消息、错误代码和HTTP状态码初始化ValidationException类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="errorCode">错误代码</param>
        /// <param name="httpStatusCode">HTTP状态码</param>
        public ValidationException(string message, string errorCode, int httpStatusCode)
            : base(message, errorCode, httpStatusCode)
        {
            ValidationErrors = new List<ValidationError>();
        }

        /// <summary>
        /// 使用指定的错误消息和内部异常初始化ValidationException类的新实例
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="innerException">内部异常</param>
        public ValidationException(string message, Exception innerException)
            : base(message, "validation_error", innerException)
        {
            HttpStatusCode = 400;
            ValidationErrors = new List<ValidationError>();
        }

        /// <summary>
        /// 使用序列化数据初始化ValidationException类的新实例
        /// </summary>
        /// <param name="info">序列化信息</param>
        /// <param name="context">流上下文</param>
        protected ValidationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ValidationErrors = (ICollection<ValidationError>)info.GetValue(nameof(ValidationErrors), typeof(ICollection<ValidationError>))!;
        }

        /// <summary>
        /// 设置序列化所需的数据
        /// </summary>
        /// <param name="info">序列化信息</param>
        /// <param name="context">流上下文</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(ValidationErrors), ValidationErrors);
            base.GetObjectData(info, context);
        }
    }

    /// <summary>
    /// 验证错误类，用于表示单个字段的验证错误
    /// </summary>
    [Serializable]
    public class ValidationError
    {
        /// <summary>
        /// 获取或设置字段名
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// 获取或设置错误消息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 初始化ValidationError类的新实例
        /// </summary>
        /// <param name="fieldName">字段名</param>
        /// <param name="errorMessage">错误消息</param>
        public ValidationError(string fieldName, string errorMessage)
        {
            FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
            ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
        }

        /// <summary>
        /// 使用序列化数据初始化ValidationError类的新实例
        /// </summary>
        /// <param name="info">序列化信息</param>
        /// <param name="context">流上下文</param>
        protected ValidationError(SerializationInfo info, StreamingContext context)
        {
            FieldName = info.GetString(nameof(FieldName))!;
            ErrorMessage = info.GetString(nameof(ErrorMessage))!;
        }

        /// <summary>
        /// 设置序列化所需的数据
        /// </summary>
        /// <param name="info">序列化信息</param>
        /// <param name="context">流上下文</param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(FieldName), FieldName);
            info.AddValue(nameof(ErrorMessage), ErrorMessage);
        }
    }
}