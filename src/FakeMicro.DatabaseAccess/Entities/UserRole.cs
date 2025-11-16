using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Orleans;
using Orleans.Serialization;
using Orleans.Serialization.Configuration;
using FakeMicro.Entities;
using SqlSugar;

namespace FakeMicro.DatabaseAccess.Entities;

/// <summary>
/// 用户角色关联实体
/// </summary>
[GenerateSerializer]
[SugarTable("user_roles")]
public class UserRole
{
    /// <summary>
    /// 关联ID (雪花ID)
    /// </summary>
    [SugarColumn(IsPrimaryKey = true,IsIdentity =true)]
    [Id(0)]
    public long Id { get; set; }
    
    /// <summary>
    /// 用户ID (雪花ID)
    /// </summary>
    [Required]
    [Id(1)]
    public long UserId { get; set; }
    
    /// <summary>
    /// 角色ID (雪花ID)
    /// </summary>
    [Required]
    [Id(2)]
    public long RoleId { get; set; }
    
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
    public string? TenantId { get; set; }
    
    /// <summary>
    /// 用户
    /// </summary>
    [Id(5)]
    public virtual User User { get; set; } = null!;
    
    /// <summary>
    /// 角色
    /// </summary>
    [Id(6)]
    public virtual Role Role { get; set; } = null!;
}