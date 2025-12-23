using System;
using System.ComponentModel.DataAnnotations;
using Orleans;
using MongoDB.Bson.Serialization.Attributes;

namespace FakeMicro.Entities.KnowledgeBase
{
    /// <summary>
    /// 知识库条目
    /// </summary>
    [GenerateSerializer]
    [BsonIgnoreExtraElements]
    public class KnowledgeBaseItem
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Id(0)]
        [Required]
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }

        /// <summary>
        /// 条目类型
        /// </summary>
        [Id(1)]
        [Required]
        public string Type { get; set; }

        /// <summary>
        /// 来源ID（如实体ID）
        /// </summary>
        [Id(2)]
        public string SourceId { get; set; }

        /// <summary>
        /// 标题
        /// </summary>
        [Id(3)]
        [Required]
        public string Title { get; set; }

        /// <summary>
        /// 内容摘要
        /// </summary>
        [Id(4)]
        public string Summary { get; set; }

        /// <summary>
        /// 详细内容
        /// </summary>
        [Id(5)]
        public string Content { get; set; }

        /// <summary>
        /// 关键词
        /// </summary>
        [Id(6)]
        public string Keywords { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Id(7)]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [Id(8)]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 最后分析时间
        /// </summary>
        [Id(9)]
        public DateTime? LastAnalyzedAt { get; set; }

        /// <summary>
        /// 分析状态
        /// </summary>
        [Id(10)]
        public string AnalysisStatus { get; set; }
    }
}