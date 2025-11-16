using System.ComponentModel.DataAnnotations;
using SqlSugar;
using Orleans.CodeGeneration;
using Orleans.Runtime;

namespace FakeMicro.DatabaseAccess.Entities;

/// <summary>
/// 租户实体
/// </summary>
[SugarTable("tenants")]
public class Tenant
{
    /// <summary>
    /// 租户ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true)]
    [MaxLength(50)]
    public string id { get; set; } = string.Empty;
    
    /// <summary>
    /// 租户名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 租户描述
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool is_enabled { get; set; } = true;
    
    /// <summary>
    /// 创建时间
    /// </summary>
    [Required]
    public DateTime created_at { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime updated_at { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 配置信息（JSON格式）
    /// </summary>
    [MaxLength(4000)]
    public string? Configuration { get; set; }
    
    /// <summary>
    /// 域名
    /// </summary>
    [MaxLength(200)]
    public string? Domain { get; set; }
}