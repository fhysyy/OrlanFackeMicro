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
using System.Diagnostics;


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
        public async Task<ActionResult<BaseResultModel>> GetTag(Guid id, CancellationToken cancellationToken = default)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            try
            {
                if (id == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 获取标签请求参数无效，ID不能为空", traceId);
                    return BadRequest(BaseResultModel.FailedResult(null,"标签ID不能为空"));
                }

                _logger.LogInformation("[TraceId: {TraceId}] 开始获取标签，ID: {Id}", traceId, id);
                var grain = _clusterClient.GetGrain<ITagGrain>(id.ToString());
                var result = await grain.GetAsync(cancellationToken);
                if (result == null)
                {
                    _logger.LogInformation("[TraceId: {TraceId}] 未找到指定的标签，ID: {Id}", traceId, id);
                    return NotFound(BaseResultModel.FailedResult("404", "未找到指定的标签"));
                }
                return Ok(BaseResultModel.SuccessResult(result, "获取标签成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId: {TraceId}] 获取标签时发生系统错误: {Id}", traceId, id);
                return StatusCode(500, BaseResultModel.FailedResult("500", "系统内部错误，请稍后重试"));
            }
        }

        /// <summary>
        /// 根据用户ID获取标签列表
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<BaseResultModel>> GetTagsByUserId(Guid userId, CancellationToken cancellationToken = default)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            try
            {
                if (userId == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 获取标签列表请求参数无效，用户ID不能为空", traceId);
                    return BadRequest(BaseResultModel.FailedResult(null, "用户ID不能为空"));
                }

                _logger.LogInformation("[TraceId: {TraceId}] 开始根据用户ID获取标签列表，用户ID: {UserId}", traceId, userId);
                var grain = _clusterClient.GetGrain<ITagGrain>("list");
                var result = await grain.GetByUserIdAsync(userId, cancellationToken);
                
                _logger.LogInformation("[TraceId: {TraceId}] 根据用户ID获取标签列表成功，用户ID: {UserId}，数量: {Count}", traceId, userId, result.Count);
                return Ok(BaseResultModel.SuccessResult(result, "获取标签列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId: {TraceId}] 根据用户ID获取标签列表时发生系统错误: {UserId}", traceId, userId);
                return StatusCode(500, BaseResultModel.FailedResult(null, "系统内部错误，请稍后重试",500));
            }
        }

        /// <summary>
        /// 根据用户ID和标签名称获取标签
        /// </summary>
        [HttpGet("user/{userId}/name/{name}")]
        public async Task<ActionResult<BaseResultModel>> GetTagByUserIdAndName(Guid userId, string name, CancellationToken cancellationToken = default)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            try
            {
                if (userId == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 根据用户ID和名称获取标签请求参数无效，用户ID不能为空", traceId);
                    return BadRequest(BaseResultModel.FailedResult(null, "用户ID不能为空", 400));
                }

                if (string.IsNullOrEmpty(name))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 根据用户ID和名称获取标签请求参数无效，标签名称不能为空", traceId);
                    return BadRequest(BaseResultModel.FailedResult(null, "标签名称不能为空", 400));
                }

                _logger.LogInformation("[TraceId: {TraceId}] 开始根据用户ID和名称获取标签，用户ID: {UserId}, 标签名称: {Name}", traceId, userId, name);
                var grain = _clusterClient.GetGrain<ITagGrain>("list");
                var result = await grain.GetByUserIdAndNameAsync(userId, name, cancellationToken);
                if (result == null)
                {
                    _logger.LogInformation("[TraceId: {TraceId}] 未找到指定的标签，用户ID: {UserId}, 标签名称: {Name}", traceId, userId, name);
                    return NotFound(BaseResultModel.FailedResult("404", "未找到指定的标签", 404));
                }
                return Ok(BaseResultModel.SuccessResult(result, "根据用户ID和名称获取标签成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId: {TraceId}] 根据用户ID和标签名称获取标签时发生系统错误: {UserId}, {Name}", traceId, userId, name);
                return StatusCode(500, BaseResultModel.FailedResult("500", "系统内部错误，请稍后重试", 500));
            }
        }

        /// <summary>
        /// 检查标签名称是否存在
        /// </summary>
        [HttpGet("exists/user/{userId}/name/{name}")]
        public async Task<ActionResult<BaseResultModel>> CheckTagNameExists(Guid userId, string name, [FromQuery] Guid excludeId = default, CancellationToken cancellationToken = default)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            try
            {
                if (userId == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 检查标签名称是否存在请求参数无效，用户ID不能为空", traceId);
                    return BadRequest(BaseResultModel.FailedResult("400", "用户ID不能为空",400));
                }

                if (string.IsNullOrEmpty(name))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 检查标签名称是否存在请求参数无效，标签名称不能为空", traceId);
                    return BadRequest(BaseResultModel.FailedResult("400", "标签名称不能为空", 400));
                }

                _logger.LogInformation("[TraceId: {TraceId}] 开始检查标签名称是否存在，用户ID: {UserId}, 标签名称: {Name}, 排除ID: {ExcludeId}", traceId, userId, name, excludeId);
                var grain = _clusterClient.GetGrain<ITagGrain>("list");
                var result = await grain.NameExistsAsync(userId, name, excludeId, cancellationToken);
                
                _logger.LogInformation("[TraceId: {TraceId}] 检查标签名称是否存在成功，用户ID: {UserId}, 标签名称: {Name}, 结果: {Result}", traceId, userId, name, result);
                return Ok(BaseResultModel.SuccessResult(result, "检查标签名称是否存在成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId: {TraceId}] 检查标签名称是否存在时发生系统错误: {UserId}, {Name}, {ExcludeId}", traceId, userId, name, excludeId);
                return StatusCode(500, BaseResultModel.FailedResult("500", "系统内部错误，请稍后重试",500));
            }
        }

        /// <summary>
        /// 创建标签
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BaseResultModel>> CreateTag([FromBody] Tag tag, CancellationToken cancellationToken = default)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            try
            {
                if (tag == null)
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 创建标签请求参数无效，请求数据不能为空", traceId);
                    return BadRequest(BaseResultModel.FailedResult("400", "请求数据不能为空", 400));
                }

                // 验证必填字段
                if (string.IsNullOrEmpty(tag.Name))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 创建标签请求参数无效，标签名称不能为空", traceId);
                    return BadRequest(BaseResultModel.FailedResult("400", "标签名称不能为空",400));
                }

                if (tag.UserId == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 创建标签请求参数无效，用户ID不能为空", traceId);
                    return BadRequest(BaseResultModel.FailedResult("400", "用户ID不能为空",400));
                }

                _logger.LogInformation("[TraceId: {TraceId}] 开始创建标签，用户ID: {UserId}, 标签名称: {TagName}", traceId, tag.UserId, tag.Name);
                var newId = Guid.NewGuid();
                tag.Id = newId;
                tag.CreatedAt = DateTime.UtcNow;
                tag.UpdatedAt = DateTime.UtcNow;
                
                var grain = _clusterClient.GetGrain<ITagGrain>(newId.ToString());
                var result = await grain.CreateAsync(tag, cancellationToken);
                
                if (result != null)
                {
                    _logger.LogInformation("[TraceId: {TraceId}] 创建标签成功，ID: {Id}", traceId, result.Id);
                    return CreatedAtAction(nameof(GetTag), new { id = result.Id }, BaseResultModel.SuccessResult(result, "创建标签成功"));
                }
                
                _logger.LogWarning("[TraceId: {TraceId}] 创建标签失败，用户ID: {UserId}, 标签名称: {TagName}", traceId, tag.UserId, tag.Name);
                return BadRequest(BaseResultModel.FailedResult("400", "创建标签失败"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId: {TraceId}] 创建标签时发生系统错误");
                return StatusCode(500, BaseResultModel.FailedResult("500", "系统内部错误，请稍后重试", 500));
            }
        }

        /// <summary>
        /// 更新标签
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<BaseResultModel>> UpdateTag(Guid id, [FromBody] Tag tag, CancellationToken cancellationToken = default)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            try
            {
                if (id == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 更新标签请求参数无效，主键值无效", traceId);
                    return BadRequest(BaseResultModel.FailedResult("400", "主键值无效", 400));
                }

                if (tag == null)
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 更新标签请求参数无效，请求数据不能为空", traceId);
                    return BadRequest(BaseResultModel.FailedResult("400", "请求数据不能为空", 400));
                }

                // 确保ID一致
                if (tag.Id != id)
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 更新标签请求参数无效，URL中的ID与请求体中的ID不一致", traceId);
                    return BadRequest(BaseResultModel.FailedResult("400", "URL中的ID与请求体中的ID不一致",400));
                }

                // 验证必填字段
                if (string.IsNullOrEmpty(tag.Name))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 更新标签请求参数无效，标签名称不能为空", traceId);
                    return BadRequest(BaseResultModel.FailedResult("400", "标签名称不能为空",400));
                }

                if (tag.UserId == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 更新标签请求参数无效，用户ID不能为空", traceId);
                    return BadRequest(BaseResultModel.FailedResult("400", "用户ID不能为空"));
                }

                _logger.LogInformation("[TraceId: {TraceId}] 开始更新标签，ID: {Id}", traceId, id);
                tag.UpdatedAt = DateTime.UtcNow;
                
                var grain = _clusterClient.GetGrain<ITagGrain>(id.ToString());
                var result = await grain.UpdateAsync(tag, cancellationToken);
                
                if (result != null)
                {
                    _logger.LogInformation("[TraceId: {TraceId}] 更新标签成功，ID: {Id}", traceId, id);
                    return Ok(BaseResultModel.SuccessResult(result, "更新标签成功"));
                }
                
                _logger.LogInformation("[TraceId: {TraceId}] 未找到指定的标签，ID: {Id}", traceId, id);
                return NotFound(BaseResultModel.FailedResult("404", "未找到指定的标签"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId: {TraceId}] 更新标签时发生系统错误: {Id}", traceId, id);
                return StatusCode(500, BaseResultModel.FailedResult("500", "系统内部错误，请稍后重试", 500));
            }
        }

        /// <summary>
        /// 删除标签
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<BaseResultModel>> DeleteTag(Guid id, CancellationToken cancellationToken = default)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            try
            {
                if (id == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 删除标签请求参数无效，主键值无效", traceId);
                    return BadRequest(BaseResultModel.FailedResult("400", "主键值无效", 400));
                }

                _logger.LogInformation("[TraceId: {TraceId}] 开始删除标签，ID: {Id}", traceId, id);
                var grain = _clusterClient.GetGrain<ITagGrain>(id.ToString());
                var result = await grain.DeleteAsync(cancellationToken);
                
                if (result)
                {
                    _logger.LogInformation("[TraceId: {TraceId}] 删除标签成功，ID: {Id}", traceId, id);
                    return Ok(BaseResultModel.SuccessResult(true, "删除标签成功"));
                }
                
                _logger.LogInformation("[TraceId: {TraceId}] 未找到指定的标签，ID: {Id}", traceId, id);
                return NotFound(BaseResultModel.FailedResult(false, "未找到指定的标签", 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId: {TraceId}] 删除标签时发生系统错误: {Id}", traceId, id);
                return StatusCode(500, BaseResultModel.FailedResult(false, "系统内部错误，请稍后重试", 500));
            }
        }
    }
}