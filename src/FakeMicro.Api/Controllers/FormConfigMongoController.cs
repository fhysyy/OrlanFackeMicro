using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FakeMicro.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System;
using Orleans;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;

namespace FakeMicro.Api.Controllers
{
    /// <summary>
    /// 表单配置管理控制器（MongoDB版本）
    /// 使用Orleans Grain进行数据访问
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class FormConfigMongoController : ControllerBase
    {
        private readonly IClusterClient _clusterClient;
        private readonly ILogger<FormConfigMongoController> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="clusterClient">Orleans集群客户端</param>
        /// <param name="logger">日志记录器</param>
        public FormConfigMongoController(
            IClusterClient clusterClient,
            ILogger<FormConfigMongoController> logger)
        {
            _clusterClient = clusterClient;
            _logger = logger;
        }

        /// <summary>
        /// 获取表单配置详情
        /// </summary>
        /// <param name="id">配置ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表单配置信息</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("正在获取表单配置，ID: {Id}", id);
                var grain = _clusterClient.GetGrain<IFormConfigGrain>(id);
                var formConfig = await grain.GetAsync(cancellationToken);
                
                if (formConfig == null)
                {
                    _logger.LogWarning("未找到表单配置，ID: {Id}", id);
                    return NotFound($"表单配置不存在，ID: {id}");
                }
                
                _logger.LogInformation("成功获取表单配置，ID: {Id}, 编码: {Code}", id, formConfig.Code);
                return Ok(formConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取表单配置失败，ID: {Id}", id);
                return StatusCode(500, "获取表单配置失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 创建表单配置
        /// </summary>
        /// <param name="formConfigDto">表单配置信息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>创建结果</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FormConfigCreateDto formConfigDto, CancellationToken cancellationToken = default)
        {
            try
            {
                // 验证输入
                if (formConfigDto == null)
                {
                    return BadRequest("表单配置信息不能为空");
                }

                if (string.IsNullOrWhiteSpace(formConfigDto.Code))
                {
                    return BadRequest("表单编码不能为空");
                }

                _logger.LogInformation("正在创建表单配置，编码: {Code}", formConfigDto.Code);
                var grain = _clusterClient.GetGrain<IFormConfigGrain>(Guid.NewGuid().ToString());
                var result = await grain.CreateAsync(formConfigDto, cancellationToken);
                _logger.LogInformation("表单配置创建成功，ID: {Id}, 编码: {Code}", result.Id, result.Code);

                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建表单配置失败");
                return StatusCode(500, "创建表单配置失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 更新表单配置
        /// </summary>
        /// <param name="id">配置ID</param>
        /// <param name="formConfigDto">表单配置信息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新结果</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] FormConfigUpdateDto formConfigDto, CancellationToken cancellationToken = default)
        {
            try
            {
                // 验证输入
                if (formConfigDto == null)
                {
                    return BadRequest("表单配置信息不能为空");
                }

                if (string.IsNullOrWhiteSpace(formConfigDto.Code))
                {
                    return BadRequest("表单编码不能为空");
                }

                _logger.LogInformation("正在更新表单配置，ID: {Id}, 编码: {Code}", id, formConfigDto.Code);
                var grain = _clusterClient.GetGrain<IFormConfigGrain>(id);
                var result = await grain.UpdateAsync(formConfigDto, cancellationToken);
                _logger.LogInformation("表单配置更新成功，ID: {Id}, 编码: {Code}", id, result.Code);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新表单配置失败，ID: {Id}", id);
                return StatusCode(500, "更新表单配置失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 删除表单配置
        /// </summary>
        /// <param name="id">配置ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("正在删除表单配置，ID: {Id}", id);
                var grain = _clusterClient.GetGrain<IFormConfigGrain>(id);
                var result = await grain.DeleteAsync(cancellationToken);
                _logger.LogInformation("表单配置删除成功，ID: {Id}", id);

                return Ok(new { message = "表单配置删除成功", success = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除表单配置失败，ID: {Id}", id);
                return StatusCode(500, "删除表单配置失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 分页查询表单配置
        /// </summary>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="code">编码（可选）</param>
        /// <param name="name">名称（可选）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>分页结果</returns>
        [HttpGet("pagination")]
        public async Task<IActionResult> GetPaged(int pageNumber = 1, int pageSize = 10,
            string? code = null, string? name = null, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("正在分页查询表单配置，页码: {PageNumber}, 每页大小: {PageSize}", pageNumber, pageSize);
                var service = _clusterClient.GetGrain<IFormConfigService>(Guid.NewGuid());
                var query = new FormConfigQueryDto
                {
                    PageIndex = pageNumber,
                    PageSize = pageSize,
                    Code = code,
                    Name = name
                };
                var result = await service.GetFormConfigsAsync(query, cancellationToken);

                _logger.LogInformation("分页查询成功，总记录数: {TotalCount}, 页码: {PageNumber}", result.TotalCount, pageNumber);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询表单配置失败");
                return StatusCode(500, "分页查询表单配置失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 获取所有表单配置（不分页）
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表单配置列表</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("正在获取所有表单配置");
                var service = _clusterClient.GetGrain<IFormConfigService>(Guid.NewGuid());
                var formConfigs = await service.GetAllFormConfigsAsync(cancellationToken: cancellationToken);

                _logger.LogInformation("成功获取所有表单配置，数量: {Count}", formConfigs.Count);
                return Ok(formConfigs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有表单配置失败");
                return StatusCode(500, "获取所有表单配置失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 根据编码查询表单配置
        /// </summary>
        /// <param name="code">表单编码</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表单配置信息</returns>
        [HttpGet("by-code/{code}")]
        public async Task<IActionResult> GetByCode(string code, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("正在根据编码查询表单配置，编码: {Code}", code);
                var service = _clusterClient.GetGrain<IFormConfigService>(Guid.NewGuid());
                var formConfig = await service.GetByCodeAsync(code, cancellationToken);
                
                if (formConfig == null)
                {
                    _logger.LogWarning("未找到表单配置，编码: {Code}", code);
                    return NotFound($"表单配置不存在，编码: {code}");
                }
                
                _logger.LogInformation("成功获取表单配置，ID: {Id}, 编码: {Code}", formConfig.Id, formConfig.Code);
                return Ok(formConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据编码查询表单配置失败，编码: {Code}", code);
                return StatusCode(500, "根据编码查询表单配置失败: " + ex.Message);
            }
        }
    }
}