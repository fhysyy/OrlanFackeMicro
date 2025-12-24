using System;
using System.Text.Json.Serialization;
using FakeMicro.Entities;

namespace FakeMicro.DatabaseAccess.Entities
{
    /// <summary>
    /// 幂等性请求实体
    /// 用于持久化存储请求的幂等性键和响应结果
    /// </summary>
    public class IdempotentRequest
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 幂等性键
        /// 由客户端生成并在请求头X-Idempotency-Key中传递
        /// </summary>
        public string IdempotencyKey { get; set; }

        /// <summary>
        /// 用户ID
        /// 用于区分不同用户的相同幂等性键
        /// </summary>
        public long? UserId { get; set; }

        /// <summary>
        /// HTTP方法
        /// </summary>
        public string HttpMethod { get; set; }

        /// <summary>
        /// 请求路径
        /// </summary>
        public string RequestPath { get; set; }

        /// <summary>
        /// 响应状态码
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// 响应头
        /// JSON格式存储
        /// </summary>
        public string ResponseHeaders { get; set; }

        /// <summary>
        /// 响应内容
        /// JSON格式存储
        /// </summary>
        public string ResponseBody { get; set; }

        /// <summary>
        /// 请求创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 请求过期时间
        /// 默认24小时后过期
        /// </summary>
        public DateTime ExpiresAt { get; set; }
    }
}
