using Orleans;
using System.ComponentModel.DataAnnotations;
using SqlSugar;

namespace FakeMicro.Entities
{
    /// <summary>
    /// 字典项实体
    /// </summary>
    [SugarTable("DictionaryItems")]
    [GenerateSerializer]
    public class DictionaryItem
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, ColumnName = "Id")]
        [Id(0)]
        public long Id { get; set; }

        /// <summary>
        /// 字典类型ID
        /// </summary>
        [Required]
        [Id(1)]
        public long DictionaryTypeId { get; set; }

        /// <summary>
        /// 字典项值
        /// </summary>
        [Required]
        [Id(2)]
        [StringLength(100)]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 字典项文本
        /// </summary>
        [Required]
        [Id(3)]
        [StringLength(100)]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        [Id(4)]
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [Id(5)]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 排序号
        /// </summary>
        [Id(6)]
        public int SortOrder { get; set; }

        /// <summary>
        /// 额外数据（JSON格式）
        /// </summary>
        [Id(7)]
        [StringLength(1000)]
        public string? ExtraData { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Id(8)]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [Id(9)]
        public DateTime UpdatedAt { get; set; }
    }
}