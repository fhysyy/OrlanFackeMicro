using System.ComponentModel.DataAnnotations;
using Orleans;
using Orleans.Serialization;
using SqlSugar;

namespace FakeMicro.Entities;

/// <summary>
/// 科目实体
/// </summary>
[SugarTable("subjects")]
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
    [SqlSugar.SugarColumn(IsNullable = true,ColumnName ="name")]
    public string name { get; set; } = string.Empty;

    /// <summary>
    /// 科目代码
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Id(2)]
    [SqlSugar.SugarColumn(IsNullable = true,ColumnName = "code")]
    public string code { get; set; } = string.Empty;
   
    /// <summary>
    /// 科目描述
    /// </summary>
    [MaxLength(200)]
    [Id(3)]
    [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "description")]
    public string? description { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [Id(4)]
    [SqlSugar.SugarColumn(IsNullable = true,ColumnName = "created_at")]
    public DateTime created_at { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [Id(5)]
    [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "updated_at")]
    public DateTime? updated_at { get; set; }
    
    
    [Id(6)]
    [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "created_by")]
    public string created_by { get; set; }

    [Id(7)]

    [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "updated_by")]
    public string updated_by { get; set; }
}