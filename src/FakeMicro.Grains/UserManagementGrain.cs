using FakeMicro.Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using FakeMicro.DatabaseAccess.Interfaces;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace FakeMicro.Grains
{
    /// <summary>
    /// 用户管理Grain - 负责用户权限和状态管理
    /// </summary>
    [StatelessWorker(10)]
    [Reentrant]
    public class UserManagementGrain : OrleansGrainBase, IUserManagementGrain
    {
        private readonly ILogger<UserManagementGrain> _logger;
        private readonly IUserRepository _userRepository;

        public UserManagementGrain(
            ILogger<UserManagementGrain> logger, 
            IUserRepository userRepository) : base(logger)
        {
            _logger = logger;
            _userRepository = userRepository;
        }

        public async Task<bool> LogoutAsync(long userId, CancellationToken cancellationToken = default)
        {
            try
            {
                // 更新数据库中的用户状态
                await _userRepository.UpdateLoginInfoAsync(userId, false, DateTime.UtcNow, 
                    loginIp: null, cancellationToken: cancellationToken);
                
                _logger.LogInformation("用户登出成功: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户登出失败: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ValidateUserPermissionAsync(long userId, string resource, string permissionType, CancellationToken cancellationToken = default)
        {
            try
            {
                // 从数据库获取用户信息
                var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
                if (user == null || !user.is_active)
                {
                    return false;
                }
                
                // 简单的权限验证逻辑（根据角色判断基本权限）
                // 这里可以根据具体需求扩展为更复杂的权限系统
                switch (user.role)
                {
                    case "Admin":
                        // 管理员拥有所有权限
                        return true;
                    case "User":
                        // 普通用户权限检查
                        return resource.StartsWith("user") || resource.StartsWith("profile");
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证用户权限失败: {UserId}, {Resource}, {PermissionType}", userId, resource, permissionType);
                return false;
            }
        }
    }
}
