using FakeMicro.Entities;
using SqlSugar;
using System.Threading;
using Microsoft.Extensions.Logging;
using FakeMicro.DatabaseAccess.Interfaces;

namespace FakeMicro.DatabaseAccess.Repositories
{
    public class RoleRepository : SqlSugarRepository<Role, long>, IRoleRepository
    {
        public RoleRepository(ISqlSugarClient db, ILogger<RoleRepository> logger)
            : base(db, logger)
        {
        }

        public async Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await GetSqlSugarClient().Queryable<Role>()
                    .Where("code = @code", new { code })
                    .FirstAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"根据代码获取角色失败: {ex.Message}", ex);
            }
        }

        public async Task<List<Role>> GetUserRolesAsync(long userId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await GetSqlSugarClient().Queryable<UserRole, Role>((ur, r) => new JoinQueryInfos(
                    JoinType.Left, true
                ))
                .Where("ur.role_id = r.id AND ur.user_id = @userId", new { userId })
                .Select<Role>()
                .ToListAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"获取用户角色失败: {ex.Message}", ex);
            }
        }

        public async Task AddUserRoleAsync(UserRole userRole, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // 设置创建时间
                userRole.created_at = DateTime.UtcNow;
                userRole.updated_at = DateTime.UtcNow;
                await GetSqlSugarClient().Insertable(userRole).ExecuteCommandAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"添加用户角色关系失败: {ex.Message}", ex);
            }
        }

        public async Task RemoveUserRoleAsync(UserRole userRole, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await GetSqlSugarClient().Deleteable<UserRole>()
                    .Where(ur => ur.user_id == userRole.user_id && ur.role_id == userRole.role_id)
                    .ExecuteCommandAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"移除用户角色关系失败: {ex.Message}", ex);
            }
        }

        public async Task<UserRole?> GetUserRoleAsync(long userId, long roleId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await GetSqlSugarClient().Queryable<UserRole>()
                    .Where(ur => ur.user_id == userId && ur.role_id == roleId)
                    .FirstAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"获取用户角色关系失败: {ex.Message}", ex);
            }
        }

        public async Task<bool> CodeExistsAsync(string code, long excludeId = 0, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var query = GetSqlSugarClient().Queryable<Role>()
                    .Where("code = @code", new { code });
                
                if (excludeId > 0)
                {
                    query = query.Where("id != @excludeId", new { excludeId });
                }
                
                var count = await query.CountAsync();
                return count > 0;
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"检查角色代码是否存在失败: {ex.Message}", ex);
            }
        }



    }
}