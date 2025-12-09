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
    /// 笔记本控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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
        public async Task<ActionResult<Notebook>> GetNotebook(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (id == default(Guid))
                {
                    return BadRequest(new { Error = "笔记本ID不能为空" });
                }

                var grain = _clusterClient.GetGrain<INotebookGrain>(id.ToString());
                var result = await grain.GetAsync(cancellationToken);
                if (result == null)
                    return NotFound(new { Message = "未找到指定的笔记本" });
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取笔记本时发生系统错误: {Id}", id);
                return StatusCode(500, new { Error = "系统内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 根据用户ID获取笔记本列表
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<Notebook>>> GetNotebooksByUserId(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (userId == default(Guid))
                {
                    return BadRequest(new { Error = "用户ID不能为空" });
                }

                var grain = _clusterClient.GetGrain<INotebookGrain>("list");
                var result = await grain.GetByUserIdAsync(userId, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据用户ID获取笔记本列表时发生系统错误: {UserId}", userId);
                return StatusCode(500, new { Error = "系统内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 根据用户ID和父笔记本ID获取笔记本列表
        /// </summary>
        [HttpGet("user/{userId}/parent/{parentId}")]
        public async Task<ActionResult<List<Notebook>>> GetNotebooksByUserIdAndParentId(Guid userId, Guid? parentId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (userId == default(Guid))
                {
                    return BadRequest(new { Error = "用户ID不能为空" });
                }

                var grain = _clusterClient.GetGrain<INotebookGrain>("list");
                var result = await grain.GetByUserIdAndParentIdAsync(userId, parentId, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据用户ID和父笔记本ID获取笔记本列表时发生系统错误: {UserId}, {ParentId}", userId, parentId);
                return StatusCode(500, new { Error = "系统内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 创建笔记本
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Notebook>> CreateNotebook([FromBody] Notebook notebook, CancellationToken cancellationToken = default)
        {
            try
            {
                if (notebook == null)
                {
                    return BadRequest(new { Error = "请求数据不能为空" });
                }

                var newId = Guid.NewGuid();
                var grain = _clusterClient.GetGrain<INotebookGrain>(newId.ToString());
                var result = await grain.CreateAsync(notebook, cancellationToken);
                if (result != null)
                    return CreatedAtAction(nameof(GetNotebook), new { id = result.Id }, result);
                return BadRequest(new { Error = "创建笔记本失败" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建笔记本时发生系统错误");
                return StatusCode(500, new { Error = "系统内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 更新笔记本
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<Notebook>> UpdateNotebook(Guid id, [FromBody] Notebook notebook, CancellationToken cancellationToken = default)
        {
            try
            {
                if (id == default(Guid))
                {
                    return BadRequest(new { Error = "主键值无效" });
                }

                if (notebook == null)
                {
                    return BadRequest(new { Error = "请求数据不能为空" });
                }

                var grain = _clusterClient.GetGrain<INotebookGrain>(id.ToString());
                var result = await grain.UpdateAsync(notebook, cancellationToken);
                if (result != null)
                    return Ok(result);
                return NotFound(new { Message = "未找到指定的笔记本" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新笔记本时发生系统错误: {Id}", id);
                return StatusCode(500, new { Error = "系统内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 删除笔记本
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotebook(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (id == default(Guid))
                {
                    return BadRequest(new { Error = "主键值无效" });
                }

                var grain = _clusterClient.GetGrain<INotebookGrain>(id.ToString());
                var result = await grain.DeleteAsync(cancellationToken);
                if (result)
                    return NoContent();
                return NotFound(new { Message = "未找到指定的笔记本" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除笔记本时发生系统错误: {Id}", id);
                return StatusCode(500, new { Error = "系统内部错误，请稍后重试" });
            }
        }
    }
}