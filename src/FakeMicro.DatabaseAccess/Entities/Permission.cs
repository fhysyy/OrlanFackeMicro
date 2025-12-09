using System.ComponentModel.DataAnnotations;
using SqlSugar;
using Orleans;
using Orleans.CodeGeneration;

namespace FakeMicro.DatabaseAccess.Entities;

/// <summary>
/// 权限实体
/// </summary>
[SugarTable("permissions")]
public class Permission
{
    /// <summary>
    /// 权限ID (雪花ID)
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    [Id(0)]
    public long id { get; set; }
    
    /// <summary>
    /// 权限名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Id(1)]
    public string name { get; set; } = string.Empty;
    
    /// <summary>
    /// 权限代码
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Id(2)]
    public string code { get; set; } = string.Empty;
    
    /// <summary>
    /// 资源类型
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Id(3)]
    public string resource { get; set; } = string.Empty;
    
    /// <summary>
    /// 权限类型
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Id(4)]
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// 权限描述
    /// </summary>
    [MaxLength(500)]
    [Id(5)]
    public string? Description { get; set; }
    
    /// <summary>
    /// 是否启用
    /// </summary>
    [Id(6)]
    public bool is_enabled { get; set; } = true;
    
    /// <summary>
    /// 创建时间
    /// </summary>
    [Required]
    [Id(7)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 更新时间
    /// </summary>
    [Id(8)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 租户ID（多租户支持）
    /// </summary>
    [MaxLength(50)]
    [Id(9)]
    public string? tenant_id { get; set; }
}