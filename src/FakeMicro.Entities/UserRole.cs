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
    [SugarColumn(ColumnName = "created_at")]
    [Id(2)]
    public DateTime created_at { get; set; }
    
    /// <summary>
    /// 更新时间
    /// </summary>
    [SugarColumn(ColumnName = "updated_at")]
    [Id(3)]
    public DateTime updated_at { get; set; }
}