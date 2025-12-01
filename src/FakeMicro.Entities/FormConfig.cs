using FakeMicro.Entities.Enums;
using Orleans;
using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace FakeMicro.Entities
{
    /// <summary>
    /// 表单配置实体
    /// </summary>
    [GenerateSerializer]
    [SugarTable("form_configs")]
    public class FormConfig : IAuditable, ISoftDeletable
    {
        /// <summary>
        /// 表单配置ID
        /// </summary>
        [SqlSugar.SugarColumn(IsPrimaryKey = true, ColumnName = "id")]
        [Id(0)]
        public string id { get; set; }

        /// <summary>
        /// 表单编码
        /// </summary>
        [MaxLength(50)]
        [Id(1)]
        [SugarColumn(IsNullable = true, ColumnName = "code")]
        public string code { get; set; } = string.Empty;

        /// <summary>
        /// 表单名称
        /// </summary>
        [MaxLength(200)]
        [Id(2)]
        [SugarColumn(IsNullable = true, ColumnName = "name")]
        public string name { get; set; } = string.Empty;

        /// <summary>
        /// 表单描述
        /// </summary>
        [MaxLength(500)]
        [Id(3)]
        [SugarColumn(IsNullable = true, ColumnName = "description")]
        public string description { get; set; } = string.Empty;

        /// <summary>
        /// 表单布局配置
        /// </summary>
        [Id(4)]
        [SugarColumn(IsNullable = true, ColumnName = "layout_config", ColumnDataType = "json")]
        public string layout_config { get; set; } = string.Empty;

        /// <summary>
        /// 表单状态
        /// </summary>
        [Id(5)]
        [SugarColumn(IsNullable = true, ColumnName = "status")]
        public FormConfigStatus status { get; set; } = FormConfigStatus.Draft;

        /// <summary>
        /// 版本号
        /// </summary>
        [Id(6)]
        [SugarColumn(IsNullable = true, ColumnName = "version")]
        public int version { get; set; } = 1;

        /// <summary>
        /// 是否为最新版本
        /// </summary>
        [Id(7)]
        [SugarColumn(IsNullable = true, ColumnName = "is_latest")]
        public bool is_latest { get; set; } = true;

        /// <summary>
        /// 是否启用
        /// </summary>
        [Id(8)]
        [SugarColumn(IsNullable = true, ColumnName = "is_enabled")]
        public bool is_enabled { get; set; } = false;

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Id(9)]
        [SugarColumn(IsNullable = true, ColumnName = "created_at")]
        public DateTime created_at { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 更新时间
        /// </summary>
        [Id(10)]
        [SugarColumn(IsNullable = true, ColumnName = "updated_at")]
        public DateTime? updated_at { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 是否删除
        /// </summary>
        [Id(11)]
        [SugarColumn(IsNullable = true, ColumnName = "is_deleted")]
        public bool is_deleted { get; set; } = false;

        /// <summary>
        /// 删除时间
        /// </summary>
        [Id(12)]
        [SugarColumn(IsNullable = true, ColumnName = "deleted_at")]
        public DateTime? deleted_at { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        [Id(13)]
        [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "created_by")]
        public string created_by { get; set; }

        /// <summary>
        /// 更新人
        /// </summary>
        [Id(14)]
        [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "updated_by")]
        public string updated_by { get; set; }
    }
}