using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using System.Security.Claims;
using FakeMicro.Interfaces.Models;
using System;

namespace FakeMicro.Api.Controllers
{
    /// <summary>
    /// 受保护的控制器 - 基于Orleans实现的认证授权管理
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 需要认证
    public class ProtectedController : ControllerBase
    {
        private readonly IClusterClient _clusterClient;
        private readonly ILogger<ProtectedController> _logger;

        /// <summary>
        /// 构造函数 - 注入Orleans集群客户端和日志记录器
        /// </summary>
        public ProtectedController(IClusterClient clusterClient, ILogger<ProtectedController> logger)
        {
            _clusterClient = clusterClient;
            _logger = logger;
        }

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        [HttpGet("user-info")]

        public IActionResult GetUserInfo()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var username = User.FindFirst(ClaimTypes.Name)?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                _logger.LogInformation("用户访问个人信息: {UserId}, Username: {Username}", userId, username);

                // 可以在这里使用Orleans Grain获取更多用户信息
                // var userGrain = _clusterClient.GetGrain<IUserGrain>(long.Parse(userId));
                // var userDetails = await userGrain.GetUserDetailsAsync();

                if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out long userIdLong))
                {
                    _logger.LogWarning("无效的用户ID格式: {UserId}", userId);
                    return BadRequest(new ErrorResponse { Success = false, Message = "无效的用户信息" });
                }

                return Ok(new ControllerUserInfo
                {
                    Id = userIdLong,
                    Username = username,
                    Role = role
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户信息失败: {Message}", ex.Message);
                return StatusCode(500, new ErrorResponse { Success = false, Message = "获取用户信息失败: " + ex.Message });
            }
        }

        /// <summary>
        /// 需要管理员权限的接口
        /// </summary>
        [HttpGet("admin-only")]
        [Authorize(Roles = "Admin")]

        public IActionResult AdminOnly()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("管理员访问: {Username}, UserId: {UserId}", username, userId);
            
            return Ok(new AdminResponse
            {
                Success = true,
                Message = "欢迎管理员",
                Timestamp = DateTime.UtcNow,
                Username = username
            });
        }

        /// <summary>
        /// 需要特定角色的接口
        /// </summary>
        [HttpGet("role-based/{role}")]
        [Authorize(Roles = "Admin,Manager")]

        public IActionResult RoleBased(string role)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            _logger.LogInformation("角色访问: {Username} - {Role}, UserId: {UserId}", username, role, userId);
            
            return Ok(new RoleBasedResponse
            {
                Success = true,
                Message = $"欢迎 {role} 角色用户",
                UserRole = userRole,
                RequestedRole = role,
                Username = username,
                Timestamp = DateTime.UtcNow
            });
        }
    }
    
    // 响应模型已移至 FakeMicro.Interfaces.Models 命名空间下的 AuthResponseModels.cs 文件中
}