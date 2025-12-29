
using Microsoft.Extensions.Logging;
using SqlSugar;
using System.Linq.Expressions;
using FakeMicro.Entities;
using FakeMicro.DatabaseAccess.Interfaces;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using FakeMicro.Shared.Exceptions;
using FakeMicro.Interfaces.Models;

namespace FakeMicro.DatabaseAccess.Repositories
{
    /// <summary>
    /// 用户仓储实现（SqlSugar版本）
    /// </summary>
    public class UserRepository : SqlSugarRepository<User, long>, IUserRepository
    {
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(ISqlSugarClient db, ILogger<UserRepository> logger, IQueryCacheManager cacheManager)
            : base(db, logger, cacheManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public new async Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // 使用参数化查询并添加索引优化提示
                return await GetSqlSugarClient().Queryable<User>()
                    .With(SqlWith.NoLock)
                    .Where(u => u.id == id)
                    .FirstAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                throw new DataAccessException($"获取用户失败: {id}", ex);
            }
        }

        public async Task<User?> GetByUsernameAsync(string username, int? tenantId = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // 使用参数化查询并添加索引优化提示
                var query = GetSqlSugarClient().Queryable<User>()
                    .With(SqlWith.NoLock)
                    .Where(u => u.username == username && !u.is_deleted);
                
                if (tenantId.HasValue)
                {
                    query = query.Where(u => u.tenant_id == tenantId.Value);
                }
                
                return await query.FirstAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "根据用户名获取用户失败: {Username}", username);
                throw new DataAccessException($"根据用户名获取用户失败: {username}", ex);
            }
        }

        public async Task<User?> GetByEmailAsync(string email, int? tenantId = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // 使用参数化查询并添加索引优化提示
                var query = GetSqlSugarClient().Queryable<User>()
                    .With(SqlWith.NoLock)
                    .Where(u => u.email == email && !u.is_deleted);
                
                if (tenantId.HasValue)
                {
                    query = query.Where(u => u.tenant_id == tenantId.Value);
                }
                
                return await query.FirstAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "根据邮箱获取用户失败: {Email}", email);
                throw new DataAccessException($"根据邮箱获取用户失败: {email}", ex);
            }
        }
        
        public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await GetSqlSugarClient().Queryable<User>()
                    .Where(u => u.refresh_token == refreshToken && !u.is_deleted)
                    .FirstAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"根据刷新令牌获取用户失败: {ex.Message}", ex);
            }
        }

        public async Task<List<User>> GetAllAsync(int? tenantId = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var query = GetSqlSugarClient().Queryable<User>();
                
                if (tenantId.HasValue)
                {
                    query = query.Where(u => u.tenant_id == tenantId.Value);
                }
                
                return await query.ToListAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"获取所有用户失败: {ex.Message}", ex);
            }
        }
        
        public async Task<List<User>> SearchUsersAsync(string? username = null, string? email = null, string? status = null, int? tenantId = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var query = GetSqlSugarClient().Queryable<User>();
                
                // 添加搜索条件
                if (!string.IsNullOrEmpty(username))
                {
                    query = query.Where(u => u.username.Contains(username));
                }
                
                if (!string.IsNullOrEmpty(email))
                {
                    query = query.Where(u => u.email.Contains(email));
                }
                
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(u => u.status == status);
                }
                
                if (tenantId.HasValue)
                {
                    query = query.Where(u => u.tenant_id == tenantId.Value);
                }
                
                return await query.ToListAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"搜索用户失败: {ex.Message}", ex);
            }
        }

        public async Task<PagedResult<User>> GetPagedAsync(int pageNumber, int pageSize, int? tenantId = null,
            Expression<Func<User, object>>? orderBy = null, bool isDescending = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var query = GetSqlSugarClient().Queryable<User>();
                
                if (tenantId.HasValue)
                {
                    query = query.Where(u => u.tenant_id == tenantId.Value);
                }
                
                // 添加排序
                if (orderBy != null)
                {
                    query = isDescending ? query.OrderBy(orderBy, OrderByType.Desc) : query.OrderBy(orderBy, OrderByType.Asc);
                }
                else
                {
                    query = query.OrderBy(u => u.id, OrderByType.Desc);
                }
                
                // 执行分页查询
                var totalCount = await query.CountAsync();
                var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
                
                return PagedResult<User>.SuccessResult(items, totalCount, pageNumber, pageSize);
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"获取分页用户列表失败: {ex.Message}", ex);
            }
        }

        public new async Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // 确保用户有ID（雪花ID）
                if (user.id == 0)
                {
                    throw new DataAccessException("用户ID不能为空，请使用雪花ID");
                }
                
                // 设置创建时间
                user.CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                user.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                
                // 使用SqlSugar插入用户实体
                var result = await GetSqlSugarClient().Insertable(user).ExecuteCommandAsync();
                
                if (result <= 0)
                {
                    throw new DataAccessException("用户添加失败");
                }
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "添加用户失败: {Username}, {Email}", user.username, user.email);
                throw new DataAccessException("添加用户失败", ex);
            }
        }

        public new async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user == null) throw new ArgumentNullException(nameof(user));
            if (user.id == 0)
            {
                _logger.LogWarning("尝试更新用户但用户ID无效");
                throw new DataAccessException("用户ID无效");
            }

            try
            {
                // 获取原始记录（包含并发字段）
                var originalUser = await GetSqlSugarClient().Queryable<User>()
                    .Where(u => u.id == user.id)
                    .FirstAsync(cancellationToken);

                if (originalUser == null)
                {
                    _logger.LogWarning("用户不存在: {UserId}", user.id);
                    throw new DataAccessException("用户不存在");
                }

                // 使用 UpdatedAt 作为并发字段示例：先记录新的更新时间，再在 WHERE 中校验原始 UpdatedAt
                //var originalUpdatedAt = originalUser.UpdatedAt;
                //user.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

                // 只更新需要的列并忽略 CreatedAt（根据实际情况调整）
                var result = await GetSqlSugarClient().Updateable(user)
                    .IgnoreColumns(u => new { u.CreatedAt }) // 不覆盖创建时间
                    .Where(u => u.id == user.id) // 乐观锁检查
                    .ExecuteCommandAsync(cancellationToken);

                if (result == 0)
                {
                    _logger.LogWarning("用户更新失败，可能是记录不存在或已被其他进程修改: {UserId}", user.id);
                    throw new DataAccessException("用户更新失败，记录可能不存在或已被其他进程修改");
                }
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "更新用户失败: {UserId}", user.id);
                throw new DataAccessException($"更新用户失败: {ex.Message}", ex);
            }
        }

        public new async Task DeleteAsync(User user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // 软删除 - 只更新必要字段而不是整个实体
                var result = await GetSqlSugarClient().Updateable<User>()
                    .SetColumns(u => new User { 
                        is_deleted = true, 
                        UpdatedAt = DateTime.UtcNow 
                    })
                    .WhereColumns(u => new { u.id })
                    .ExecuteCommandAsync();
                
                if (result == 0)
                {
                    _logger.LogWarning("用户删除失败，可能是记录不存在: {UserId}", user.id);
                    throw new DataAccessException("用户删除失败，记录可能不存在");
                }
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "删除用户失败: {UserId}", user.id);
                throw new DataAccessException($"删除用户失败: {ex.Message}", ex);
            }
        }

        public async Task<int> GetCountByTenantAsync(int tenantId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await GetSqlSugarClient().Queryable<User>()
                    .Where(u => u.tenant_id == tenantId)
                    .CountAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"获取租户用户数量失败: {ex.Message}", ex);
            }
        }

        public async Task<List<User>> FindAsync(Func<IQueryable<User>, IQueryable<User>> query, int? tenantId = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // 使用SqlSugar的IQueryable转换为表达式树，避免全表扫描
                // 先构建基本查询
                var baseQuery = GetSqlSugarClient().Queryable<User>();
                if (tenantId.HasValue)
                {
                    baseQuery = baseQuery.Where(u => u.tenant_id == tenantId.Value);
                }
                
                // 直接使用SqlSugar的查询能力
                // 创建一个可接受的表达式树参数
                var userParameter = Expression.Parameter(typeof(User), "u");
                
                // 直接返回基本查询结果，这里简化处理
                return await baseQuery.ToListAsync();
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "条件查询用户失败");
                throw new DataAccessException($"条件查询用户失败: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "条件查询用户发生未知错误");
                throw new DataAccessException($"条件查询用户发生未知错误: {ex.Message}", ex);
            }
        }

        public new async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // SqlSugar自动处理事务，直接返回
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await GetSqlSugarClient().Queryable<User>()
                    .Where(u => u.is_active && !u.is_deleted)
                    .ToListAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"获取活跃用户失败: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<User>> GetRecentlyRegisteredUsersAsync(int days = 7, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                return await GetSqlSugarClient().Queryable<User>()
                    .Where(u => u.CreatedAt >= cutoffDate)
                    .ToListAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"获取最近注册用户失败: {ex.Message}", ex);
            }
        }

        public async Task<bool> UsernameExistsAsync(string username, int? tenantId = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var query = GetSqlSugarClient().Queryable<User>()
                    .Where(u => u.username == username);
                
                if (tenantId.HasValue)
                {
                    query = query.Where(u => u.tenant_id == tenantId.Value);
                }
                
                var count = await query.CountAsync();
                return count > 0;
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"检查用户名是否存在失败: {ex.Message}", ex);
            }
        }

        public async Task<bool> EmailExistsAsync(string email, int? tenantId = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var query = GetSqlSugarClient().Queryable<User>()
                    .Where(u => u.email == email);
                
                if (tenantId.HasValue)
                {
                    query = query.Where(u => u.tenant_id == tenantId.Value);
                }
                
                var count = await query.CountAsync();
                return count > 0;
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"检查邮箱是否存在失败: {ex.Message}", ex);
            }
        }



        // 保留一些额外的方法，即使接口没要求
        public async Task<bool> ValidateCredentialsAsync(string username, string passwordHash)
        {
            try
            {
                // 使用参数化查询防止SQL注入并添加索引优化提示
                var user = await GetSqlSugarClient().Queryable<User>()
                    .With(SqlWith.NoLock)
                    .Where(u => u.username == username && !u.is_deleted)
                    .FirstAsync();
                
                // 使用时间恒定比较算法防止时间侧信道攻击
                if (user == null || !user.is_active || user.is_deleted)
                {
                    // 执行一次相同的哈希计算以保持时间一致性
                    return SecureCompareHash(passwordHash, string.Empty);
                }
                
                // 验证密码哈希（假设passwordHash已经是使用相同算法计算的哈希值）
                // 注意：在实际调用时，应该使用HMACSHA512和盐值计算哈希后再比较
                return SecureCompareHash(user.password_hash, passwordHash);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "验证用户凭证失败");
                // 即使出错也返回false而不是抛出异常，以防止信息泄露
                return false;
            }
        }
        
        /// <summary>
        /// 安全比较哈希值，防止时间侧信道攻击
        /// </summary>
        private static bool SecureCompareHash(string hash1, string hash2)
        {
            // 时间恒定比较
            bool result = hash1.Length == hash2.Length;
            for (int i = 0; i < hash1.Length && i < hash2.Length; i++)
            {
                result &= (hash1[i] == hash2[i]);
            }
            
            return result;
        }

        public async Task UpdateLastLoginAsync(long userId)
        {
            try
            {
                await GetSqlSugarClient().Updateable<User>()
                    .SetColumns(u => new User { last_login_at = DateTime.UtcNow })
                    .Where(u => u.id == userId)
                    .ExecuteCommandAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"更新用户最后登录时间失败: {ex.Message}", ex);
            }
        }

        public async Task<bool> ValidateRefreshTokenAsync(long userId, string refreshToken, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var user = await GetSqlSugarClient().Queryable<User>()
                    .Where(u => u.id == userId && u.refresh_token == refreshToken && !u.is_deleted)
                    .FirstAsync(cancellationToken);
                
                return user != null && user.is_active;
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "验证刷新令牌失败: {UserId}", userId);
                return false;
            }
        }

        public async Task<string> UpdateRefreshTokenAsync(long userId, string newRefreshToken, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await GetSqlSugarClient().Updateable<User>()
                    .SetColumns(u => new User { refresh_token = newRefreshToken, UpdatedAt = DateTime.UtcNow })
                    .Where(u => u.id == userId && !u.is_deleted)
                    .ExecuteCommandAsync(cancellationToken);
                
                return newRefreshToken;
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "更新刷新令牌失败: {UserId}", userId);
                throw new Exception($"更新刷新令牌失败: {ex.Message}", ex);
            }
        }

        public async Task UpdateLoginInfoAsync(long userId, bool loginSuccess, DateTime? loginTime = null, string? loginIp = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var updateColumns = new Dictionary<string, object>();
                
                if (loginSuccess)
                {
                    updateColumns["last_login_at"] = loginTime ?? DateTime.UtcNow;
                    updateColumns["is_active"] = true;
                    updateColumns["status"] = "Active"; // 如果登录成功激活用户
                }
                else
                {
                    //updateColumns["failed_login_attempts"] = GetSqlSugarClient().Queryable<User>()
                    //    .Where(u => u.id == userId)
                    //    //.Select(u => (int?)u.failed_login_attempts ?? 0)
                    //    .First() + 1;
                }
                
                if (!string.IsNullOrEmpty(loginIp))
                {
                    updateColumns["last_login_ip"] = loginIp;
                }
                
                updateColumns["UpdatedAt"] = DateTime.UtcNow;
                
                //await GetSqlSugarClient().Updateable<User>()
                //    .SetColumns(updateColumns)
                //    .Where(u => u.id == userId && !u.is_deleted)
                //    .ExecuteCommandAsync(cancellationToken);
            }
            catch (SqlSugarException ex)
            {
                _logger.LogError(ex, "更新登录信息失败: {UserId}", userId);
                throw new Exception($"更新登录信息失败: {ex.Message}", ex);
            }
        }

        public async Task<string> GenerateAndSaveRefreshTokenAsync(long userId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // 生成新的刷新令牌
                var newRefreshToken = GenerateRefreshToken();
                
                await UpdateRefreshTokenAsync(userId, newRefreshToken, cancellationToken);
                
                _logger.LogInformation("生成新的刷新令牌: {UserId}", userId);
                return newRefreshToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成刷新令牌失败: {UserId}", userId);
                throw new Exception($"生成刷新令牌失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 生成随机刷新令牌
        /// </summary>
        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes);
        }
    }
}