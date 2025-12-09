using Orleans;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

namespace FakeMicro.Entities
{
    /// <summary>
    /// 标签实体类
    /// </summary>
    [GenerateSerializer]
    [SugarTable("tags")]
    public class Tag : IAuditable
    {
        /// <summary>
        /// 标签ID
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
        /// 标签名称
        /// </summary>
        [MaxLength(100)]
        [Id(2)]
        [SugarColumn(ColumnName = "name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 标签颜色
        /// </summary>
        [MaxLength(7)]
        [Id(3)]
        [SugarColumn(ColumnName = "color")]
        public string Color { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Id(4)]
        [SugarColumn(ColumnName = "created_at")]
        public DateTime created_at { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 更新时间
        /// </summary>
        [Id(5)]
        [SugarColumn(ColumnName = "updated_at")]
        public DateTime? updated_at { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 创建人
        /// </summary>
        [Id(6)]
        [SugarColumn(ColumnName = "created_by", IsNullable = true)]
        public string created_by { get; set; } = string.Empty;

        /// <summary>
        /// 更新人
        /// </summary>
        [Id(7)]
        [SugarColumn(ColumnName = "updated_by", IsNullable = true)]
        public string? updated_by { get; set; }
    }
}