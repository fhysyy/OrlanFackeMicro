using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using FakeMicro.DatabaseAccess.Interfaces;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;
using System.Collections.Generic;

// 使用别名解决命名空间冲突
using UserRoleEnum = FakeMicro.Entities.Enums.UserRole;
using UserStatusEnum = FakeMicro.Entities.Enums.UserStatus;

namespace FakeMicro.Grains
{
    /// <summary>
    /// 用户查询Grain - 负责用户数据查询和统计
    /// </summary>
    [StatelessWorker(10)]
    [Reentrant]
    public class UserQueryGrain : OrleansGrainBase, IUserQueryGrain
    {
        private readonly ILogger<UserQueryGrain> _logger;
        private readonly IUserRepository _userRepository;

        public UserQueryGrain(
            ILogger<UserQueryGrain> logger, 
            IUserRepository userRepository) : base(logger)
        {
            _logger = logger;
            _userRepository = userRepository;
        }

        public async Task<long?> FindUserByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default)
        {
            try
            {
                // 先尝试按用户名查找
                var user = await _userRepository.GetByUsernameAsync(usernameOrEmail);
                if (user == null)
                {
                    // 再尝试按邮箱查找
                    user = await _userRepository.GetByEmailAsync(usernameOrEmail);
                }
                return user?.id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查找用户失败: {UsernameOrEmail}", usernameOrEmail);
                return null;
            }
        }

        public async Task<UserStatistics> GetUserStatisticsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var users = await _userRepository.GetAllAsync();
                
                return new UserStatistics
                {
                    TotalUsers = users.Count(),
                    ActiveUsers = users.Count(u => u.status == UserStatusEnum.Active.ToString()),
                    NewUsersToday = users.Count(u => u.CreatedAt.Date == DateTime.UtcNow.Date),
                    StatusDistribution = users.GroupBy(u => u.status).ToDictionary(g => g.Key, g => g.Count()),
                    RoleDistribution = users.GroupBy(u => u.role).ToDictionary(g => g.Key, g => g.Count())
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户统计信息失败");
                return new UserStatistics();
            }
        }

        public async Task<List<UserDto>> GetUsersAsync(string? username = null, string? email = null, string? status = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var users = await _userRepository.SearchUsersAsync(username, email, status);
                
                // 转换为DTO
                return users.Select(u => new UserDto
                {
                    Id = u.id,
                    Username = u.username,
                    Email = u.email,
                    Phone = u.phone,
                    Role = Enum.TryParse<UserRoleEnum>(u.role, out var role) ? role : UserRoleEnum.User,
                    Status = Enum.TryParse<UserStatusEnum>(u.status, out var statusEnum) ? statusEnum : UserStatusEnum.Pending,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt.Value
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户列表失败");
                return new List<UserDto>();
            }
        }
    }
}
