using FakeMicro.DatabaseAccess.Entities;
using FakeMicro.DatabaseAccess.Interfaces;
using System.Linq.Expressions;
using System.Threading;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// 权限数据访问接口
/// </summary>
public interface IPermissionRepository : IRepository<Permission, long>
{
    /// <summary>
    /// 根据代码获取权限
    /// </summary>
    Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取角色权限
    /// </summary>
    Task<List<Permission>> GetRolePermissionsAsync(long roleId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 添加角色权限关联
    /// </summary>
    Task AddRolePermissionAsync(RolePermission rolePermission, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 删除角色权限关联
    /// </summary>
    Task RemoveRolePermissionAsync(RolePermission rolePermission, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取角色权限关联
    /// </summary>
    Task<RolePermission?> GetRolePermissionAsync(long roleId, long permissionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 检查权限代码是否存在
    /// </summary>
    Task<bool> CodeExistsAsync(string code, long excludeId = 0, CancellationToken cancellationToken = default);

    ///// <summary>
    ///// 获取所有权限
    ///// </summary>
    //Task<List<Permission>> GetAllAsync(CancellationToken cancellationToken = default);

    ///// <summary>
    ///// 保存更改
    ///// </summary>
    //Task SaveChangesAsync(CancellationToken cancellationToken = default);
}