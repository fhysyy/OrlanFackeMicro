using System;
using System.ComponentModel.DataAnnotations;
using Orleans;
using MongoDB.Bson.Serialization.Attributes;

namespace FakeMicro.Entities.KnowledgeBase
{
    /// <summary>
    /// 知识库摘要
    /// </summary>
    [GenerateSerializer]
    [BsonIgnoreExtraElements]
    public class KnowledgeBaseSummary
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
        /// 摘要类型（如：用户总结、内容统计、系统分析等）
        /// </summary>
        [Id(1)]
        [Required]
        public string SummaryType { get; set; }

        /// <summary>
        /// 摘要标题
        /// </summary>
        [Id(2)]
        [Required]
        public string Title { get; set; }

        /// <summary>
        /// 摘要内容
        /// </summary>
        [Id(3)]
        public string Content { get; set; }

        /// <summary>
        /// 统计数据
        /// </summary>
        [Id(4)]
        public string StatisticsData { get; set; }

        /// <summary>
        /// 分析范围（如：时间范围、实体类型等）
        /// </summary>
        [Id(5)]
        public string AnalysisScope { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Id(6)]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [Id(7)]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 生成方式（如：自动、手动）
        /// </summary>
        [Id(8)]
        public string GenerationMethod { get; set; }

        /// <summary>
        /// 生成者ID
        /// </summary>
        [Id(9)]
        public string GeneratedBy { get; set; }
    }
}