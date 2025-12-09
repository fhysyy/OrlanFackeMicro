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
    /// 笔记本控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    ////[Authorize]
    public class NotebookController : ControllerBase
    {
        private readonly IClusterClient _clusterClient;
        private readonly ILogger<NotebookController> _logger;

        public NotebookController(IClusterClient clusterClient, ILogger<NotebookController> logger)
        {
            _clusterClient = clusterClient;
            _logger = logger;
        }

        /// <summary>
        /// 获取笔记本
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<BaseResultModel<Notebook>>> GetNotebook(Guid id, CancellationToken cancellationToken = default)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            try
            {
                if (id == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 获取笔记本请求参数无效，ID不能为空", traceId);
                    var failedResult = BaseResultModel<Notebook>.FailedResult(null, "笔记本ID不能为空", 400);
                    failedResult.TraceId = traceId;
                    return BadRequest(failedResult);
                }

                _logger.LogInformation("[TraceId: {TraceId}] 开始获取笔记本，ID: {Id}", traceId, id);
                var grain = _clusterClient.GetGrain<INotebookGrain>(id.ToString());
                var result = await grain.GetAsync(cancellationToken);
                
                if (result == null)
                {
                    _logger.LogInformation("[TraceId: {TraceId}] 未找到指定的笔记本，ID: {Id}", traceId, id);
                    var failedResult = BaseResultModel<Notebook>.FailedResult(null, "未找到指定的笔记本", 404);
                    failedResult.TraceId = traceId;
                    return NotFound(failedResult);
                }
                
                _logger.LogInformation("[TraceId: {TraceId}] 获取笔记本成功，ID: {Id}", traceId, id);
                var successResult = BaseResultModel<Notebook>.SuccessResult(result, "获取笔记本成功");
                successResult.TraceId = traceId;
                return Ok(successResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId: {TraceId}] 获取笔记本时发生系统错误: {Id}", traceId, id);
                var failedResult = BaseResultModel<Notebook>.FailedResult(null, "系统内部错误，请稍后重试", 500);
                failedResult.TraceId = traceId;
                return StatusCode(500, failedResult);
            }
        }

        /// <summary>
        /// 根据用户ID获取笔记本列表
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<BaseResultModel<List<Notebook>>>> GetNotebooksByUserId(Guid userId, CancellationToken cancellationToken = default)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            try
            {
                if (userId == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 获取笔记本列表请求参数无效，用户ID不能为空", traceId);
                    var failedResult = BaseResultModel<List<Notebook>>.FailedResult(null, "用户ID不能为空", 400);
                    failedResult.TraceId = traceId;
                    return BadRequest(failedResult);
                }

                _logger.LogInformation("[TraceId: {TraceId}] 开始根据用户ID获取笔记本列表，用户ID: {UserId}", traceId, userId);
                var grain = _clusterClient.GetGrain<INotebookGrain>("list");
                var result = await grain.GetByUserIdAsync(userId, cancellationToken);
                
                _logger.LogInformation("[TraceId: {TraceId}] 根据用户ID获取笔记本列表成功，用户ID: {UserId}，数量: {Count}", traceId, userId, result.Count);
                var successResult = BaseResultModel<List<Notebook>>.SuccessResult(result, "获取笔记本列表成功");
                successResult.TraceId = traceId;
                return Ok(successResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId: {TraceId}] 根据用户ID获取笔记本列表时发生系统错误: {UserId}", traceId, userId);
                var failedResult = BaseResultModel<List<Notebook>>.FailedResult(null, "系统内部错误，请稍后重试", 500);
                failedResult.TraceId = traceId;
                return StatusCode(500, failedResult);
            }
        }

        /// <summary>
        /// 根据用户ID和父笔记本ID获取笔记本列表
        /// </summary>
        [HttpGet("user/{userId}/parent/{parentId}")]
        public async Task<ActionResult<BaseResultModel<List<Notebook>>>> GetNotebooksByUserIdAndParentId(Guid userId, Guid? parentId, CancellationToken cancellationToken = default)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            try
            {
                if (userId == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 获取笔记本列表请求参数无效，用户ID不能为空", traceId);
                    var failedResult = BaseResultModel<List<Notebook>>.FailedResult(null, "用户ID不能为空", 400);
                    failedResult.TraceId = traceId;
                    return BadRequest(failedResult);
                }

                _logger.LogInformation("[TraceId: {TraceId}] 开始根据用户ID和父笔记本ID获取笔记本列表，用户ID: {UserId}, 父笔记本ID: {ParentId}", traceId, userId, parentId);
                var grain = _clusterClient.GetGrain<INotebookGrain>("list");
                var result = await grain.GetByUserIdAndParentIdAsync(userId, parentId, cancellationToken);
                
                _logger.LogInformation("[TraceId: {TraceId}] 根据用户ID和父笔记本ID获取笔记本列表成功，用户ID: {UserId}, 父笔记本ID: {ParentId}，数量: {Count}", traceId, userId, parentId, result.Count);
                var successResult = BaseResultModel<List<Notebook>>.SuccessResult(result, "获取笔记本列表成功");
                successResult.TraceId = traceId;
                return Ok(successResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId: {TraceId}] 根据用户ID和父笔记本ID获取笔记本列表时发生系统错误: {UserId}, {ParentId}", traceId, userId, parentId);
                var failedResult = BaseResultModel<List<Notebook>>.FailedResult(null, "系统内部错误，请稍后重试", 500);
                failedResult.TraceId = traceId;
                return StatusCode(500, failedResult);
            }
        }

        /// <summary>
        /// 创建笔记本
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BaseResultModel<Notebook>>> CreateNotebook([FromBody] Notebook notebook, CancellationToken cancellationToken = default)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            try
            {
                if (notebook == null)
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 创建笔记本请求参数无效，请求数据不能为空", traceId);
                    var failedResult = BaseResultModel<Notebook>.FailedResult(null, "请求数据不能为空", 400);
                    failedResult.TraceId = traceId;
                    return BadRequest(failedResult);
                }

                // 验证必填字段
                if (string.IsNullOrWhiteSpace(notebook.Title))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 创建笔记本请求参数无效，笔记本标题不能为空", traceId);
                    var failedResult = BaseResultModel<Notebook>.FailedResult(null, "笔记本标题不能为空", 400);
                    failedResult.TraceId = traceId;
                    return BadRequest(failedResult);
                }

                if (notebook.UserId == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 创建笔记本请求参数无效，用户ID不能为空", traceId);
                    var failedResult = BaseResultModel<Notebook>.FailedResult(null, "用户ID不能为空", 400);
                    failedResult.TraceId = traceId;
                    return BadRequest(failedResult);
                }

                _logger.LogInformation("[TraceId: {TraceId}] 开始创建笔记本，用户ID: {UserId}, 标题: {Title}", traceId, notebook.UserId, notebook.Title);
                var newId = Guid.NewGuid();
                notebook.Id = newId;
                notebook.CreatedAt = DateTime.UtcNow;
                notebook.UpdatedAt = DateTime.UtcNow;
                
                var grain = _clusterClient.GetGrain<INotebookGrain>(newId.ToString());
                var result = await grain.CreateAsync(notebook, cancellationToken);
                
                if (result != null)
                {
                    _logger.LogInformation("[TraceId: {TraceId}] 创建笔记本成功，ID: {Id}", traceId, result.Id);
                    var successResult = BaseResultModel<Notebook>.SuccessResult(result, "创建笔记本成功");
                    successResult.TraceId = traceId;
                    return CreatedAtAction(nameof(GetNotebook), new { id = result.Id }, successResult);
                }
                
                _logger.LogWarning("[TraceId: {TraceId}] 创建笔记本失败，用户ID: {UserId}, 标题: {Title}", traceId, notebook.UserId, notebook.Title);
                var createFailedResult = BaseResultModel<Notebook>.FailedResult(null, "创建笔记本失败", 400);
                createFailedResult.TraceId = traceId;
                return BadRequest(createFailedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId: {TraceId}] 创建笔记本时发生系统错误");
                var failedResult = BaseResultModel<Notebook>.FailedResult(null, "系统内部错误，请稍后重试", 500);
                failedResult.TraceId = traceId;
                return StatusCode(500, failedResult);
            }
        }

        /// <summary>
        /// 更新笔记本
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<BaseResultModel<Notebook>>> UpdateNotebook(Guid id, [FromBody] Notebook notebook, CancellationToken cancellationToken = default)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            try
            {
                if (id == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 更新笔记本请求参数无效，主键值无效", traceId);
                    var failedResult = BaseResultModel<Notebook>.FailedResult(null, "主键值无效", 400);
                    failedResult.TraceId = traceId;
                    return BadRequest(failedResult);
                }

                if (notebook == null)
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 更新笔记本请求参数无效，请求数据不能为空", traceId);
                    var failedResult = BaseResultModel<Notebook>.FailedResult(null, "请求数据不能为空", 400);
                    failedResult.TraceId = traceId;
                    return BadRequest(failedResult);
                }

                // 确保ID一致
                if (notebook.Id != id)
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 更新笔记本请求参数无效，URL中的ID与请求体中的ID不一致", traceId);
                    var failedResult = BaseResultModel<Notebook>.FailedResult(null, "URL中的ID与请求体中的ID不一致", 400);
                    failedResult.TraceId = traceId;
                    return BadRequest(failedResult);
                }

                // 验证必填字段
                if (string.IsNullOrWhiteSpace(notebook.Title))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 更新笔记本请求参数无效，笔记本标题不能为空", traceId);
                    var failedResult = BaseResultModel<Notebook>.FailedResult(null, "笔记本标题不能为空", 400);
                    failedResult.TraceId = traceId;
                    return BadRequest(failedResult);
                }

                if (notebook.UserId == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 更新笔记本请求参数无效，用户ID不能为空", traceId);
                    var failedResult = BaseResultModel<Notebook>.FailedResult(null, "用户ID不能为空", 400);
                    failedResult.TraceId = traceId;
                    return BadRequest(failedResult);
                }

                _logger.LogInformation("[TraceId: {TraceId}] 开始更新笔记本，ID: {Id}", traceId, id);
                notebook.UpdatedAt = DateTime.UtcNow;
                
                var grain = _clusterClient.GetGrain<INotebookGrain>(id.ToString());
                var result = await grain.UpdateAsync(notebook, cancellationToken);
                
                if (result != null)
                {
                    _logger.LogInformation("[TraceId: {TraceId}] 更新笔记本成功，ID: {Id}", traceId, id);
                    var successResult = BaseResultModel<Notebook>.SuccessResult(result, "更新笔记本成功");
                    successResult.TraceId = traceId;
                    return Ok(successResult);
                }
                
                _logger.LogInformation("[TraceId: {TraceId}] 未找到指定的笔记本，ID: {Id}", traceId, id);
                var notFoundResult = BaseResultModel<Notebook>.FailedResult(null, "未找到指定的笔记本", 404);
                notFoundResult.TraceId = traceId;
                return NotFound(notFoundResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId: {TraceId}] 更新笔记本时发生系统错误: {Id}", traceId, id);
                var failedResult = BaseResultModel<Notebook>.FailedResult(null, "系统内部错误，请稍后重试", 500);
                failedResult.TraceId = traceId;
                return StatusCode(500, failedResult);
            }
        }

        /// <summary>
        /// 删除笔记本
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<BaseResultModel<bool>>> DeleteNotebook(Guid id, CancellationToken cancellationToken = default)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            try
            {
                if (id == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 删除笔记本请求参数无效，主键值无效", traceId);
                    var failedResult = BaseResultModel<bool>.FailedResult(false, "主键值无效", 400);
                    failedResult.TraceId = traceId;
                    return BadRequest(failedResult);
                }

                _logger.LogInformation("[TraceId: {TraceId}] 开始删除笔记本，ID: {Id}", traceId, id);
                var grain = _clusterClient.GetGrain<INotebookGrain>(id.ToString());
                var result = await grain.DeleteAsync(cancellationToken);
                
                if (result)
                {
                    _logger.LogInformation("[TraceId: {TraceId}] 删除笔记本成功，ID: {Id}", traceId, id);
                    var successResult = BaseResultModel<bool>.SuccessResult(true, "删除笔记本成功");
                    successResult.TraceId = traceId;
                    return Ok(successResult);
                }
                
                _logger.LogInformation("[TraceId: {TraceId}] 未找到指定的笔记本，ID: {Id}", traceId, id);
                var notFoundResult = BaseResultModel<bool>.FailedResult(false, "未找到指定的笔记本", 404);
                notFoundResult.TraceId = traceId;
                return NotFound(notFoundResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId: {TraceId}] 删除笔记本时发生系统错误: {Id}", traceId, id);
                var failedResult = BaseResultModel<bool>.FailedResult(false, "系统内部错误，请稍后重试", 500);
                failedResult.TraceId = traceId;
                return StatusCode(500, failedResult);
            }
        }
    }
}