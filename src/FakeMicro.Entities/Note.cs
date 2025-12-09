using Orleans;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

namespace FakeMicro.Entities
{
    /// <summary>
    /// 笔记实体类
    /// </summary>
    [GenerateSerializer]
    [SugarTable("notes")]
    public class Note : IAuditable
    {
        /// <summary>
        /// 笔记ID
        /// </summary>
        [Id(0)]
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        [SugarColumn(IsPrimaryKey = true, ColumnName = "id")]
        public Guid Id { get; set; }
        
        /// <summary>
        /// 用户ID
        /// </summary>
        [Id(1)]
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        [SugarColumn(ColumnName = "user_id")]
        public Guid UserId { get; set; }
        
        /// <summary>
        /// 笔记本ID
        /// </summary>
        [Id(2)]
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        [SugarColumn(ColumnName = "notebook_id")]
        public Guid NotebookId { get; set; }

        /// <summary>
        /// 笔记标题
        /// </summary>
        [MaxLength(1000)]
        [Id(3)]
        [SugarColumn(ColumnName = "title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 内容哈希值（用于快速比较变化）
        /// </summary>
        [MaxLength(64)]
        [Id(4)]
        [SugarColumn(ColumnName = "content_hash")]
        public string ContentHash { get; set; } = string.Empty;

        /// <summary>
        /// 内容CRDT二进制数据
        /// </summary>
        [Id(5)]
        [SugarColumn(ColumnName = "content_crdt", ColumnDataType = "varbinary(max)")]
        public byte[] ContentCrdt { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// 元数据（存储标签、星标等）
        /// </summary>
        [Id(6)]
        [SugarColumn(ColumnName = "metadata", ColumnDataType = "json")]
        public string Metadata { get; set; } = "{}";

        /// <summary>
        /// 版本号
        /// </summary>
        [Id(7)]
        [SugarColumn(ColumnName = "version", DefaultValue = "0")]
        public int Version { get; set; } = 0;

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Id(8)]
        [SugarColumn(ColumnName = "created_at")]
        public DateTime created_at { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 更新时间
        /// </summary>
        [Id(9)]
        [SugarColumn(ColumnName = "updated_at")]
        public DateTime? updated_at { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 删除时间
        /// </summary>
        [Id(10)]
        [SugarColumn(ColumnName = "deleted_at", IsNullable = true)]
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// 是否删除
        /// </summary>
        [Id(11)]
        [SugarColumn(ColumnName = "is_deleted")]
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// 创建人
        /// </summary>
        [Id(12)]
        [SugarColumn(ColumnName = "created_by", IsNullable = true)]
        public string created_by { get; set; } = string.Empty;

        /// <summary>
        /// 更新人
        /// </summary>
        [Id(13)]
        [SugarColumn(ColumnName = "updated_by", IsNullable = true)]
        public string? updated_by { get; set; }
    }
}