using System;
using System.Collections.Generic;
using System.Text;
using Orleans;

namespace FakeMicro.Interfaces.Models.Results
{
    /// <summary>
    /// 基础结果类 - 所有操作结果类的基类
    /// 提供统一的成功/失败状态和错误信息处理
    /// </summary>
    [GenerateSerializer]
    public abstract class BaseResult
    {
        /// <summary>
        /// 操作是否成功
        /// </summary>
        [Id(0)]
        public bool Success { get; set; }

        /// <summary>
        /// 错误信息（操作失败时）
        /// </summary>
        [Id(1)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 错误代码（可选）
        /// </summary>
        [Id(2)]
        public string? ErrorCode { get; set; }

        /// <summary>
        /// 操作时间戳
        /// </summary>
        [Id(3)]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 额外的元数据（可选）
        /// </summary>
        [Id(4)]
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        /// <typeparam name="T">具体的结果类型</typeparam>
        /// <returns>成功的结果实例</returns>
        public static T CreateSuccess<T>() where T : BaseResult, new()
        {
            return new T
            {
                Success = true,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        /// <typeparam name="T">具体的结果类型</typeparam>
        /// <param name="errorMessage">错误信息</param>
        /// <param name="errorCode">错误代码（可选）</param>
        /// <returns>失败的结果实例</returns>
        public static T CreateFailed<T>(string errorMessage, string? errorCode = null) where T : BaseResult, new()
        {
            return new T
            {
                Success = false,
                ErrorMessage = errorMessage,
                ErrorCode = errorCode,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 添加元数据
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public void AddMetadata(string key, object value)
        {
            Metadata ??= new Dictionary<string, object>();
            Metadata[key] = value;
        }

        /// <summary>
        /// 获取元数据
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>元数据值</returns>
        public T? GetMetadata<T>(string key, T? defaultValue = default)
        {
            if (Metadata?.TryGetValue(key, out var value) == true && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// 隐式转换为布尔值（成功状态）
        /// </summary>
        /// <param name="result">结果实例</param>
        public static implicit operator bool(BaseResult result)
        {
            return result?.Success ?? false;
        }

        /// <summary>
        /// 重写ToString方法
        /// </summary>
        /// <returns>字符串表示</returns>
        public override string ToString()
        {
            return Success 
                ? $"Success (at {Timestamp:yyyy-MM-dd HH:mm:ss})" 
                : $"Failed: {ErrorMessage} (at {Timestamp:yyyy-MM-dd HH:mm:ss})";
        }
    }

    /// <summary>
    /// 带有数据的基础结果类
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    [GenerateSerializer]
    public abstract class BaseResult<T> : BaseResult
    {
        /// <summary>
        /// 操作返回的数据
        /// </summary>
        [Id(5)]
        public T? Data { get; set; }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        /// <typeparam name="TResult">具体的结果类型</typeparam>
        /// <param name="data">返回的数据</param>
        /// <returns>成功的结果实例</returns>
        public static TResult CreateSuccess<TResult>(T data) where TResult : BaseResult<T>, new()
        {
            return new TResult
            {
                Success = true,
                Data = data,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 重写ToString方法
        /// </summary>
        /// <returns>字符串表示</returns>
        public override string ToString()
        {
            var baseString = base.ToString();
            return Success 
                ? $"{baseString}, Data: {Data}" 
                : baseString;
        }
    }
}