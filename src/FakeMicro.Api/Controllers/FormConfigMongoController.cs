using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using FakeMicro.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using FakeMicro.DatabaseAccess;
using FakeMicro.DatabaseAccess.Interfaces;
using System;
using System.Linq.Expressions;
using MongoDB.Bson;

namespace FakeMicro.Api.Controllers
{
    /// <summary>
    /// 表单配置管理控制器（MongoDB版本）
    /// 使用MongoDB进行数据存储
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class FormConfigMongoController : ControllerBase
    {
        private readonly IMongoRepository<FormConfig, string> _formConfigRepository;
        private readonly ILogger<FormConfigMongoController> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="formConfigRepository">表单配置MongoDB仓储</param>
        /// <param name="logger">日志记录器</param>
        public FormConfigMongoController(
            IMongoRepository<FormConfig, string> formConfigRepository,
            ILogger<FormConfigMongoController> logger)
        {
            _formConfigRepository = formConfigRepository;
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
                var formConfig = await _formConfigRepository.GetByIdAsync(id, cancellationToken);
                
                if (formConfig == null)
                {
                    _logger.LogWarning("未找到表单配置，ID: {Id}", id);
                    return NotFound($"表单配置不存在，ID: {id}");
                }
                
                _logger.LogInformation("成功获取表单配置，ID: {Id}, 编码: {Code}", id, formConfig.code);
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
        /// <param name="formConfig">表单配置信息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>创建结果</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FormConfig formConfig, CancellationToken cancellationToken = default)
        {
            try
            {
                // 验证输入
                if (formConfig == null)
                {
                    return BadRequest("表单配置信息不能为空");
                }

                if (string.IsNullOrWhiteSpace(formConfig.code))
                {
                    return BadRequest("表单编码不能为空");
                }

                // 检查编码是否已存在
                var exists = await _formConfigRepository.ExistsAsync(f => f.code == formConfig.code, cancellationToken);
                if (exists)
                {
                    return Conflict($"表单编码已存在: {formConfig.code}");
                }

                // 设置默认值
                if (string.IsNullOrEmpty(formConfig.id))
                {
                    formConfig.id = ObjectId.GenerateNewId().ToString();
                }
                
                formConfig.created_at = DateTime.UtcNow;
                formConfig.updated_at = DateTime.UtcNow;
                formConfig.is_deleted = false;

                _logger.LogInformation("正在创建表单配置，编码: {Code}", formConfig.code);
                await _formConfigRepository.AddAsync(formConfig, cancellationToken);
                _logger.LogInformation("表单配置创建成功，ID: {Id}, 编码: {Code}", formConfig.id, formConfig.code);

                return CreatedAtAction(nameof(GetById), new { id = formConfig.id }, formConfig);
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
        /// <param name="formConfig">表单配置信息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新结果</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] FormConfig formConfig, CancellationToken cancellationToken = default)
        {
            try
            {
                // 验证输入
                if (formConfig == null)
                {
                    return BadRequest("表单配置信息不能为空");
                }

                if (string.IsNullOrWhiteSpace(formConfig.code))
                {
                    return BadRequest("表单编码不能为空");
                }

                // 检查配置是否存在
                var existingConfig = await _formConfigRepository.GetByIdAsync(id, cancellationToken);
                if (existingConfig == null)
                {
                    return NotFound($"表单配置不存在，ID: {id}");
                }

                // 检查编码是否被其他配置使用
                if (existingConfig.code != formConfig.code)
                {
                    var codeExists = await _formConfigRepository.ExistsAsync(f => f.code == formConfig.code && f.id != id, cancellationToken);
                    if (codeExists)
                    {
                        return Conflict($"表单编码已存在: {formConfig.code}");
                    }
                }

                // 更新配置
               // formConfig.id = id;
                formConfig.created_at = existingConfig.created_at;
                formConfig.updated_at = DateTime.UtcNow;
                formConfig.is_deleted = existingConfig.is_deleted;

                _logger.LogInformation("正在更新表单配置，ID: {Id}, 编码: {Code}", id, formConfig.code);
                await _formConfigRepository.UpdateAsync(formConfig, cancellationToken);
                _logger.LogInformation("表单配置更新成功，ID: {Id}, 编码: {Code}", id, formConfig.code);

                return Ok(formConfig);
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
                // 检查配置是否存在
                var formConfig = await _formConfigRepository.GetByIdAsync(id, cancellationToken);
                if (formConfig == null)
                {
                    return NotFound($"表单配置不存在，ID: {id}");
                }

                // 软删除
                formConfig.is_deleted = true;
                formConfig.updated_at = DateTime.UtcNow;

                _logger.LogInformation("正在删除表单配置，ID: {Id}, 编码: {Code}", id, formConfig.code);
                await _formConfigRepository.UpdateAsync(formConfig, cancellationToken);
                _logger.LogInformation("表单配置删除成功，ID: {Id}, 编码: {Code}", id, formConfig.code);

                return Ok(new { message = "表单配置删除成功" });
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
                // 构建查询条件
                var predicate = PredicateBuilder.True<FormConfig>();
                predicate = predicate.And(f => !f.is_deleted); // 排除已删除的记录

                if (!string.IsNullOrEmpty(code))
                {
                    predicate = predicate.And(f => f.code.Contains(code));
                }

                if (!string.IsNullOrEmpty(name))
                {
                    predicate = predicate.And(f => f.name.Contains(name));
                }

                _logger.LogInformation("正在分页查询表单配置，页码: {PageNumber}, 每页大小: {PageSize}", pageNumber, pageSize);
                var result = await _formConfigRepository.GetPagedByConditionAsync(
                    predicate,
                    pageNumber,
                    pageSize,
                    f => f.created_at,
                    true, // 按创建时间倒序
                    cancellationToken);

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
                var formConfigs = await _formConfigRepository.GetByConditionAsync(
                    f => !f.is_deleted, // 排除已删除的记录
                    cancellationToken);

                _logger.LogInformation("成功获取所有表单配置，数量: {Count}", formConfigs.Count());
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
                var formConfigs = await _formConfigRepository.GetByConditionAsync(
                    f => !f.is_deleted && f.code == code,
                    cancellationToken);
                
                var formConfig = formConfigs.FirstOrDefault();
                
                if (formConfig == null)
                {
                    _logger.LogWarning("未找到表单配置，编码: {Code}", code);
                    return NotFound($"表单配置不存在，编码: {code}");
                }
                
                _logger.LogInformation("成功获取表单配置，ID: {Id}, 编码: {Code}", formConfig.id, formConfig.code);
                return Ok(formConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据编码查询表单配置失败，编码: {Code}", code);
                return StatusCode(500, "根据编码查询表单配置失败: " + ex.Message);
            }
        }
    }

    /// <summary>
    /// 表达式构建辅助类
    /// </summary>
    public static class PredicateBuilder
    {
        public static Expression<Func<T, bool>> True<T>() { return f => true; }
        public static Expression<Func<T, bool>> False<T>() { return f => false; }

        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1,
                                                            Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>(
                  Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
        }

        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1,
                                                             Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>(
                  Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
        }
    }
}