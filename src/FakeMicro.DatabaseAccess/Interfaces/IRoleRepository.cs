using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Entities;
using System.Linq.Expressions;
using System.Threading;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// 角色数据访问接口
/// </summary>
public interface IRoleRepository : IRepository<Role, long>
{
    /// <summary>
    /// 根据代码获取角色
    /// </summary>
    Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取用户角色
    /// </summary>
    Task<List<Role>> GetUserRolesAsync(long userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 添加用户角色关联
    /// </summary>
    Task AddUserRoleAsync(UserRole userRole, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 删除用户角色关联
    /// </summary>
    Task RemoveUserRoleAsync(UserRole userRole, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取用户角色关联
    /// </summary>
    Task<UserRole?> GetUserRoleAsync(long userId, long roleId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 检查角色代码是否存在
    /// </summary>
    Task<bool> CodeExistsAsync(string code, long excludeId = 0, CancellationToken cancellationToken = default);


}