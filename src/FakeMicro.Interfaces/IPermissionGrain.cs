 using FakeMicro.Interfaces.Models;
using Orleans;

namespace FakeMicro.Interfaces;

/// <summary>
/// 权限管理Grain接口
/// </summary>
public interface IPermissionGrain : IGrainWithStringKey
{
    /// <summary>
    /// 检查用户权限
    /// </summary>
    Task<bool> HasPermissionAsync(long userId, string resource, string permissionType);
    
    /// <summary>
    /// 获取用户所有权限
    /// </summary>
    Task<List<PermissionDto>> GetUserPermissionsAsync(long userId);
    
    /// <summary>
    /// 获取角色权限
    /// </summary>
    Task<List<PermissionDto>> GetRolePermissionsAsync(long roleId);
    
    /// <summary>
    /// 为用户分配角色
    /// </summary>
    Task<bool> AssignRoleToUserAsync(long userId, long roleId);
    
    /// <summary>
    /// 为用户移除角色
    /// </summary>
    Task<bool> RemoveRoleFromUserAsync(long userId, long roleId);
    
    /// <summary>
    /// 为角色分配权限
    /// </summary>
    Task<bool> AssignPermissionToRoleAsync(long roleId, long permissionId);
    
    /// <summary>
    /// 为角色移除权限
    /// </summary>
    Task<bool> RemovePermissionFromRoleAsync(long roleId, long permissionId);
    
    /// <summary>
    /// 创建权限
    /// </summary>
    Task<PermissionDto> CreatePermissionAsync(PermissionDto permission);
    
    /// <summary>
    /// 创建角色
    /// </summary>
    Task<RoleDto> CreateRoleAsync(RoleDto role);
    
    /// <summary>
    /// 获取所有权限
    /// </summary>
    Task<List<PermissionDto>> GetAllPermissionsAsync();
    
    /// <summary>
    /// 获取所有角色
    /// </summary>
    Task<List<RoleDto>> GetAllRolesAsync();
    
    /// <summary>
    /// 获取用户角色
    /// </summary>
    Task<List<RoleDto>> GetUserRolesAsync(long userId);
    
    /// <summary>
    /// 记录审计日志
    /// </summary>
    Task LogAuditAsync(AuditLogDto auditLog);
    
    /// <summary>
    /// 获取审计日志
    /// </summary>
    Task<List<AuditLogDto>> GetAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null, long? userId = null);
}