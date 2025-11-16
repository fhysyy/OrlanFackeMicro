// // // // using FakeMicro.Api.Dtos; // DTO类已移动到FakeMicro.Interfaces.Models命名空间 // DTO类已移动到FakeMicro.Interfaces.Models命名空间 // DTO类已移动到FakeMicro.Interfaces.Models命名空间 // DTO类已移动到FakeMicro.Interfaces.Models命名空间
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FakeMicro.Api.Controllers
{
    /// <summary>
    /// 用户管理控制器（遵循最佳实践）
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IClusterClient _clusterClient;
        private readonly ILogger<UserController> _logger;

        public UserController(IClusterClient clusterClient, ILogger<UserController> logger)
        {
            _clusterClient = clusterClient;
            _logger = logger;
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        [HttpGet("{userId}")]
        public async Task<ActionResult<UserDto>> GetUser(string userId)
        {
            try
            {
                var userGrain = _clusterClient.GetGrain<IUserGrain>(userId);
                var user = await userGrain.GetUserAsync();
                
                if (user == null)
                {
                    return NotFound();
                }
                
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户信息失败: {UserId}", userId);
                return StatusCode(500, "服务器内部错误");
            }
        }

        /// <summary>
        /// 更新用户信息
        /// </summary>
        [HttpPut("{userId}")]
        public async Task<ActionResult> UpdateUser(string userId, UserDto userDto)
        {
            try
            {
                var userGrain = _clusterClient.GetGrain<IUserGrain>(userId);
                var result = await userGrain.UpdateUserAsync(userDto);
                
                if (!result.Success)
                {
                    return BadRequest("更新用户信息失败");
                }
                
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户信息失败: {UserId}", userId);
                return StatusCode(500, "服务器内部错误");
            }
        }

        /// <summary>
        /// 获取用户权限
        /// </summary>
        [HttpGet("{userId}/permissions")]
        public async Task<ActionResult<List<Permission>>> GetUserPermissions(string userId)
        {
            try
            {
                var userGrain = _clusterClient.GetGrain<IUserGrain>(userId);
                var permissions = await userGrain.GetPermissionsAsync();
                
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户权限失败: {UserId}", userId);
                return StatusCode(500, "服务器内部错误");
            }
        }
    }
}