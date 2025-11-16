using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using System;
using System.Threading.Tasks;

namespace FakeMicro.Api.Controllers
{
    /// <summary>
    /// 权限管理控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PermissionController : ControllerBase
    {
        private readonly IClusterClient _clusterClient;
        private readonly ILogger<PermissionController> _logger;

        public PermissionController(IClusterClient clusterClient, ILogger<PermissionController> logger)
        {
            _clusterClient = clusterClient;
            _logger = logger;
        }

        /// <summary>
        /// 检查用户权限
        /// </summary>
        [HttpPost("check")]
        public async Task<IActionResult> CheckPermission([FromBody] CheckPermissionRequest request)
        {
            try
            {
                var permissionGrain = _clusterClient.GetGrain<IPermissionGrain>(Guid.NewGuid().ToString());
                var hasPermission = await permissionGrain.HasPermissionAsync(request.UserId, request.Resource, request.PermissionType);
                
                return Ok(new { HasPermission = hasPermission });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查权限失败: UserId={UserId}, Resource={Resource}, PermissionType={PermissionType}", 
                    request.UserId, request.Resource, request.PermissionType);
                return StatusCode(500, "检查权限失败");
            }
        }

        /// <summary>
        /// 获取用户权限列表
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserPermissions(int userId)
        {
            try
            {
                var permissionGrain = _clusterClient.GetGrain<IPermissionGrain>(Guid.NewGuid().ToString());
                var permissions = await permissionGrain.GetUserPermissionsAsync(userId);
                
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户权限失败: UserId={UserId}", userId);
                return StatusCode(500, "获取用户权限失败");
            }
        }

        /// <summary>
        /// 获取角色权限列表
        /// </summary>
        [HttpGet("role/{roleId}")]
        public async Task<IActionResult> GetRolePermissions(int roleId)
        {
            try
            {
                var permissionGrain = _clusterClient.GetGrain<IPermissionGrain>(Guid.NewGuid().ToString());
                var permissions = await permissionGrain.GetRolePermissionsAsync(roleId);
                
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取角色权限失败: RoleId={RoleId}", roleId);
                return StatusCode(500, "获取角色权限失败");
            }
        }

