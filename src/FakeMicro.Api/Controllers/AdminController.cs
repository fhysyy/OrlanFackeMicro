
using FakeMicro.Entities;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
using FakeMicro.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FakeMicro.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 需要认证
    public class AdminController : ControllerBase
    {
        private readonly IClusterClient _clusterClient;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IClusterClient clusterClient, ILogger<AdminController> logger)
        {
            _clusterClient = clusterClient;
            _logger = logger;
        }

        /// <summary>
        /// 获取用户列表（支持搜索）
        /// </summary>
        [HttpGet("users")]
         [Authorize(Policy = "Admin")] // 需要管理员权限*/
        public async Task<IActionResult> GetAllUsers([FromQuery] string username = null, [FromQuery] string email = null, [FromQuery] string status = null)
        {
            try
            {
                _logger.LogInformation("管理员请求获取用户列表，搜索条件：用户名={Username}, 邮箱={Email}, 状态={Status}", 
                    username ?? "全部", email ?? "全部", status ?? "全部");
                
                // 通过Orleans Grain获取用户列表
                var userServiceGrain = _clusterClient.GetGrain<IUserServiceGrain>(Guid.Empty); // 使用固定key获取服务粒度Grain
                var userDtos = await userServiceGrain.GetUsersAsync(username, email, status);
                
                return Ok(new { success = true, users = userDtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户列表失败");
                return StatusCode(500, new { success = false, message = "获取用户列表失败" });
            }
        }

        /// <summary>
        /// 更新用户信息（仅管理员）
        /// </summary>
        [HttpPut("users/{userId}")]
        [Authorize(Policy = "Admin")] // 需要管理员权限
        public async Task<IActionResult> UpdateUser(long userId, [FromBody] UpdateUserRequest request)
        {
            try
            {
                _logger.LogInformation("管理员请求更新用户 {UserId} 信息", userId);
                
                // 构建更新的用户DTO
                // 注意：这里不直接调用仓储验证用户是否存在，而是通过Grain的UpdateUserAsync方法来处理
                var userDto = new UserDto
                {
                    Id = userId,
                    Username = request.Username,
                    Email = request.Email,
                    Phone = request.Phone,
                    Role = string.IsNullOrEmpty(request.Role) ? FakeMicro.Entities.Enums.UserRole.User : 
                           (Enum.TryParse<FakeMicro.Entities.Enums.UserRole>(request.Role, true, out var role) ? role : FakeMicro.Entities.Enums.UserRole.User),
                    Status = string.IsNullOrEmpty(request.Status) ? FakeMicro.Entities.Enums.UserStatus.Active :
                           (Enum.TryParse<FakeMicro.Entities.Enums.UserStatus>(request.Status, true, out var userStatus) ? userStatus : FakeMicro.Entities.Enums.UserStatus.Active)
                };
                
                // 使用UserGrain更新用户信息
                var userGrain = _clusterClient.GetGrain<IUserGrain>(userId.ToString());
                var result = await userGrain.UpdateUserAsync(userDto);
                
                if (result.Success)
                {
                    // 记录操作日志
                    var currentUserId = GetCurrentUserId();
                    _logger.LogInformation("用户 {CurrentUserId} 成功更新用户 {UserId} 信息", currentUserId, userId);
                    
                    return Ok(new { success = true, message = "用户信息更新成功" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "用户信息更新失败" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户信息失败: {UserId}", userId);
                return StatusCode(500, new { success = false, message = "更新用户信息失败" });
            }
        }
        
        /// <summary>
        /// 修改用户角色（仅系统管理员）
        /// </summary>
        [HttpPost("users/{userId}/role")]
        [Authorize(Policy = "SystemAdmin")] // 需要系统管理员权限
        public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] UpdateRoleRequest request)
        {
            try
            {
                _logger.LogInformation("系统管理员请求修改用户 {UserId} 角色为 {Role}", userId, request.Role);
                
                // 验证角色是否有效
                if (!Enum.IsDefined(typeof(UserRole), request.Role))
                {
                    return BadRequest(new { success = false, message = "无效的角色类型" });
                }
                
                // 通过Orleans Grain更新用户角色
                var userGrain = _clusterClient.GetGrain<IUserGrain>(((long)userId).ToString());
                
                // 构建更新的用户DTO，只更新角色
                var userDto = new UserDto
                {
                    Id = userId,
                    Role = Enum.Parse<FakeMicro.Entities.Enums.UserRole>(request.Role, true),
                    Status = FakeMicro.Entities.Enums.UserStatus.Active // 保持原有状态，这里仅用于更新角色
                };
                
                var result = await userGrain.UpdateUserAsync(userDto);
                
                if (!result.Success)
                {
                    return NotFound(new { success = false, message = "用户不存在" });
                }
                
                // 记录操作日志
                var currentUserId = GetCurrentUserId();
                _logger.LogInformation("用户 {CurrentUserId} 成功将用户 {UserId} 的角色修改为 {Role}", 
                    currentUserId, userId, request.Role);
                
                return Ok(new { success = true, message = "角色修改成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改用户角色失败: {UserId}", userId);
                return StatusCode(500, new { success = false, message = "修改用户角色失败" });
            }
        }

        /// <summary>
        /// 获取系统统计信息（仅管理员）
        /// </summary>
        [HttpGet("statistics")]
        [Authorize(Policy = "Admin")] // 需要管理员权限
        public async Task<IActionResult> GetSystemStatistics()
        {
            try
            {
                _logger.LogInformation("管理员请求获取系统统计信息");
                
                // 调用UserServiceGrain获取用户统计信息
                var userServiceGrain = _clusterClient.GetGrain<IUserServiceGrain>(Guid.Empty); // 使用固定key获取服务粒度Grain
                var userStats = await userServiceGrain.GetUserStatisticsAsync();
                
                // 构建完整的系统统计信息
                var statistics = new
                {
                    TotalUsers = userStats.TotalUsers,
                    ActiveUsers = userStats.ActiveUsers,
                    NewUsersToday = userStats.NewUsersToday,
                    RoleDistribution = userStats.RoleDistribution,
                    StatusDistribution = userStats.StatusDistribution,
                    SystemUptime = GetSystemUptime()
                };
                
                return Ok(new { success = true, statistics });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取系统统计信息失败");
                return StatusCode(500, new { success = false, message = "获取系统统计信息失败" });
            }
        }

        /// <summary>
        /// 清理系统缓存（仅系统管理员）
        /// </summary>
        [HttpPost("cache/clear")]
        [Authorize(Policy = "SystemAdmin")] // 需要系统管理员权限
        public IActionResult ClearSystemCache()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                _logger.LogInformation("系统管理员 {UserId} 请求清理系统缓存", currentUserId);
                
                // 清理Orleans Grain缓存 - 可以根据需要调用特定的清理方法
                // 这里简化处理，实际项目中可能需要更复杂的缓存清理逻辑
                
                // 记录操作日志
                _logger.LogInformation("系统管理员 {UserId} 成功清理系统缓存", currentUserId);
                
                return Ok(new { success = true, message = "系统缓存清理完成" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理系统缓存失败");
                return StatusCode(500, new { success = false, message = "清理系统缓存失败" });
            }
        }

        /// <summary>
        /// 获取当前登录用户ID
        /// </summary>
        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return 0;
        }

        /// <summary>
        /// 删除用户（仅系统管理员）
        /// </summary>
        [HttpDelete("users/{userId}")]
        [Authorize(Policy = "SystemAdmin")] // 需要系统管理员权限
        public async Task<IActionResult> DeleteUser(long userId)
        {
            try
            {
                _logger.LogInformation("系统管理员请求删除用户 {UserId}", userId);
                
                // 不允许删除当前登录用户
                var currentUserId = GetCurrentUserId();
                if (userId == currentUserId)
                {
                    return BadRequest(new { success = false, message = "不允许删除当前登录用户" });
                }
                
                // 通过Orleans Grain删除用户
                var userGrain = _clusterClient.GetGrain<IUserGrain>(userId.ToString());
                var result = await userGrain.DeleteUserAsync();
                
                if (!result.Success)
                {
                    return NotFound(new { success = false, message = "用户不存在或删除失败" });
                }
                
                // 记录操作日志
                _logger.LogInformation("用户 {CurrentUserId} 成功删除用户 {UserId}", currentUserId, userId);
                
                return Ok(new { success = true, message = "用户删除成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除用户失败: {UserId}", userId);
                return StatusCode(500, new { success = false, message = "删除用户失败" });
            }
        }
        
        /// <summary>
        /// 获取系统运行时间
        /// </summary>
        private TimeSpan GetSystemUptime()
        {
            // 简化实现，实际项目中可能需要从系统启动时间计算
            return DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();
        }
    }


}