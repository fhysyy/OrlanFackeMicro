using System.ComponentModel.DataAnnotations;
using Orleans;
using Orleans.Serialization;

namespace FakeMicro.Entities;

/// <summary>
/// 角色实体
/// </summary>
[GenerateSerializer]
[SqlSugar.SugarTable("roles")]
public class Role :IAuditable, ISoftDeletable
{
    /// <summary>
    /// 角色ID
    /// </summary>
    [SqlSugar.SugarColumn(IsPrimaryKey = true,IsIdentity =true, ColumnName = "id")]
    [Id(0)]
    public long id { get; set; }

    /// <summary>
    /// 角色名称
    /// </summary>
    [Required]
    [MaxLength(50)]
    [SqlSugar.SugarColumn(ColumnName = "name", IsNullable = true)]
    [Id(1)]
    public string name { get; set; } = string.Empty;

    /// <summary>
    /// 角色代码
    /// </summary>
    [Required]
    [MaxLength(20)]
    [SqlSugar.SugarColumn(ColumnName = "code", IsNullable = true)]
    [Id(2)]
    public string code { get; set; } = string.Empty;

    /// <summary>
    /// 角色描述
    /// </summary>
    [MaxLength(200)]
    [SqlSugar.SugarColumn(ColumnName = "description", IsNullable = true)]
    [Id(3)]
    public string? description { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [SqlSugar.SugarColumn(ColumnName = "is_enabled", IsNullable = true)]
    [Id(4)]
    public bool is_enabled { get; set; } = true;

    /// <summary>
    /// 是否为系统角色
    /// </summary>
    [SqlSugar.SugarColumn(ColumnName = "is_system_role", IsNullable = true)]
    [Id(5)]
    public bool is_system_role { get; set; } = false;

    /// <summary>
    /// 租户ID
    /// </summary>
    [SqlSugar.SugarColumn(ColumnName = "tenant_id", IsNullable = true)]
    [Id(6)]
    public long tenant_id { get; set; } = 0;

    /// <summary>
    /// 创建时间
    /// </summary>
    [SqlSugar.SugarColumn(ColumnName = "created_at", IsNullable = true)]
    [Id(7)]
    public DateTime created_at { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    [SqlSugar.SugarColumn(ColumnName = "updated_at", IsNullable = true)]
    [Id(8)]
    public DateTime updated_at { get; set; }

    /// <summary>
    /// 是否删除
    /// </summary>
    [SqlSugar.SugarColumn(ColumnName = "is_deleted")]
    [Id(9)]
    public bool is_deleted { get; set; } = false;

    /// <summary>
    /// 删除时间
    /// </summary>
    [SqlSugar.SugarColumn(ColumnName = "deleted_at",IsNullable =true)]
    [Id(10)]
    public DateTime? deleted_at { get; set; }
}