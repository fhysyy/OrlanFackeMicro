using Orleans;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

namespace FakeMicro.Entities
{
    /// <summary>
    /// 笔记本实体类
    /// </summary>
    [GenerateSerializer]
    [SugarTable("notebooks")]
    public class Notebook : IAuditable
    {
        /// <summary>
        /// 笔记本ID
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
        /// 父笔记本ID（支持嵌套）
        /// </summary>
        [Id(2)]
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        [SugarColumn(ColumnName = "parent_id", IsNullable = true)]
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 排序顺序
        /// </summary>
        [Id(4)]
        [SugarColumn(ColumnName = "sort_order")]
        public int SortOrder { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Id(5)]
        [SugarColumn(ColumnName = "created_at")]
        public DateTime created_at { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 更新时间
        /// </summary>
        [Id(6)]
        [SugarColumn(ColumnName = "updated_at")]
        public DateTime? updated_at { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 创建人
        /// </summary>
        [Id(7)]
        [SugarColumn(ColumnName = "created_by", IsNullable = true)]
        public string created_by { get; set; } = string.Empty;

        /// <summary>
        /// 更新人
        /// </summary>
        [Id(8)]
        [SugarColumn(ColumnName = "updated_by", IsNullable = true)]
        public string? updated_by { get; set; }
    }
}