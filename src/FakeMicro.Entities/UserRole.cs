using System.ComponentModel.DataAnnotations;
using Orleans;
using Orleans.Serialization;
using SqlSugar;

namespace FakeMicro.Entities;

/// <summary>
/// 用户角色关联实体
/// </summary>
[SugarTable("user_roles")]
[GenerateSerializer]
public class UserRole : IAuditable
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, ColumnName = "user_id")]
    [Required]
    [Id(0)]
    public long user_id { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, ColumnName = "role_id")]
    [Required]
    [Id(1)]
    public long role_id { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(ColumnName = "CreatedAt")]
    [Id(2)]
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// 更新时间
    /// </summary>
    [SugarColumn(ColumnName = "UpdatedAt")]
    [Id(3)]
    public DateTime? UpdatedAt { get; set; }


    [Id(4)]
    [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "created_by")]
    public string created_by { get; set; }

    [Id(5)]

    [SqlSugar.SugarColumn(IsNullable = true, ColumnName = "updated_by")]
    public string updated_by { get; set; }

}