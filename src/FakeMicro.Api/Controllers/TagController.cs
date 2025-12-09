using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FakeMicro.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using FakeMicro.Entities;
using Microsoft.Extensions.Logging;
using Orleans;

namespace FakeMicro.Api.Controllers
{
    /// <summary>
    /// 标签控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class TagController : ControllerBase
    {
        private readonly IClusterClient _clusterClient;
        private readonly ILogger<TagController> _logger;

        public TagController(IClusterClient clusterClient, ILogger<TagController> logger)
        {
            _clusterClient = clusterClient;
            _logger = logger;
        }

        /// <summary>
        /// 获取标签
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Tag>> GetTag(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (id == default(Guid))
                {
                    return BadRequest(new { Error = "标签ID不能为空" });
                }

                var grain = _clusterClient.GetGrain<ITagGrain>(id.ToString());
                var result = await grain.GetAsync(cancellationToken);
                if (result == null)
                    return NotFound(new { Message = "未找到指定的标签" });
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取标签时发生系统错误: {Id}", id);
                return StatusCode(500, new { Error = "系统内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 根据用户ID获取标签列表
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<Tag>>> GetTagsByUserId(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (userId == default(Guid))
                {
                    return BadRequest(new { Error = "用户ID不能为空" });
                }

                var grain = _clusterClient.GetGrain<ITagGrain>("list");
                var result = await grain.GetByUserIdAsync(userId, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据用户ID获取标签列表时发生系统错误: {UserId}", userId);
                return StatusCode(500, new { Error = "系统内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 根据用户ID和标签名称获取标签
        /// </summary>
        [HttpGet("user/{userId}/name/{name}")]
        public async Task<ActionResult<Tag>> GetTagByUserIdAndName(Guid userId, string name, CancellationToken cancellationToken = default)
        {
            try
            {
                if (userId == default(Guid))
                {
                    return BadRequest(new { Error = "用户ID不能为空" });
                }

                if (string.IsNullOrEmpty(name))
                {
                    return BadRequest(new { Error = "标签名称不能为空" });
                }

                var grain = _clusterClient.GetGrain<ITagGrain>("list");
                var result = await grain.GetByUserIdAndNameAsync(userId, name, cancellationToken);
                if (result == null)
                    return NotFound(new { Message = "未找到指定的标签" });
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据用户ID和标签名称获取标签时发生系统错误: {UserId}, {Name}", userId, name);
                return StatusCode(500, new { Error = "系统内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 检查标签名称是否存在
        /// </summary>
        [HttpGet("exists/user/{userId}/name/{name}")]
        public async Task<ActionResult<bool>> CheckTagNameExists(Guid userId, string name, [FromQuery] Guid excludeId = default, CancellationToken cancellationToken = default)
        {
            try
            {
                if (userId == default(Guid))
                {
                    return BadRequest(new { Error = "用户ID不能为空" });
                }

                if (string.IsNullOrEmpty(name))
                {
                    return BadRequest(new { Error = "标签名称不能为空" });
                }

                var grain = _clusterClient.GetGrain<ITagGrain>("list");
                var result = await grain.NameExistsAsync(userId, name, excludeId, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查标签名称是否存在时发生系统错误: {UserId}, {Name}, {ExcludeId}", userId, name, excludeId);
                return StatusCode(500, new { Error = "系统内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 创建标签
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Tag>> CreateTag([FromBody] Tag tag, CancellationToken cancellationToken = default)
        {
            try
            {
                if (tag == null)
                {
                    return BadRequest(new { Error = "请求数据不能为空" });
                }

                var newId = Guid.NewGuid();
                var grain = _clusterClient.GetGrain<ITagGrain>(newId.ToString());
                var result = await grain.CreateAsync(tag, cancellationToken);
                if (result != null)
                    return CreatedAtAction(nameof(GetTag), new { id = result.Id }, result);
                return BadRequest(new { Error = "创建标签失败" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建标签时发生系统错误");
                return StatusCode(500, new { Error = "系统内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 更新标签
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<Tag>> UpdateTag(Guid id, [FromBody] Tag tag, CancellationToken cancellationToken = default)
        {
            try
            {
                if (id == default(Guid))
                {
                    return BadRequest(new { Error = "主键值无效" });
                }

                if (tag == null)
                {
                    return BadRequest(new { Error = "请求数据不能为空" });
                }

                var grain = _clusterClient.GetGrain<ITagGrain>(id.ToString());
                var result = await grain.UpdateAsync(tag, cancellationToken);
                if (result != null)
                    return Ok(result);
                return NotFound(new { Message = "未找到指定的标签" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新标签时发生系统错误: {Id}", id);
                return StatusCode(500, new { Error = "系统内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 删除标签
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTag(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (id == default(Guid))
                {
                    return BadRequest(new { Error = "主键值无效" });
                }

                var grain = _clusterClient.GetGrain<ITagGrain>(id.ToString());
                var result = await grain.DeleteAsync(cancellationToken);
                if (result)
                    return NoContent();
                return NotFound(new { Message = "未找到指定的标签" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除标签时发生系统错误: {Id}", id);
                return StatusCode(500, new { Error = "系统内部错误，请稍后重试" });
            }
        }
    }
}