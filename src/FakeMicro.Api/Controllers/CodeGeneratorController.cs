using FakeMicro.Utilities.CodeGenerator;
using FakeMicro.Utilities.CodeGenerator.Entities;
using FakeMicro.Utilities.CodeGenerator.Requests;
using FakeMicro.Utilities.CodeGenerator.Templates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FakeMicro.Api.Controllers
{
    /// <summary>
    /// 代码生成器API控制器
    /// 提供代码生成的REST API接口，支持Orleans Grain接口、DTO、控制器等代码生成
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CodeGeneratorController : ControllerBase
    {
        private readonly ICodeGenerator _codeGenerator;
        private readonly ICodeGeneratorValidator _validator;
        private readonly ILogger<CodeGeneratorController> _logger;

        public CodeGeneratorController(
            ICodeGenerator codeGenerator,
            ICodeGeneratorValidator validator,
            ILogger<CodeGeneratorController> logger)
        {
            _codeGenerator = codeGenerator;
            _validator = validator;
            _logger = logger;
        }

        /// <summary>
        /// 获取所有可用的实体类型
        /// </summary>
        /// <returns>实体类型列表</returns>
        [HttpGet("entities")]
        public async Task<IActionResult> GetAvailableEntities()
        {
            try
            {
                var entities = await _codeGenerator.GetAvailableEntityTypesAsync();
                return Ok(new 
                { 
                    success = true, 
                    data = entities,
                    message = "获取实体类型列表成功" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取实体类型列表时发生错误");
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = $"获取实体类型列表失败: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// 生成代码
        /// </summary>
        /// <param name="request">代码生成请求</param>
        /// <returns>生成结果</returns>
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateCode([FromBody] CodeGenerationRequest request)
        {
            try
            {
                // 验证请求
                var validationResult = await _validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new 
                    { 
                        success = false, 
                        message = "请求验证失败",
                        errors = validationResult.Errors 
                    });
                }

                // 创建实体元数据
                var entityMetadata = CreateEntityMetadata(request);
                
                // 生成代码
                var result = await _codeGenerator.GenerateCodeAsync(
                    new List<EntityMetadata> { entityMetadata },
                    request.GenerationType,
                    request.OverwriteStrategy,
                    request.OutputPath);

                if (result.IsSuccess)
                {
                    return Ok(new 
                    { 
                        success = true, 
                      //  message = result.Message ?? "代码生成成功",
                        data = new 
                        {
                            entityName = entityMetadata.EntityName,
                            generationType = request.GenerationType.ToString(),
                            outputPath = result.OutputPath ?? request.OutputPath ?? "默认路径"
                        }
                    });
                }
                else
                {
                    return StatusCode(500, new 
                    { 
                        success = false, 
                        message = result.ErrorMessage ?? "代码生成失败",
                        errorType = result.ErrorType.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成代码时发生错误");
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = $"生成代码时发生错误: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// 预览生成的代码
        /// </summary>
        /// <param name="request">代码生成请求</param>
        /// <returns>预览结果</returns>
        [HttpPost("preview")]
        public async Task<IActionResult> PreviewCode([FromBody] CodeGenerationRequest request)
        {
            try
            {
                // 验证请求
                var validationResult = await _validator.ValidateAsync(request);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new 
                    { 
                        success = false, 
                        message = "请求验证失败",
                        errors = validationResult.Errors 
                    });
                }

                // 创建实体元数据
                var entityMetadata = CreateEntityMetadata(request);
                
                // 预览代码
                var preview = await _codeGenerator.PreviewCodeAsync(entityMetadata, request.GenerationType);

                return Ok(new 
                { 
                    success = true, 
                    message = "代码预览成功",
                    data = preview 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "预览代码时发生错误");
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = $"预览代码时发生错误: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// 批量生成代码
        /// </summary>
        /// <param name="request">批量代码生成请求</param>
        /// <returns>生成结果</returns>
        [HttpPost("generate-batch")]
        public async Task<IActionResult> GenerateCodeBatch([FromBody] BatchCodeGenerationRequest request)
        {
            try
            {
                var results = new List<object>();
                
                foreach (var entityRequest in request.Entities)
                {
                    try
                    {
                        // 创建实体元数据
                        var entityMetadata = CreateEntityMetadata(entityRequest);
                        
                        // 生成代码
                        var result = await _codeGenerator.GenerateCodeAsync(
                            new List<EntityMetadata> { entityMetadata },
                            entityRequest.GenerationType,
                            entityRequest.OverwriteStrategy,
                            entityRequest.OutputPath);

                        results.Add(new
                        {
                            entityName = entityMetadata.EntityName,
                            success = result.IsSuccess,
                            message = result.IsSuccess ? "生成成功" : result.ErrorMessage
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new
                        {
                            entityName = entityRequest.EntityName,
                            success = false,
                            message = $"生成失败: {ex.Message}"
                        });
                    }
                }

                return Ok(new 
                { 
                    success = true, 
                    message = "批量代码生成完成",
                    data = results 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量生成代码时发生错误");
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = $"批量生成代码时发生错误: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// 创建实体元数据
        /// </summary>
        /// <param name="request">代码生成请求</param>
        /// <returns>实体元数据</returns>
        private EntityMetadata CreateEntityMetadata(CodeGenerationRequest request)
        {
            var properties = request.Properties?.Select(p => new PropertyMetadata
            {
                Name = p.Name,
                Type = p.Type,
                IsNullable = p.IsNullable,
                IsPrimaryKey = p.IsPrimaryKey,
                MaxLength = p.MaxLength,
                IsRequired = p.IsRequired,
                Description = p.Description
            }).ToList() ?? new List<PropertyMetadata>();

            return _codeGenerator.CreateEntityMetadata(request.EntityName, properties);
        }
    }
}