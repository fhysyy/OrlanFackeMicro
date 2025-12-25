using FakeMicro.Entities;
using System.Linq.Expressions;
using System.Threading;
using FakeMicro.Interfaces.Models;

namespace FakeMicro.DatabaseAccess.Interfaces;

/// <summary>
/// 用户数据访问接口
/// </summary>
public interface IUserRepository : IRepository<User, long>
{
    /// <summary>
    /// 根据刷新令牌获取用户
    /// </summary>
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取所有用户（支持多租户过滤）
    /// </summary>
    Task<List<User>> GetAllAsync(int? tenantId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 搜索用户（按用户名、邮箱和状态）
    /// </summary>
    Task<List<User>> SearchUsersAsync(string? username = null, string? email = null, string? status = null, int? tenantId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取分页用户列表
    /// </summary>
    Task<PagedResult<User>> GetPagedAsync(int pageNumber, int pageSize, int? tenantId = null, 
        Expression<Func<User, object>>? orderBy = null, bool isDescending = false,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    Task<User?> GetByUsernameAsync(string username, int? tenantId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    Task<User?> GetByEmailAsync(string email, int? tenantId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 根据租户ID获取用户数量
    /// </summary>
    Task<int> GetCountByTenantAsync(int tenantId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 根据条件查询用户
    /// </summary>
    Task<List<User>> FindAsync(Func<IQueryable<User>, IQueryable<User>> query, int? tenantId = null, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取活跃用户
    /// </summary>
    Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 获取最近注册的用户
    /// </summary>
    Task<IEnumerable<User>> GetRecentlyRegisteredUsersAsync(int days = 7, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 检查用户名是否存在
    /// </summary>
    Task<bool> UsernameExistsAsync(string username, int? tenantId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 检查邮箱是否存在
    /// </summary>
    Task<bool> EmailExistsAsync(string email, int? tenantId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 验证刷新令牌是否有效
    /// </summary>
    Task<bool> ValidateRefreshTokenAsync(long userId, string refreshToken, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 更新用户的刷新令牌
    /// </summary>
    Task<string> UpdateRefreshTokenAsync(long userId, string newRefreshToken, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 更新用户登录信息
    /// </summary>
    Task UpdateLoginInfoAsync(long userId, bool loginSuccess, DateTime? loginTime = null, string? loginIp = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 生成新的刷新令牌
    /// </summary>
    Task<string> GenerateAndSaveRefreshTokenAsync(long userId, CancellationToken cancellationToken = default);
}