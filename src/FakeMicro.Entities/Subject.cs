using System.ComponentModel.DataAnnotations;
using Orleans;
using Orleans.Serialization;
using SqlSugar;

namespace FakeMicro.Entities;

/// <summary>
/// 科目实体
/// </summary>
[SugarTable("Subjects")]
[GenerateSerializer]
public class Subject : IAuditable
{
    /// <summary>
    /// 科目ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, ColumnName = "id")]
    [Id(0)]
    public int id { get; set; }

    /// <summary>
    /// 科目名称
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Id(1)]
    public string name { get; set; } = string.Empty;

    /// <summary>
    /// 科目代码
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Id(2)]
    public string code { get; set; } = string.Empty;

    /// <summary>
    /// 科目描述
    /// </summary>
    [MaxLength(200)]
    [Id(3)]
    public string? description { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [Id(4)]
    public DateTime created_at { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [Id(5)]
    public DateTime updated_at { get; set; }
}