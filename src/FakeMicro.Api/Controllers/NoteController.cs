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
    /// 笔记控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NoteController : ControllerBase
    {
        private readonly IClusterClient _clusterClient;
        private readonly ILogger<NoteController> _logger;

        public NoteController(IClusterClient clusterClient, ILogger<NoteController> logger)
        {
            _clusterClient = clusterClient;
            _logger = logger;
        }

        /// <summary>
        /// 获取笔记
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Note>> GetNote(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (id == default(Guid))
                {
                    return BadRequest(new { Error = "笔记ID不能为空" });
                }

                var grain = _clusterClient.GetGrain<INoteGrain>(id.ToString());
                var result = await grain.GetAsync(cancellationToken);
                if (result == null)
                    return NotFound(new { Message = "未找到指定的笔记" });
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取笔记时发生系统错误: {Id}", id);
                return StatusCode(500, new { Error = "系统内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 根据用户ID获取笔记列表
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<Note>>> GetNotesByUserId(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (userId == default(Guid))
                {
                    return BadRequest(new { Error = "用户ID不能为空" });
                }

                var grain = _clusterClient.GetGrain<INoteGrain>("list");
                var result = await grain.GetByUserIdAsync(userId, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据用户ID获取笔记列表时发生系统错误: {UserId}", userId);
                return StatusCode(500, new { Error = "系统内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 根据笔记本ID获取笔记列表
        /// </summary>
        [HttpGet("notebook/{notebookId}")]
        public async Task<ActionResult<List<Note>>> GetNotesByNotebookId(Guid notebookId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (notebookId == default(Guid))
                {
                    return BadRequest(new { Error = "笔记本ID不能为空" });
                }

                var grain = _clusterClient.GetGrain<INoteGrain>("list");
                var result = await grain.GetByNotebookIdAsync(notebookId, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据笔记本ID获取笔记列表时发生系统错误: {NotebookId}", notebookId);
                return StatusCode(500, new { Error = "系统内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 根据用户ID和笔记本ID获取笔记列表
        /// </summary>
        [HttpGet("user/{userId}/notebook/{notebookId}")]
        public async Task<ActionResult<List<Note>>> GetNotesByUserIdAndNotebookId(Guid userId, Guid notebookId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (userId == default(Guid))
                {
                    return BadRequest(new { Error = "用户ID不能为空" });
                }

                if (notebookId == default(Guid))
                {
                    return BadRequest(new { Error = "笔记本ID不能为空" });
                }

                var grain = _clusterClient.GetGrain<INoteGrain>("list");
                var result = await grain.GetByUserIdAndNotebookIdAsync(userId, notebookId, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据用户ID和笔记本ID获取笔记列表时发生系统错误: {UserId}, {NotebookId}", userId, notebookId);
                return StatusCode(500, new { Error = "系统内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 根据标签ID获取笔记列表
        /// </summary>
        [HttpGet("tag/{tagId}")]
        public async Task<ActionResult<List<Note>>> GetNotesByTagId(Guid tagId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (tagId == default(Guid))
                {
                    return BadRequest(new { Error = "标签ID不能为空" });
                }

                var grain = _clusterClient.GetGrain<INoteGrain>("list");
                var result = await grain.GetByTagIdAsync(tagId, cancellationToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据标签ID获取笔记列表时发生系统错误: {TagId}", tagId);
                return StatusCode(500, new { Error = "系统内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 创建笔记
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Note>> CreateNote([FromBody] Note note, CancellationToken cancellationToken = default)
        {
            try
            {
                if (note == null)
                {
                    return BadRequest(new { Error = "请求数据不能为空" });
                }

                var newId = Guid.NewGuid();
                var grain = _clusterClient.GetGrain<INoteGrain>(newId.ToString());
                var result = await grain.CreateAsync(note, cancellationToken);
                if (result != null)
                    return CreatedAtAction(nameof(GetNote), new { id = result.Id }, result);
                return BadRequest(new { Error = "创建笔记失败" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建笔记时发生系统错误");
                return StatusCode(500, new { Error = "系统内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 更新笔记
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<Note>> UpdateNote(Guid id, [FromBody] Note note, CancellationToken cancellationToken = default)
        {
            try
            {
                if (id == default(Guid))
                {
                    return BadRequest(new { Error = "主键值无效" });
                }

                if (note == null)
                {
                    return BadRequest(new { Error = "请求数据不能为空" });
                }

                var grain = _clusterClient.GetGrain<INoteGrain>(id.ToString());
                var result = await grain.UpdateAsync(note, cancellationToken);
                if (result != null)
                    return Ok(result);
                return NotFound(new { Message = "未找到指定的笔记" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新笔记时发生系统错误: {Id}", id);
                return StatusCode(500, new { Error = "系统内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 删除笔记
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNote(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (id == default(Guid))
                {
                    return BadRequest(new { Error = "主键值无效" });
                }

                var grain = _clusterClient.GetGrain<INoteGrain>(id.ToString());
                var result = await grain.DeleteAsync(cancellationToken);
                if (result)
                    return NoContent();
                return NotFound(new { Message = "未找到指定的笔记" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除笔记时发生系统错误: {Id}", id);
                return StatusCode(500, new { Error = "系统内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 软删除笔记
        /// </summary>
        [HttpDelete("soft/{id}")]
        public async Task<IActionResult> SoftDeleteNote(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (id == default(Guid))
                {
                    return BadRequest(new { Error = "主键值无效" });
                }

                var grain = _clusterClient.GetGrain<INoteGrain>(id.ToString());
                var result = await grain.SoftDeleteAsync(cancellationToken);
                if (result)
                    return NoContent();
                return NotFound(new { Message = "未找到指定的笔记" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "软删除笔记时发生系统错误: {Id}", id);
                return StatusCode(500, new { Error = "系统内部错误，请稍后重试" });
            }
        }
    }
}