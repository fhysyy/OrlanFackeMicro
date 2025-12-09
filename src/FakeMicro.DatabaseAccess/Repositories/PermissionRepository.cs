using SqlSugar;
using FakeMicro.DatabaseAccess.Entities;
using FakeMicro.DatabaseAccess.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace FakeMicro.DatabaseAccess.Repositories
{
    public class PermissionRepository : SqlSugarRepository<Permission, long>, IPermissionRepository
    {
        public PermissionRepository(ISqlSugarClient db, ILogger<PermissionRepository> logger)
            : base(db, logger)
        {
        }



        public async Task<List<Permission>> GetRolePermissionsAsync(long roleId)
        {
            return await GetRolePermissionsAsync(roleId, CancellationToken.None);
        }

        public async Task AddRolePermissionAsync(RolePermission rolePermission)
        {
            await AddRolePermissionAsync(rolePermission, CancellationToken.None);
        }

        public async Task RemoveRolePermissionAsync(RolePermission rolePermission)
        {
            await RemoveRolePermissionAsync(rolePermission, CancellationToken.None);
        }

        public async Task<RolePermission?> GetRolePermissionAsync(long roleId, long permissionId)
        {
            return await GetRolePermissionAsync(roleId, permissionId, CancellationToken.None);
        }

        public new async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // SqlSugar自动处理事务，直接返回
            await Task.CompletedTask;
        }

        public async Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await GetSqlSugarClient().Queryable<Permission>()
                    .Where(p => p.code == code)
                    .FirstAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"根据代码获取权限失败: {ex.Message}", ex);
            }
        }

        public async Task<List<Permission>> GetRolePermissionsAsync(long roleId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await GetSqlSugarClient().Queryable<Permission, RolePermission>((p, rp) => new JoinQueryInfos(
                    JoinType.Inner, p.id == rp.permission_id
                ))
                .Where((p, rp) => rp.role_id == roleId && p.is_enabled)
                .Select(p => p)
                .ToListAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"获取角色权限失败: {ex.Message}", ex);
            }
        }

        public async Task AddRolePermissionAsync(RolePermission rolePermission, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // 设置创建时间
                rolePermission.CreatedAt = DateTime.UtcNow;
                await GetSqlSugarClient().Insertable(rolePermission).ExecuteCommandAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"添加角色权限关系失败: {ex.Message}", ex);
            }
        }

        public async Task RemoveRolePermissionAsync(RolePermission rolePermission, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await GetSqlSugarClient().Deleteable<RolePermission>()
                    .Where(rp => rp.role_id == rolePermission.role_id && rp.permission_id == rolePermission.permission_id)
                    .ExecuteCommandAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"移除角色权限关系失败: {ex.Message}", ex);
            }
        }

        public async Task<RolePermission?> GetRolePermissionAsync(long roleId, long permissionId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await GetSqlSugarClient().Queryable<RolePermission>()
                    .Where(rp => rp.role_id == roleId && rp.permission_id == permissionId)
                    .FirstAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"获取角色权限关系失败: {ex.Message}", ex);
            }
        }

        public async Task<bool> CodeExistsAsync(string code, long excludeId = 0, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var query = GetSqlSugarClient().Queryable<Permission>()
                    .Where(p => p.code == code);
                
                if (excludeId > 0)
                {
                    query = query.Where(p => p.id != excludeId);
                }
                
                var count = await query.CountAsync();
                return count > 0;
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"检查权限代码是否存在失败: {ex.Message}", ex);
            }
        }

        public new async Task<IEnumerable<Permission>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await GetSqlSugarClient().Queryable<Permission>().ToListAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"获取所有权限失败: {ex.Message}", ex);
            }
        }
    }
}