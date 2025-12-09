using System.ComponentModel.DataAnnotations;
using SqlSugar;

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
    public long id { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(IsNullable =true)]
    public long? user_id { get; set; }
    
    /// <summary>
    /// 用户名
    /// </summary>
   
    [SugarColumn(IsNullable = true,Length =100)]
    public string? username { get; set; }
    
    /// <summary>
    /// 操作类型
    /// </summary>


    [SugarColumn(IsNullable = true,Length =200)]
    public string action { get; set; } = string.Empty;
    
    /// <summary>
    /// 资源类型
    /// </summary>

  
    [SugarColumn(IsNullable = true,Length =200)]
    public string resource { get; set; } = string.Empty;
    
    /// <summary>
    /// 资源ID
    /// </summary>
 
    [SugarColumn(IsNullable = true, Length = 200)]
    public string? resource_id { get; set; } = null;
    
    /// <summary>
    /// 操作详情
    /// </summary>
    [SugarColumn(ColumnName = "details",IsNullable =true,Length =4000)]
  
    public string? Details { get; set; }
    
    /// <summary>
    /// IP地址
    /// </summary>

    [SugarColumn(IsNullable = true, Length = 200)]
    public string? ip_address { get; set; }
    
    /// <summary>
    /// 用户代理
    /// </summary>

    [SugarColumn(IsNullable = true, Length = 200)]
    public string? user_agent { get; set; }

    /// <summary>
    /// 操作时间
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 租户ID（多租户支持）
    /// </summary>

    [SugarColumn(IsNullable = true, Length = 200)]
    public string? tenant_id { get; set; }
    
    /// <summary>
    /// 操作结果
    /// </summary>
    [SugarColumn(ColumnName = "result",IsNullable =true,Length =4000)]

    public string? Result { get; set; }
    
    /// <summary>
    /// 错误信息
    /// </summary>
    [SugarColumn(ColumnName = "error_message", IsNullable = true,Length =4000)]

    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// 执行时间（毫秒）
    /// </summary>
    [SugarColumn(ColumnName = "execution_time", IsNullable = true)]
    public long? execution_time { get; set; }
}