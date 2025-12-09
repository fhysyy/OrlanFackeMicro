using System.ComponentModel.DataAnnotations;
using SqlSugar;
using Orleans;
using Orleans.Serialization;
using Orleans.Serialization.Configuration;
using FakeMicro.Entities;

namespace FakeMicro.DatabaseAccess.Entities;

/// <summary>
/// 角色权限关联实体
/// </summary>
[GenerateSerializer]
[Immutable]
[SugarTable("role_permissions")]
public class RolePermission
{
    /// <summary>
    /// 关联ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true,IsIdentity =true)]
    [Id(0)]
    public long id { get; set; }
    
    /// <summary>
    /// 角色ID
    /// </summary>
    [Required]
    [Id(1)]
    public long role_id { get; set; }
    
    /// <summary>
    /// 权限ID
    /// </summary>
    [Required]
    [Id(2)]
    public long permission_id { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    [Required]
    [Id(3)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 租户ID（多租户支持）
    /// </summary>
    [MaxLength(50)]
    [Id(4)]
    public string? tenant_id { get; set; }
    
    /// <summary>
    /// 角色
    /// </summary>
    [Id(5)]
    public virtual Role Role { get; set; } = null!;
    
    /// <summary>
    /// 权限
    /// </summary>
    [Id(6)]
    public virtual Permission Permission { get; set; } = null!;
}