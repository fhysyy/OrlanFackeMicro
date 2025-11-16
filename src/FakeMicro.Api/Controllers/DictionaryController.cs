using FakeMicro.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FakeMicro.Api.Controllers
{
    /// <summary>
    /// 字典管理控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DictionaryController : ControllerBase
    {
        private readonly ILogger<DictionaryController> _logger;

        public DictionaryController(
            ILogger<DictionaryController> logger)
        {
             _logger = logger;
        }

        #region 字典类型管理

        /// <summary>
        /// 获取字典类型列表
        /// </summary>
        [HttpGet("types")]
        public async Task<IActionResult> GetDictionaryTypes([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
            [FromQuery] string keyword = null, [FromQuery] bool? isEnabled = null)
        {
            try
            {
                _logger.LogInformation("模拟获取字典类型列表，页码：{Page}, 页大小：{PageSize}, 关键词：{Keyword}", page, pageSize, keyword);
                
                // 返回模拟数据
                var mockResult = new {
                    Total = 5,
                    Items = new []{
                        new { Id = "1", Name = "用户状态", Code = "UserStatus", Sort = 1, IsEnabled = true, CreateTime = DateTime.Now },
                        new { Id = "2", Name = "权限类型", Code = "PermissionType", Sort = 2, IsEnabled = true, CreateTime = DateTime.Now },
                        new { Id = "3", Name = "日志级别", Code = "LogLevel", Sort = 3, IsEnabled = true, CreateTime = DateTime.Now }
                    }
                };
                
                return Ok(new { success = true, data = mockResult });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取字典类型列表失败");
                return StatusCode(500, new { success = false, message = "获取字典类型列表失败" });
            }
        }

        /// <summary>
        /// 获取字典类型详情
        /// </summary>
        [HttpGet("types/{id:long}")]
        public async Task<IActionResult> GetDictionaryType(long id)
        {
            try
            {
                _logger.LogInformation("模拟获取字典类型详情，ID: {Id}", id);
                

                // 返回模拟数据
                var mockDictionaryType = new {
                    Id = id,
                    Code = "UserStatus",
                    Name = "用户状态",
                    Description = "用户状态字典",
                    IsEnabled = true,
                    SortOrder = 1,
                    CreatedAt = DateTime.Now.AddDays(-10),
                    UpdatedAt = DateTime.Now.AddDays(-5)
                };
                
                return Ok(new { success = true, data = mockDictionaryType });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取字典类型详情失败: {Id}", id);
                return StatusCode(500, new { success = false, message = "获取字典类型详情失败" });
            }
        }

        /// <summary>
        /// 创建字典类型
        /// </summary>
        [HttpPost("types")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> CreateDictionaryType([FromBody] DictionaryTypeCreateRequest request)
        {
            try
            {
                _logger.LogInformation("模拟创建字典类型，编码: {Code}, 名称: {Name}", request.Code, request.Name);
                
                // 模拟简单的重复检查
                if (request.Code == "DuplicateCode")
                {
                    return BadRequest(new { success = false, message = "字典类型编码已存在" });
                }
                

                // 模拟创建字典类型
                var mockDictionaryType = new {
                    Id = 1001, // 模拟ID
                    Code = request.Code,
                    Name = request.Name,
                    Description = request.Description,
                    IsEnabled = request.IsEnabled,
                    SortOrder = request.SortOrder,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                return CreatedAtAction(nameof(GetDictionaryType), new { id = mockDictionaryType.Id }, 
                    new { success = true, data = mockDictionaryType });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建字典类型失败");
                return StatusCode(500, new { success = false, message = "创建字典类型失败" });
            }
        }

        /// <summary>
        /// 更新字典类型
        /// </summary>
        [HttpPut("types/{id:long}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> UpdateDictionaryType(long id, [FromBody] DictionaryTypeUpdateRequest request)
        {
            try
            {
                _logger.LogInformation("模拟更新字典类型，ID: {Id}, 新编码: {Code}", id, request.Code);
                
                // 模拟简单的重复检查
                if (request.Code == "DuplicateCode")
                {
                    return BadRequest(new { success = false, message = "字典类型编码已存在" });
                }

                // 模拟更新字典类型
                var mockDictionaryType = new {
                    Id = id,
                    Code = request.Code,
                    Name = request.Name,
                    Description = request.Description,
                    IsEnabled = request.IsEnabled,
                    SortOrder = request.SortOrder,
                    CreatedAt = DateTime.Now.AddDays(-10), // 模拟创建时间
                    UpdatedAt = DateTime.Now
                };

                return Ok(new { success = true, data = mockDictionaryType });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新字典类型失败: {Id}", id);
                return StatusCode(500, new { success = false, message = "更新字典类型失败" });
            }
        }

        /// <summary>
        /// 删除字典类型
        /// </summary>
        [HttpDelete("types/{id:long}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> DeleteDictionaryType(long id)
        {
            try
            {
                _logger.LogInformation("模拟删除字典类型，ID: {Id}", id);
                
                // 模拟删除操作
                // 模拟ID为0时不存在的情况
                if (id == 0)
                {
                    return NotFound(new { success = false, message = "字典类型不存在" });
                }
                
                return Ok(new { success = true, message = "删除成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除字典类型失败: {Id}", id);
                return StatusCode(500, new { success = false, message = "删除字典类型失败" });
            }
        }

        #endregion

        #region 字典项管理

        /// <summary>
        /// 根据字典类型获取字典项列表
        /// </summary>
        [HttpGet("items/by-type/{typeId:long}")]
        public async Task<IActionResult> GetDictionaryItemsByTypeId(long typeId)
        {
            try
            {
                _logger.LogInformation("模拟根据字典类型获取字典项列表，类型ID: {TypeId}", typeId);
                
                // 返回模拟数据
                var mockItems = new[]{
                    new {
                        Id = 1,
                        DictionaryTypeId = typeId,
                        Value = "0",
                        Text = "正常",
                        Description = "正常状态",
                        IsEnabled = true,
                        SortOrder = 1,
                        ExtraData = new Dictionary<string, object>() { },
                        CreatedAt = DateTime.Now.AddDays(-10),
                        UpdatedAt = DateTime.Now.AddDays(-5)
                    },
                    new {
                        Id = 2,
                        DictionaryTypeId = typeId,
                        Value = "1",
                        Text = "禁用",
                        Description = "禁用状态",
                        IsEnabled = true,
                        SortOrder = 2,
                        ExtraData = new Dictionary<string, object>() { },
                        CreatedAt = DateTime.Now.AddDays(-10),
                        UpdatedAt = DateTime.Now.AddDays(-5)
                    }
                };
                
                return Ok(new { success = true, data = mockItems });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据字典类型获取字典项列表失败: {TypeId}", typeId);
                return StatusCode(500, new { success = false, message = "获取字典项列表失败" });
            }
        }

        /// <summary>
        /// 根据字典类型编码获取字典项列表
        /// </summary>
        [HttpGet("items/by-code/{code}")]
        public async Task<IActionResult> GetDictionaryItemsByCode(string code)
        {
            try
            {
                _logger.LogInformation("模拟根据字典类型编码获取字典项列表，编码: {Code}", code);
                
                // 根据不同的编码返回不同的模拟数据
                var mockItems = new[]{
                    new {
                        Id = 1,
                        DictionaryTypeId = 1,
                        DictionaryTypeCode = code,
                        Value = "0",
                        Text = "正常",
                        Description = "正常状态",
                        IsEnabled = true,
                        SortOrder = 1,
                        ExtraData = new Dictionary<string, object>() { },
                        CreatedAt = DateTime.Now.AddDays(-10),
                        UpdatedAt = DateTime.Now.AddDays(-5)
                    },
                    new {
                        Id = 2,
                        DictionaryTypeId = 1,
                        DictionaryTypeCode = code,
                        Value = "1",
                        Text = "禁用",
                        Description = "禁用状态",
                        IsEnabled = true,
                        SortOrder = 2,
                        ExtraData = new Dictionary<string, object>() { },
                        CreatedAt = DateTime.Now.AddDays(-10),
                        UpdatedAt = DateTime.Now.AddDays(-5)
                    }
                };
                
                return Ok(new { success = true, data = mockItems });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据字典类型编码获取字典项列表失败: {Code}", code);
                return StatusCode(500, new { success = false, message = "获取字典项列表失败" });
            }
        }

        /// <summary>
        /// 获取字典项详情
        /// </summary>
        [HttpGet("items/{id:long}")]
        public async Task<IActionResult> GetDictionaryItem(long id)
        {
            try
            {
                _logger.LogInformation("模拟获取字典项详情，ID: {Id}", id);
                
                // 返回模拟数据
                var mockItem = new {
                    Id = id,
                    DictionaryTypeId = 1,
                    Value = "0",
                    Text = "正常",
                    Description = "正常状态",
                    IsEnabled = true,
                    SortOrder = 1,
                    ExtraData = new Dictionary<string, object>(),
                    CreatedAt = DateTime.Now.AddDays(-10),
                    UpdatedAt = DateTime.Now.AddDays(-5)
                };
                
                return Ok(new { success = true, data = mockItem });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取字典项详情失败: {Id}", id);
                return StatusCode(500, new { success = false, message = "获取字典项详情失败" });
            }
        }

        /// <summary>
        /// 创建字典项
        /// </summary>
        [HttpPost("items")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> CreateDictionaryItem([FromBody] DictionaryItemCreateRequest request)
        {
            try
            {
                _logger.LogInformation("模拟创建字典项，类型ID: {TypeId}, 值: {Value}, 文本: {Text}", 
                    request.DictionaryTypeId, request.Value, request.Text);
                
                // 模拟简单的验证
                if (request.DictionaryTypeId == 0)
                {
                    return BadRequest(new { success = false, message = "字典类型不存在" });
                }

                // 模拟值重复检查
                if (request.Value == "DuplicateValue")
                {
                    return BadRequest(new { success = false, message = "字典项值已存在" });
                }

                // 模拟创建字典项
                var mockItem = new {
                    Id = 2001, // 模拟ID
                    DictionaryTypeId = request.DictionaryTypeId,
                    Value = request.Value,
                    Text = request.Text,
                    Description = request.Description,
                    IsEnabled = request.IsEnabled,
                    SortOrder = request.SortOrder,
                    ExtraData = request.ExtraData,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                return CreatedAtAction(nameof(GetDictionaryItem), new { id = mockItem.Id }, 
                    new { success = true, data = mockItem });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建字典项失败");
                return StatusCode(500, new { success = false, message = "创建字典项失败" });
            }
        }

        /// <summary>
        /// 更新字典项
        /// </summary>
        [HttpPut("items/{id:long}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> UpdateDictionaryItem(long id, [FromBody] DictionaryItemUpdateRequest request)
        {
            try
            {
                _logger.LogInformation("模拟更新字典项，ID: {Id}, 新值: {Value}, 新文本: {Text}", id, request.Value, request.Text);
                
                // 模拟简单的重复检查
                if (request.Value == "DuplicateValue")
                {
                    return BadRequest(new { success = false, message = "字典项值已存在" });
                }

                // 模拟更新字典项
                var mockItem = new {
                    Id = id,
                    DictionaryTypeId = 1, // 模拟字典类型ID
                    Value = request.Value,
                    Text = request.Text,
                    Description = request.Description,
                    IsEnabled = request.IsEnabled,
                    SortOrder = request.SortOrder,
                    ExtraData = request.ExtraData,
                    CreatedAt = DateTime.Now.AddDays(-10), // 模拟创建时间
                    UpdatedAt = DateTime.Now
                };

                return Ok(new { success = true, data = mockItem });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新字典项失败: {Id}", id);
                return StatusCode(500, new { success = false, message = "更新字典项失败" });
            }
        }

        /// <summary>
        /// 删除字典项
        /// </summary>
        [HttpDelete("items/{id:long}")]
        [Authorize(Roles = "Admin,SystemAdmin")]
        public async Task<IActionResult> DeleteDictionaryItem(long id)
        {
            try
            {
                _logger.LogInformation("模拟删除字典项，ID: {Id}", id);
                
                // 模拟删除操作
                // 模拟ID为0时不存在的情况
                if (id == 0)
                {
                    return NotFound(new { success = false, message = "字典项不存在" });
                }
                
                return Ok(new { success = true, message = "删除成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除字典项失败: {Id}", id);
                return StatusCode(500, new { success = false, message = "删除字典项失败" });
            }
        }

        #endregion

        /// <summary>
        /// 获取当前登录用户ID
        /// </summary>
        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return 0;
        }
    }


}