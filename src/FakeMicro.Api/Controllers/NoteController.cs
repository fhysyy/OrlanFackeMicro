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
        public async Task<ActionResult<BaseResultModel<Note>>> GetNote(Guid id, CancellationToken cancellationToken = default)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            try
            {
                if (id == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 获取笔记请求参数无效，ID不能为空", traceId);
                    return BadRequest(BaseResultModel<bool>.FailedResult(false, "笔记ID不能为空", 404));
                }

                _logger.LogInformation("[TraceId: {TraceId}] 开始获取笔记，ID: {Id}", traceId, id);
                var grain = _clusterClient.GetGrain<INoteGrain>(id.ToString());
                var result = await grain.GetAsync(cancellationToken);
                
                if (result == null)
                {
                    _logger.LogInformation("[TraceId: {TraceId}] 未找到指定的笔记，ID: {Id}", traceId, id);
                    return NotFound(BaseResultModel<bool>.FailedResult(false, "未找到指定的笔记",404));
                }
                
                _logger.LogInformation("[TraceId: {TraceId}] 获取笔记成功，ID: {Id}", traceId, id);
                return Ok(BaseResultModel<Note>.SuccessResult(result, "获取笔记成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId: {TraceId}] 获取笔记时发生系统错误: {Id}", traceId, id);
                return StatusCode(500, BaseResultModel<bool>.FailedResult(false, "系统内部错误，请稍后重试", 404));
            }
        }

        /// <summary>
        /// 根据用户ID获取笔记列表
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<BaseResultModel<List<Note>>>> GetNotesByUserId(Guid userId, CancellationToken cancellationToken = default)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            try
            {
                if (userId == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 获取笔记列表请求参数无效，用户ID不能为空", traceId);
                    return BadRequest(BaseResultModel<List<Note>>.FailedResult(null, "用户ID不能为空", 404));
                }

                _logger.LogInformation("[TraceId: {TraceId}] 开始根据用户ID获取笔记列表，用户ID: {UserId}", traceId, userId);
                var grain = _clusterClient.GetGrain<INoteGrain>("list");
                var result = await grain.GetByUserIdAsync(userId, cancellationToken);
                
                _logger.LogInformation("[TraceId: {TraceId}] 根据用户ID获取笔记列表成功，用户ID: {UserId}，数量: {Count}", traceId, userId, result.Count);
                return Ok(BaseResultModel<List<Note>>.SuccessResult(result, "获取笔记列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId: {TraceId}] 根据用户ID获取笔记列表时发生系统错误: {UserId}", traceId, userId);
                return StatusCode(500, BaseResultModel<List<Note>>.FailedResult(null, "系统内部错误，请稍后重试", 500));
            }
        }

        /// <summary>
        /// 根据笔记本ID获取笔记列表
        /// </summary>
        [HttpGet("notebook/{notebookId}")]
        public async Task<ActionResult<BaseResultModel<List<Note>>>> GetNotesByNotebookId(Guid notebookId, CancellationToken cancellationToken = default)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            try
            {
                if (notebookId == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 获取笔记列表请求参数无效，笔记本ID不能为空", traceId);
                    return BadRequest(BaseResultModel<List<Note>>.FailedResult(null, "笔记本ID不能为空", 404));
                }

                _logger.LogInformation("[TraceId: {TraceId}] 开始根据笔记本ID获取笔记列表，笔记本ID: {NotebookId}", traceId, notebookId);
                var grain = _clusterClient.GetGrain<INoteGrain>("list");
                var result = await grain.GetByNotebookIdAsync(notebookId, cancellationToken);
                
                _logger.LogInformation("[TraceId: {TraceId}] 根据笔记本ID获取笔记列表成功，笔记本ID: {NotebookId}，数量: {Count}", traceId, notebookId, result.Count);
                return Ok(BaseResultModel<List<Note>>.SuccessResult(result, "获取笔记列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId: {TraceId}] 根据笔记本ID获取笔记列表时发生系统错误: {NotebookId}", traceId, notebookId);
                return StatusCode(500, BaseResultModel<List<Note>>.FailedResult(null, "系统内部错误，请稍后重试",500));
            }
        }

        /// <summary>
        /// 根据用户ID和笔记本ID获取笔记列表
        /// </summary>
        [HttpGet("user/{userId}/notebook/{notebookId}")]
        public async Task<ActionResult<BaseResultModel<List<Note>>>> GetNotesByUserIdAndNotebookId(Guid userId, Guid notebookId, CancellationToken cancellationToken = default)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            try
            {
                if (userId == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 获取笔记列表请求参数无效，用户ID不能为空", traceId);
                    return BadRequest(BaseResultModel<List<Note>>.FailedResult(null, "用户ID不能为空",404));
                }

                if (notebookId == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 获取笔记列表请求参数无效，笔记本ID不能为空", traceId);
                    return BadRequest(BaseResultModel<List<Note>>.FailedResult(null, "笔记本ID不能为空", 400));
                }

                _logger.LogInformation("[TraceId: {TraceId}] 开始根据用户ID和笔记本ID获取笔记列表，用户ID: {UserId}, 笔记本ID: {NotebookId}", traceId, userId, notebookId);
                var grain = _clusterClient.GetGrain<INoteGrain>("list");
                var result = await grain.GetByUserIdAndNotebookIdAsync(userId, notebookId, cancellationToken);
                
                _logger.LogInformation("[TraceId: {TraceId}] 根据用户ID和笔记本ID获取笔记列表成功，用户ID: {UserId}, 笔记本ID: {NotebookId}，数量: {Count}", traceId, userId, notebookId, result.Count);
                return Ok(BaseResultModel<List<Note>>.SuccessResult(result, "获取笔记列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId: {TraceId}] 根据用户ID和笔记本ID获取笔记列表时发生系统错误: {UserId}, {NotebookId}", traceId, userId, notebookId);
                return StatusCode(500, BaseResultModel<List<Note>>.FailedResult(null, "系统内部错误，请稍后重试"));
            }
        }

        /// <summary>
        /// 根据标签ID获取笔记列表
        /// </summary>
        [HttpGet("tag/{tagId}")]
        public async Task<ActionResult<BaseResultModel<List<Note>>>> GetNotesByTagId(Guid tagId, CancellationToken cancellationToken = default)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            try
            {
                if (tagId == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 获取笔记列表请求参数无效，标签ID不能为空", traceId);
                    return BadRequest(BaseResultModel<List<Note>>.FailedResult(null, "标签ID不能为空"));
                }

                _logger.LogInformation("[TraceId: {TraceId}] 开始根据标签ID获取笔记列表，标签ID: {TagId}", traceId, tagId);
                var grain = _clusterClient.GetGrain<INoteGrain>("list");
                var result = await grain.GetByTagIdAsync(tagId, cancellationToken);
                
                _logger.LogInformation("[TraceId: {TraceId}] 根据标签ID获取笔记列表成功，标签ID: {TagId}，数量: {Count}", traceId, tagId, result.Count);
                return Ok(BaseResultModel<List<Note>>.SuccessResult(result, "获取笔记列表成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId: {TraceId}] 根据标签ID获取笔记列表时发生系统错误: {TagId}", traceId, tagId);
                return StatusCode(500, BaseResultModel<List<Note>>.FailedResult(null, "系统内部错误，请稍后重试"));
            }
        }

        /// <summary>
        /// 创建笔记
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BaseResultModel<Note>>> CreateNote([FromBody] Note note, CancellationToken cancellationToken = default)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            try
            {
                if (note == null)
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 创建笔记请求参数无效，请求数据不能为空", traceId);
                    return BadRequest(BaseResultModel.FailedResult("400", "请求数据不能为空"));
                }

                // 验证必填字段
                if (string.IsNullOrWhiteSpace(note.Title))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 创建笔记请求参数无效，笔记标题不能为空", traceId);
                    return BadRequest(BaseResultModel.FailedResult("400", "笔记标题不能为空"));
                }

                if (note.UserId == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 创建笔记请求参数无效，用户ID不能为空", traceId);
                    return BadRequest(BaseResultModel.FailedResult("400", "用户ID不能为空"));
                }

                if (note.NotebookId == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 创建笔记请求参数无效，笔记本ID不能为空", traceId);
                    return BadRequest(BaseResultModel.FailedResult("400", "笔记本ID不能为空"));
                }

                _logger.LogInformation("[TraceId: {TraceId}] 开始创建笔记，用户ID: {UserId}, 笔记本ID: {NotebookId}, 标题: {Title}", traceId, note.UserId, note.NotebookId, note.Title);
                var newId = Guid.NewGuid();
                note.Id = newId;
                note.CreatedAt = DateTime.UtcNow;
                note.UpdatedAt = DateTime.UtcNow;
                
                var grain = _clusterClient.GetGrain<INoteGrain>(newId.ToString());
                var result = await grain.CreateAsync(note, cancellationToken);
                
                if (result != null)
                {
                    _logger.LogInformation("[TraceId: {TraceId}] 创建笔记成功，ID: {Id}", traceId, result.Id);
                    return CreatedAtAction(nameof(GetNote), new { id = result.Id }, BaseResultModel.SuccessResult(result, "创建笔记成功"));
                }
                
                _logger.LogWarning("[TraceId: {TraceId}] 创建笔记失败，用户ID: {UserId}, 笔记本ID: {NotebookId}, 标题: {Title}", traceId, note.UserId, note.NotebookId, note.Title);
                return BadRequest(BaseResultModel.FailedResult("400", "创建笔记失败"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId: {TraceId}] 创建笔记时发生系统错误");
                return StatusCode(500, BaseResultModel.FailedResult(null, "系统内部错误，请稍后重试"));
            }
        }

        /// <summary>
        /// 更新笔记
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<BaseResultModel>> UpdateNote(Guid id, [FromBody] Note note, CancellationToken cancellationToken = default)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            try
            {
                if (id == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 更新笔记请求参数无效，主键值无效", traceId);
                    return BadRequest(BaseResultModel.FailedResult("400", "主键值无效"));
                }

                if (note == null)
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 更新笔记请求参数无效，请求数据不能为空", traceId);
                    return BadRequest(BaseResultModel.FailedResult("400", "请求数据不能为空"));
                }

                // 确保ID一致
                if (note.Id != id)
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 更新笔记请求参数无效，URL中的ID与请求体中的ID不一致", traceId);
                    return BadRequest(BaseResultModel.FailedResult("400", "URL中的ID与请求体中的ID不一致"));
                }

                // 验证必填字段
                if (string.IsNullOrWhiteSpace(note.Title))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 更新笔记请求参数无效，笔记标题不能为空", traceId);
                    return BadRequest(BaseResultModel.FailedResult("400", "笔记标题不能为空"));
                }

                if (note.UserId == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 更新笔记请求参数无效，用户ID不能为空", traceId);
                    return BadRequest(BaseResultModel.FailedResult("400", "用户ID不能为空"));
                }

                if (note.NotebookId == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 更新笔记请求参数无效，笔记本ID不能为空", traceId);
                    return BadRequest(BaseResultModel.FailedResult("400", "笔记本ID不能为空"));
                }

                _logger.LogInformation("[TraceId: {TraceId}] 开始更新笔记，ID: {Id}", traceId, id);
                note.UpdatedAt = DateTime.UtcNow;
                
                var grain = _clusterClient.GetGrain<INoteGrain>(id.ToString());
                var result = await grain.UpdateAsync(note, cancellationToken);
                
                if (result != null)
                {
                    _logger.LogInformation("[TraceId: {TraceId}] 更新笔记成功，ID: {Id}", traceId, id);
                    return Ok(BaseResultModel.SuccessResult(result, "更新笔记成功"));
                }
                
                _logger.LogInformation("[TraceId: {TraceId}] 未找到指定的笔记，ID: {Id}", traceId, id);
                return NotFound(BaseResultModel.FailedResult("404", "未找到指定的笔记"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId: {TraceId}] 更新笔记时发生系统错误: {Id}", traceId, id);
                return StatusCode(500, BaseResultModel.FailedResult("500", "系统内部错误，请稍后重试"));
            }
        }

        /// <summary>
        /// 删除笔记
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<BaseResultModel>> DeleteNote(Guid id, CancellationToken cancellationToken = default)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            try
            {
                if (id == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 删除笔记请求参数无效，主键值无效", traceId);
                    return BadRequest(BaseResultModel.FailedResult("400", "主键值无效"));
                }

                _logger.LogInformation("[TraceId: {TraceId}] 开始删除笔记，ID: {Id}", traceId, id);
                var grain = _clusterClient.GetGrain<INoteGrain>(id.ToString());
                var result = await grain.DeleteAsync(cancellationToken);
                
                if (result)
                {
                    _logger.LogInformation("[TraceId: {TraceId}] 删除笔记成功，ID: {Id}", traceId, id);
                    return Ok(BaseResultModel.SuccessResult(true, "删除笔记成功"));
                }
                
                _logger.LogInformation("[TraceId: {TraceId}] 未找到指定的笔记，ID: {Id}", traceId, id);
                return NotFound(BaseResultModel.FailedResult("404", "未找到指定的笔记"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId: {TraceId}] 删除笔记时发生系统错误: {Id}", traceId, id);
                return StatusCode(500, BaseResultModel.FailedResult("500", "系统内部错误，请稍后重试"));
            }
        }

        /// <summary>
        /// 软删除笔记
        /// </summary>
        [HttpDelete("soft/{id}")]
        public async Task<ActionResult<BaseResultModel>> SoftDeleteNote(Guid id, CancellationToken cancellationToken = default)
        {
            var traceId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
            try
            {
                if (id == default(Guid))
                {
                    _logger.LogWarning("[TraceId: {TraceId}] 软删除笔记请求参数无效，主键值无效", traceId);
                    return BadRequest(BaseResultModel<bool>.FailedResult(false, "主键值无效", 400));
                }

                _logger.LogInformation("[TraceId: {TraceId}] 开始软删除笔记，ID: {Id}", traceId, id);
                var grain = _clusterClient.GetGrain<INoteGrain>(id.ToString());
                var result = await grain.SoftDeleteAsync(cancellationToken);
                
                if (result)
                {
                    _logger.LogInformation("[TraceId: {TraceId}] 软删除笔记成功，ID: {Id}", traceId, id);
                    return Ok(BaseResultModel<bool>.SuccessResult(true, "软删除笔记成功"));
                }
                
                _logger.LogInformation("[TraceId: {TraceId}] 未找到指定的笔记，ID: {Id}", traceId, id);
                return NotFound(BaseResultModel<bool>.FailedResult(false, "未找到指定的笔记", 404));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TraceId: {TraceId}] 软删除笔记时发生系统错误: {Id}", traceId, id);
                return StatusCode(500, BaseResultModel<bool>.FailedResult(false, "系统内部错误，请稍后重试", 500));
            }
        }
    }
}