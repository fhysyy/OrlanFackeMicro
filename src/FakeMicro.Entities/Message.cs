using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Orleans;
using Orleans.Serialization;
using FakeMicro.Entities.Enums;
using SqlSugar;

namespace FakeMicro.Entities
{
    /// <summary>
    /// 消息实体
    /// </summary>
    [SugarTable("messages")]
    [GenerateSerializer]
    public class Message : IAuditable
    {
        /// <summary>
        /// 消息ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, ColumnName = "id")]
        [Id(0)]
        public long id { get; set; }
        
        /// <summary>
        /// 发送者ID
        /// </summary>
        [SqlSugar.SugarColumn(ColumnName = "sender_id")]
        [Id(1)]
        public long sender_id { get; set; }
        
        /// <summary>
        /// 接收者ID（可为空，用于站内信）
        /// </summary>
        [SqlSugar.SugarColumn(ColumnName = "receiver_id")]
        [Id(2)]
        public long? receiver_id { get; set; }
        
        /// <summary>
        /// 消息标题
        /// </summary>
        [Required]
        [MaxLength(500)]
        [SqlSugar.SugarColumn(ColumnName = "title")]
        [Id(3)]
        public string title { get; set; } = string.Empty;
        
        /// <summary>
        /// 消息内容
        /// </summary>
        [Required]
        [SqlSugar.SugarColumn(ColumnName = "content")]
        [Id(4)]
        public string content { get; set; } = string.Empty;
        
        /// <summary>
        /// 消息类型
        /// </summary>
        [MaxLength(20)]
        [SqlSugar.SugarColumn(ColumnName = "message_type")]
        [Id(5)]
        public string message_type { get; set; } = string.Empty;
        
        /// <summary>
        /// 消息通道
        /// </summary>
        [MaxLength(20)]
        [SqlSugar.SugarColumn(ColumnName = "message_channel")]
        [Id(6)]
        public string message_channel { get; set; } = string.Empty;
        
        /// <summary>
        /// 消息状态
        /// </summary>
        [MaxLength(20)]
        [SqlSugar.SugarColumn(ColumnName = "status")]
        [Id(7)]
        public string status { get; set; } = "Pending";
        
        /// <summary>
        /// 发送时间
        /// </summary>
        [SqlSugar.SugarColumn(ColumnName = "sent_at")]
        [Id(8)]
        public DateTime? sent_at { get; set; }
        
        /// <summary>
        /// 投递时间
        /// </summary>
        [SqlSugar.SugarColumn(ColumnName = "delivered_at")]
        [Id(9)]
        public DateTime? delivered_at { get; set; }
        
        /// <summary>
        /// 阅读时间
        /// </summary>
        [SqlSugar.SugarColumn(ColumnName = "read_at")]
        [Id(10)]
        public DateTime? read_at { get; set; }
        
        /// <summary>
        /// 失败时间
        /// </summary>
        [SqlSugar.SugarColumn(ColumnName = "failed_at")]
        [Id(11)]
        public DateTime? failed_at { get; set; }
        
        /// <summary>
        /// 重试次数
        /// </summary>
        [SqlSugar.SugarColumn(ColumnName = "retry_count")]
        [Id(12)]
        public int retry_count { get; set; }
        
        /// <summary>
        /// 错误信息
        /// </summary>
        [SqlSugar.SugarColumn(ColumnName = "error_message")]
        [Id(13)]
        public string? error_message { get; set; }
        
        /// <summary>
        /// 元数据（JSON格式）
        /// </summary>
        [SqlSugar.SugarColumn(ColumnName = "metadata")]
        [Id(14)]
        public string? metadata { get; set; }
        
        /// <summary>
        /// 计划发送时间（用于延迟发送）
        /// </summary>
        [SqlSugar.SugarColumn(ColumnName = "scheduled_at")]
        [Id(15)]
        public DateTime? scheduled_at { get; set; }
        
        /// <summary>
        /// 过期时间
        /// </summary>
        [SqlSugar.SugarColumn(ColumnName = "expires_at")]
        [Id(16)]
        public DateTime? expires_at { get; set; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        [SqlSugar.SugarColumn(ColumnName = "CreatedAt")]
        [Id(17)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// 更新时间
        /// </summary>
        [SqlSugar.SugarColumn(ColumnName = "UpdatedAt")]
        [Id(18)]
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// 发送者用户信息
        /// </summary>
        [JsonIgnore]
        [SugarColumn(IsIgnore = true)]
        [Id(19)]
        public User? Sender { get; set; }
        
        /// <summary>
        /// 接收者用户信息
        /// </summary>
        [JsonIgnore]
        [SugarColumn(IsIgnore = true)]
        [Id(20)]
        public User? Receiver { get; set; }

        [Id(21)]
        [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "CreatedBy")]
        public string CreatedBy { get; set; }

        [Id(22)]

        [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "UpdatedBy")]
        public string UpdatedBy { get; set; }

    }

    /// <summary>
    /// 消息模板实体
    /// </summary>
    [SugarTable("message_templates")]
    [GenerateSerializer]
    public class MessageTemplate : IAuditable, ISoftDeletable
    {
        /// <summary>
        /// 模板ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        [Id(0)]
        public long id { get; set; }
        
        /// <summary>
        /// 模板名称
        /// </summary>
        [Required]
        [MaxLength(100)]
        [SqlSugar.SugarColumn(ColumnName = "name")]
        [Id(1)]
        public string name { get; set; } = string.Empty;
        
        /// <summary>
        /// 模板代码（唯一标识）
        /// </summary>
        [Required]
        [MaxLength(50)]
        [SqlSugar.SugarColumn(ColumnName = "code")]
        [Id(2)]
        public string code { get; set; } = string.Empty;
        
        /// <summary>
        /// 模板标题
        /// </summary>
        [Required]
        [MaxLength(500)]
        [SqlSugar.SugarColumn(ColumnName = "title")]
        [Id(3)]
        public string title { get; set; } = string.Empty;
        
        /// <summary>
        /// 模板内容
        /// </summary>
        [Required]
        [SqlSugar.SugarColumn(ColumnName = "content")]
        [Id(4)]
        public string content { get; set; } = string.Empty;
        
        /// <summary>
        /// 消息类型
        /// </summary>
        [MaxLength(20)]
        [SqlSugar.SugarColumn(ColumnName = "message_type")]
        [Id(5)]
        public string message_type { get; set; } = string.Empty;
        
        /// <summary>
        /// 模板变量（JSON格式）
        /// </summary>
        [SqlSugar.SugarColumn(ColumnName = "variables")]
        [Id(6)]
        public string? variables { get; set; }
        
        /// <summary>
        /// 是否启用
        /// </summary>
        [SqlSugar.SugarColumn(ColumnName = "is_enabled")]
        [Id(7)]
        public bool is_enabled { get; set; } = true;
        
        /// <summary>
        /// 创建时间
        /// </summary>
        [SqlSugar.SugarColumn(ColumnName = "CreatedAt")]
        [Id(8)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// 更新时间
        /// </summary>
        [SqlSugar.SugarColumn(ColumnName = "UpdatedAt")]
        [Id(9)]
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// 是否删除
        /// </summary>
        [Id(10)]
        public bool is_deleted { get; set; } = false;
        
        /// <summary>
        /// 删除时间
        /// </summary>
        [Id(11)]
        public DateTime? deleted_at { get; set; }
       

      

        [Id(12)]
        [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "CreatedBy")]
        public string CreatedBy { get; set; }

        [Id(13)]

        [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "UpdatedBy")]
        public string UpdatedBy { get; set; }
    }
}