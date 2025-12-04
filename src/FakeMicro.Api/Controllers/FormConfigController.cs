using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Orleans;
using System;
using System.Threading;
using System.Threading.Tasks;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
using FakeMicro.Entities.Enums;

namespace FakeMicro.Api.Controllers
{
    /// <summary>
    /// 表单配置管理控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FormConfigController : ControllerBase
    {
        private readonly IClusterClient _clusterClient;
        private readonly ILogger<FormConfigController> _logger;

        public FormConfigController(IClusterClient clusterClient, ILogger<FormConfigController> logger)
        {
            _clusterClient = clusterClient ?? throw new ArgumentNullException(nameof(clusterClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 获取表单配置详情
        /// </summary>
        /// <param name="id">表单配置ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表单配置详情</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<FormConfigDto>> GetFormConfig(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { Message = "表单配置ID不能为空" });
                }

                var grain = _clusterClient.GetGrain<IFormConfigGrain>(id);
                var formConfig = await grain.GetAsync(cancellationToken);

                if (formConfig == null)
                {
                    return NotFound(new { Message = "表单配置不存在或已删除" });
                }

                return Ok(formConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取表单配置详情失败: {Id}", id);
                return StatusCode(500, new { Message = "服务器内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 创建表单配置
        /// </summary>
        /// <param name="request">创建请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>创建结果</returns>
        [HttpPost]
        public async Task<ActionResult<FormConfigDto>> CreateFormConfig([FromBody] FormConfigCreateDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { Message = "请求参数不能为空" });
                }

                // 验证模型
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "请求参数验证失败", Errors = ModelState });
                }

                // 创建临时ID用于Grain
                var tempId = Guid.NewGuid().ToString();
                var grain = _clusterClient.GetGrain<IFormConfigGrain>(tempId);
                var result = await grain.CreateAsync(request, cancellationToken);

                return CreatedAtAction(nameof(GetFormConfig), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "创建表单配置参数错误: {Message}", ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建表单配置失败");
                return StatusCode(500, new { Message = "服务器内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 更新表单配置
        /// </summary>
        /// <param name="id">表单配置ID</param>
        /// <param name="request">更新请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新结果</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<FormConfigDto>> UpdateFormConfig(string id, [FromBody] FormConfigUpdateDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { Message = "表单配置ID不能为空" });
                }

                if (request == null)
                {
                    return BadRequest(new { Message = "请求参数不能为空" });
                }

                // 验证模型
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { Message = "请求参数验证失败", Errors = ModelState });
                }

                var grain = _clusterClient.GetGrain<IFormConfigGrain>(id);
                var result = await grain.UpdateAsync(request, cancellationToken);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "更新表单配置参数错误: {Message}", ex.Message);
                if (ex.Message.Contains("不存在"))
                {
                    return NotFound(new { Message = ex.Message });
                }
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新表单配置失败: {Id}", id);
                return StatusCode(500, new { Message = "服务器内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 删除表单配置
        /// </summary>
        /// <param name="id">表单配置ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult<bool>> DeleteFormConfig(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { Message = "表单配置ID不能为空" });
                }

                var grain = _clusterClient.GetGrain<IFormConfigGrain>(id);
                var result = await grain.DeleteAsync(cancellationToken);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除表单配置失败: {Id}", id);
                return StatusCode(500, new { Message = "服务器内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 更新表单配置状态
        /// </summary>
        /// <param name="id">表单配置ID</param>
        /// <param name="status">新状态</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新结果</returns>
        [HttpPut("{id}/status")]
        public async Task<ActionResult<FormConfigDto>> UpdateFormConfigStatus(string id, [FromBody] FormConfigStatus status, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { Message = "表单配置ID不能为空" });
                }

                var grain = _clusterClient.GetGrain<IFormConfigGrain>(id);
                var result = await grain.UpdateStatusAsync(status, cancellationToken);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "更新表单配置状态参数错误: {Message}", ex.Message);
                if (ex.Message.Contains("不存在"))
                {
                    return NotFound(new { Message = ex.Message });
                }
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新表单配置状态失败: {Id}, 状态: {Status}", id, status);
                return StatusCode(500, new { Message = "服务器内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 检查表单编码是否已存在
        /// </summary>
        /// <param name="code">表单编码</param>
        /// <param name="excludeId">排除的ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>检查结果</returns>
        [HttpGet("check-code")]
        public async Task<ActionResult<bool>> CheckCodeExists([FromQuery] string code, [FromQuery] string excludeId = "", CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return BadRequest(new { Message = "表单编码不能为空" });
                }

                // 使用临时Grain进行检查
                var tempId = Guid.NewGuid().ToString();
                var grain = _clusterClient.GetGrain<IFormConfigGrain>(tempId);
                var exists = await grain.CodeExistsAsync(code, excludeId, cancellationToken);

                return Ok(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查表单编码是否存在失败: {Code}", code);
                return StatusCode(500, new { Message = "服务器内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 获取表单配置列表（分页）
        /// </summary>
        /// <param name="query">查询参数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>分页结果</returns>
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<FormConfigDto>>> GetFormConfigs([FromQuery] FormConfigQueryDto query, CancellationToken cancellationToken = default)
        {
            try
            {
                var serviceGrain = _clusterClient.GetGrain<IFormConfigService>(Guid.NewGuid());
                var result = await serviceGrain.GetFormConfigsAsync(query, cancellationToken);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取表单配置列表失败");
                return StatusCode(500, new { Message = "服务器内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 获取所有表单配置列表（用于下拉选择）
        /// </summary>
        /// <param name="status">状态筛选</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表单配置列表</returns>
        [HttpGet("all")]
        public async Task<ActionResult<dynamic>> GetAllFormConfigs([FromQuery] FormConfigStatus? status, CancellationToken cancellationToken = default)
        {
            try
            {
                var serviceGrain = _clusterClient.GetGrain<IFormConfigService>(Guid.NewGuid());
                var result = await serviceGrain.GetAllFormConfigsAsync(status, cancellationToken);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有表单配置列表失败");
                return StatusCode(500, new { Message = "服务器内部错误，请稍后重试" });
            }
        }

        /// <summary>
        /// 根据编码获取表单配置
        /// </summary>
        /// <param name="code">表单编码</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表单配置</returns>
        [HttpGet("by-code/{code}")]
        public async Task<ActionResult<FormConfigDto>> GetFormConfigByCode(string code, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return BadRequest(new { Message = "表单编码不能为空" });
                }

                var serviceGrain = _clusterClient.GetGrain<IFormConfigService>(Guid.NewGuid());
                var formConfig = await serviceGrain.GetByCodeAsync(code, cancellationToken);

                if (formConfig == null)
                {
                    return NotFound(new { Message = "未找到指定编码的表单配置" });
                }

                return Ok(formConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据编码获取表单配置失败: {Code}", code);
                return StatusCode(500, new { Message = "服务器内部错误，请稍后重试" });
            }
        }
    }
}