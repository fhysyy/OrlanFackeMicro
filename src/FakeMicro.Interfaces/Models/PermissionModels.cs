using Orleans;

namespace FakeMicro.Interfaces.Models;

/// <summary>
/// 权限DTO
/// </summary>
[GenerateSerializer]
public class PermissionDto
{
    [Id(0)]
    public long Id { get; set; }
    
    [Id(1)]
    public string Name { get; set; } = string.Empty;
    
    [Id(2)]
    public string Code { get; set; } = string.Empty;
    
    [Id(3)]
    public string Resource { get; set; } = string.Empty;
    
    [Id(4)]
    public string Type { get; set; } = string.Empty;
    
    [Id(5)]
    public string? Description { get; set; }
    
    [Id(6)]
    public bool IsEnabled { get; set; } = true;
    
    [Id(7)]
    public DateTime CreatedAt { get; set; }
    
    [Id(8)]
    public string? TenantId { get; set; }
}

/// <summary>
/// 角色DTO
/// </summary>
[GenerateSerializer]
public class RoleDto
{
    [Id(0)]
    public long Id { get; set; }
    
    [Id(1)]
    public string Name { get; set; } = string.Empty;
    
    [Id(2)]
    public string Code { get; set; } = string.Empty;
    
    [Id(3)]
    public string? Description { get; set; }
    
    [Id(4)]
    public bool IsSystemRole { get; set; } = false;
    
    [Id(5)]
    public bool IsEnabled { get; set; } = true;
    
    [Id(6)]
    public DateTime CreatedAt { get; set; }
    
    [Id(7)]
    public string? TenantId { get; set; }
    
    [Id(8)]
    public List<PermissionDto> Permissions { get; set; }
}

/// <summary>
/// 审计日志DTO
/// </summary>
[GenerateSerializer]
public class AuditLogDto
{
    [Id(0)]
    public int Id { get; set; }
    
    [Id(1)]
    public long? UserId { get; set; }
    
    [Id(2)]
    public string? Username { get; set; }
    
    [Id(3)]
    public string Action { get; set; } = string.Empty;
    
    [Id(4)]
    public string Resource { get; set; } = string.Empty;
    
    [Id(5)]
    public string? ResourceId { get; set; }
    
    [Id(6)]
    public string? Details { get; set; }
    
    [Id(7)]
    public string? IpAddress { get; set; }
    
    [Id(8)]
    public string? UserAgent { get; set; }
    
    [Id(9)]
    public DateTime CreatedAt { get; set; }
    
    [Id(10)]
    public string? TenantId { get; set; }
    
    [Id(11)]
    public string? Result { get; set; }
    
    [Id(12)]
    public string? ErrorMessage { get; set; }
    
    [Id(13)]
    public long? ExecutionTime { get; set; }
}

/// <summary>
/// 租户DTO
/// </summary>
[GenerateSerializer]
public class TenantDto
{
    [Id(0)]
    public string Id { get; set; } = string.Empty;
    
    [Id(1)]
    public string Name { get; set; } = string.Empty;
    
    [Id(2)]
    public string? Description { get; set; }
    
    [Id(3)]
    public bool IsEnabled { get; set; } = true;
    
    [Id(4)]
    public DateTime CreatedAt { get; set; }
    
    [Id(5)]
    public DateTime UpdatedAt { get; set; }
    
    [Id(6)]
    public string? Configuration { get; set; }
    
    [Id(7)]
    public string? Domain { get; set; }
}

/// <summary>
/// 权限分配请求
/// </summary>
[GenerateSerializer]
public class AssignPermissionRequest
{
    [Id(0)]
    public long RoleId { get; set; }
    
    [Id(1)]
    public long PermissionId { get; set; }
}

/// <summary>
/// 角色分配请求
/// </summary>
[GenerateSerializer]
public class AssignRoleRequest
{
    [Id(0)]
    public long UserId { get; set; }
    
    [Id(1)]
    public long RoleId { get; set; }
}

/// <summary>
/// 权限检查请求
/// </summary>
[GenerateSerializer]
public class CheckPermissionRequest
{
    [Id(0)]
    public long UserId { get; set; }
    
    [Id(1)]
    public string Resource { get; set; } = string.Empty;
    
    [Id(2)]
    public string PermissionType { get; set; } = string.Empty;
}