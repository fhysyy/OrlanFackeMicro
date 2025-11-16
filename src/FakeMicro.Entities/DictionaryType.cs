using Orleans;
using System.ComponentModel.DataAnnotations;
using SqlSugar;

namespace FakeMicro.Entities
{
    /// <summary>
    /// 字典类型实体
    /// </summary>
    [SugarTable("DictionaryTypes")]
    [GenerateSerializer]
    public class DictionaryType
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        [Id(0)]
        public long Id { get; set; }

        /// <summary>
        /// 字典类型编码
        /// </summary>
        [Required]
        [Id(1)]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 字典类型名称
        /// </summary>
        [Required]
        [Id(2)]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        [Id(3)]
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [Id(4)]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        [Id(5)]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [Id(6)]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 排序号
        /// </summary>
        [Id(7)]
        public int SortOrder { get; set; }
    }
}