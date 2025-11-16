using System.ComponentModel.DataAnnotations;
using SqlSugar;
using Orleans;
using Orleans.CodeGeneration;

namespace FakeMicro.Entities;

/// <summary>
/// 审计日志实体
/// </summary>
[SugarTable("audit_logs")]
public class AuditLog
{
    /// <summary>
    /// 日志ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    [Id(0)]
    public int id { get; set; }
    
    /// <summary>
    /// 用户ID
    /// </summary>
    [Id(1)]
    public long? user_id { get; set; }
    
    /// <summary>
    /// 用户名
    /// </summary>
    [MaxLength(100)]
    [Id(2)]
    public string? username { get; set; }
    
    /// <summary>
    /// 操作类型
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Id(3)]
    public string action { get; set; } = string.Empty;
    
    /// <summary>
    /// 资源类型
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Id(4)]
    public string resource { get; set; } = string.Empty;
    
    /// <summary>
    /// 资源ID
    /// </summary>
    [MaxLength(100)]
    [Id(5)]
    public string? resource_id { get; set; }
    
    /// <summary>
    /// 操作详情
    /// </summary>
    [MaxLength(2000)]
    [Id(6)]
    public string? Details { get; set; }
    
    /// <summary>
    /// IP地址
    /// </summary>
    [MaxLength(45)]
    [Id(7)]
    public string? ip_address { get; set; }
    
    /// <summary>
    /// 用户代理
    /// </summary>
    [MaxLength(500)]
    [Id(8)]
    public string? user_agent { get; set; }
    
    /// <summary>
    /// 操作时间
    /// </summary>
    [Required]
    [Id(9)]
    public DateTime created_at { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 租户ID（多租户支持）
    /// </summary>
    [MaxLength(50)]
    [Id(10)]
    public string? tenant_id { get; set; }
    
    /// <summary>
    /// 操作结果
    /// </summary>
    [MaxLength(20)]
    [Id(11)]
    public string? Result { get; set; }
    
    /// <summary>
    /// 错误信息
    /// </summary>
    [MaxLength(1000)]
    [Id(12)]
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// 执行时间（毫秒）
    /// </summary>
    [Id(13)]
    public long? execution_time { get; set; }
}