        /// <summary>
        /// 为用户分配角色
        /// </summary>
        [HttpPost("assign-role")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> AssignRoleToUser([FromBody] AssignRoleRequest request)
        {
            try
            {
                var permissionGrain = _clusterClient.GetGrain<IPermissionGrain>(Guid.NewGuid().ToString());
                var result = await permissionGrain.AssignRoleToUserAsync(request.UserId, request.RoleId);
                
                if (result)
                {
                    return Ok(new { Message = "角色分配成功" });
                }
                else
                {
                    return BadRequest("角色分配失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分配角色失败: UserId={UserId}, RoleId={RoleId}", request.UserId, request.RoleId);
                return StatusCode(500, "分配角色失败");
            }
        }

        /// <summary>
        /// 为用户移除角色
        /// </summary>
        [HttpPost("remove-role")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> RemoveRoleFromUser([FromBody] AssignRoleRequest request)
        {
            try
            {
                var permissionGrain = _clusterClient.GetGrain<IPermissionGrain>(Guid.NewGuid().ToString());
                var result = await permissionGrain.RemoveRoleFromUserAsync(request.UserId, request.RoleId);
                
                if (result)
                {
                    return Ok(new { Message = "角色移除成功" });
                }
                else
                {
                    return BadRequest("角色移除失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除角色失败: UserId={UserId}, RoleId={RoleId}", request.UserId, request.RoleId);
                return StatusCode(500, "移除角色失败");
            }
        }

        /// <summary>
        /// 为角色分配权限
        /// </summary>
        [HttpPost("assign-permission")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> AssignPermissionToRole([FromBody] AssignPermissionRequest request)
        {
            try
            {
                var permissionGrain = _clusterClient.GetGrain<IPermissionGrain>(Guid.NewGuid().ToString());
                var result = await permissionGrain.AssignPermissionToRoleAsync(request.RoleId, request.PermissionId);
                
                if (result)
                {
                    return Ok(new { Message = "权限分配成功" });
                }
                else
                {
                    return BadRequest("权限分配失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分配权限失败: RoleId={RoleId}, PermissionId={PermissionId}", request.RoleId, request.PermissionId);
                return StatusCode(500, "分配权限失败");
            }
        }

        /// <summary>
        /// 为角色移除权限
        /// </summary>
        [HttpPost("remove-permission")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> RemovePermissionFromRole([FromBody] AssignPermissionRequest request)
        {
            try
            {
                var permissionGrain = _clusterClient.GetGrain<IPermissionGrain>(Guid.NewGuid().ToString());
                var result = await permissionGrain.RemovePermissionFromRoleAsync(request.RoleId, request.PermissionId);
                
                if (result)
                {
                    return Ok(new { Message = "权限移除成功" });
                }
                else
                {
                    return BadRequest("权限移除失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除权限失败: RoleId={RoleId}, PermissionId={PermissionId}", request.RoleId, request.PermissionId);
                return StatusCode(500, "移除权限失败");
            }
        }

        /// <summary>
        /// 创建权限
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> CreatePermission([FromBody] PermissionDto permissionDto)
        {
            try
            {
                var permissionGrain = _clusterClient.GetGrain<IPermissionGrain>(Guid.NewGuid().ToString());
                var result = await permissionGrain.CreatePermissionAsync(permissionDto);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建权限失败: {PermissionName}", permissionDto.Name);
                return StatusCode(500, "创建权限失败");
            }
        }

        /// <summary>
        /// 创建角色
        /// </summary>
        [HttpPost("role")]
        //[Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> CreateRole([FromBody] RoleDto roleDto)
        {
            try
            {
                var permissionGrain = _clusterClient.GetGrain<IPermissionGrain>(Guid.NewGuid().ToString());
                var result = await permissionGrain.CreateRoleAsync(roleDto);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建角色失败: {RoleName}", roleDto.Name);
                return StatusCode(500, "创建角色失败");
            }
        }

        /// <summary>
        /// 获取所有权限
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> GetAllPermissions()
        {
            try
            {
                var permissionGrain = _clusterClient.GetGrain<IPermissionGrain>(Guid.NewGuid().ToString());
                var permissions = await permissionGrain.GetAllPermissionsAsync();
                
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有权限失败");
                return StatusCode(500, "获取所有权限失败");
            }
        }

        /// <summary>
        /// 获取所有角色
        /// </summary>
        [HttpGet("role")]
        //[Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> GetAllRoles()
        {
            try
            {
                var permissionGrain = _clusterClient.GetGrain<IPermissionGrain>(Guid.NewGuid().ToString());
                var roles = await permissionGrain.GetAllRolesAsync();
                
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有角色失败");
                return StatusCode(500, "获取所有角色失败");
            }
        }

        /// <summary>
        /// 获取用户角色
        /// </summary>
        [HttpGet("user/{userId}/roles")]
        public async Task<IActionResult> GetUserRoles(int userId)
        {
            try
            {
                var permissionGrain = _clusterClient.GetGrain<IPermissionGrain>(Guid.NewGuid().ToString());
                var roles = await permissionGrain.GetUserRolesAsync(userId);
                
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户角色失败: UserId={UserId}", userId);
                return StatusCode(500, "获取用户角色失败");
            }
        }

        /// <summary>
        /// 获取审计日志
        /// </summary>
        [HttpGet("audit")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> GetAuditLogs([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, [FromQuery] int? userId = null)
        {
            try
            {
                var permissionGrain = _clusterClient.GetGrain<IPermissionGrain>(Guid.NewGuid().ToString());
                var logs = await permissionGrain.GetAuditLogsAsync(startDate, endDate, userId);
                
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取审计日志失败");
                return StatusCode(500, "获取审计日志失败");
            }
        }
    }
}