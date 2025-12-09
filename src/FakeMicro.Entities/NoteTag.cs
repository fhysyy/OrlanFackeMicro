using Orleans;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

namespace FakeMicro.Entities
{
    /// <summary>
    /// 笔记标签关联实体类
    /// </summary>
    [GenerateSerializer]
    [SugarTable("note_tags")]
    public class NoteTag : IAuditable
    {
        /// <summary>
        /// 笔记ID
        /// </summary>
        [Id(0)]
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        [SugarColumn(IsPrimaryKey = true, ColumnName = "note_id")]
        public Guid NoteId { get; set; }

        /// <summary>
        /// 标签ID
        /// </summary>
        [Id(1)]
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        [SugarColumn(IsPrimaryKey = true, ColumnName = "tag_id")]
        public Guid TagId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Id(2)]
        [SugarColumn(ColumnName = "CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 创建人
        /// </summary>
        [Id(3)]
        [SugarColumn(ColumnName = "created_by")]
        public string created_by { get; set; } = string.Empty;

        /// <summary>
        /// 更新时间
        /// </summary>
        [Id(4)]
        [SugarColumn(ColumnName = "UpdatedAt", IsNullable = true)]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 更新人
        /// </summary>
        [Id(5)]
        [SugarColumn(ColumnName = "updated_by", IsNullable = true)]
        public string? updated_by { get; set; }
    }
}