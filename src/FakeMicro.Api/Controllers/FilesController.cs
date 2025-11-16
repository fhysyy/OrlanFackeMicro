using FakeMicro.Entities;
using FakeMicro.Interfaces;
using FileInfo = FakeMicro.Entities.FileInfo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using System.Linq;

namespace FakeMicro.Api.Controllers;

/// <summary>
/// 文件管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<FilesController> _logger;

    public FilesController(IClusterClient clusterClient, ILogger<FilesController> logger)
    {
        _clusterClient = clusterClient;
        _logger = logger;
    }

    /// <summary>
    /// 上传文件
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] string description = "", [FromForm] bool isPublic = false, [FromForm] int previewExpiryMinutes = 60)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "请选择要上传的文件" });
            }

            // 获取当前用户ID
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { message = "用户未认证" });
            }

            // 读取文件数据
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileData = memoryStream.ToArray();

            // 调用FileGrain处理文件上传
            var fileGrain = _clusterClient.GetGrain<IFileGrain>(userId.ToString());
            var uploadRequest = new FileUploadRequest
            {
                FileName = file.FileName,
                FileData = fileData,
                ContentType = file.ContentType,
                UserId = userId,
                Description = description,
                IsPublic = isPublic,
                PreviewExpiryMinutes = previewExpiryMinutes
            };

            var result = await fileGrain.UploadFileAsync(uploadRequest);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    fileId = result.FileId,
                    previewUrl = result.PreviewUrl,
                    message = result.Message
                });
            }
            else
            {
                return BadRequest(new { success = false, message = result.Message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件上传失败");
            return StatusCode(500, new { success = false, message = "文件上传失败" });
        }
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    [HttpGet("download/{fileId}")]
    public async Task<IActionResult> DownloadFile(int fileId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { message = "用户未认证" });
            }

            // 调用FileGrain处理文件下载
            var fileGrain = _clusterClient.GetGrain<IFileGrain>(userId.ToString());
            var result = await fileGrain.DownloadFileAsync(fileId);

            if (result.Success)
            {
                // 返回文件流
                return File(result.FileData, result.ContentType, result.FileName);
            }
            else
            {
                return NotFound(new { success = false, message = result.Message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"文件下载失败: {fileId}");
            return StatusCode(500, new { success = false, message = "文件下载失败" });
        }
    }

    /// <summary>
    /// 获取文件预览地址
    /// </summary>
    [HttpGet("preview/{fileId}")]
    public async Task<IActionResult> GetPreviewUrl(int fileId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { message = "用户未认证" });
            }

            // 调用FileGrain生成预览地址
            var fileGrain = _clusterClient.GetGrain<IFileGrain>(userId.ToString());
            var result = await fileGrain.GeneratePreviewUrlAsync(fileId);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    previewUrl = result.PreviewUrl,
                    expiryTime = result.ExpiryTime,
                    message = result.Message
                });
            }
            else
            {
                return NotFound(new { success = false, message = result.Message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"获取预览地址失败: {fileId}");
            return StatusCode(500, new { success = false, message = "获取预览地址失败" });
        }
    }

    /// <summary>
    /// 预览文件（公开访问）
    /// </summary>
    [HttpGet("preview")]
    [AllowAnonymous]
    public async Task<IActionResult> PreviewFile([FromQuery] string token)
    {
        try
        {
            // 解析预览令牌
            var (fileId, expiryTime) = ParsePreviewToken(token);
            if (fileId == 0 || expiryTime < DateTime.UtcNow)
            {
                return BadRequest(new { message = "预览链接已过期或无效" });
            }

            // 获取文件信息
            var userId = 1; // 使用系统用户获取文件信息
            var fileGrain = _clusterClient.GetGrain<IFileGrain>(userId.ToString());
            var fileInfo = await fileGrain.GetFileInfoAsync(fileId);

            if (fileInfo == null)
            {
                return NotFound(new { message = "文件不存在" });
            }

            if (!fileInfo.is_public)
            {
                return Forbid("文件未公开，无法预览");
            }

            // 下载文件
            var result = await fileGrain.DownloadFileAsync(fileId);
            if (result.Success)
            {
                // 根据文件类型返回不同的预览方式
                if (fileInfo.mime_type?.StartsWith("image/") == true)
                {
                    // 图片直接显示
                    return File(result.FileData, result.ContentType);
                }
                else if (fileInfo.mime_type == "application/pdf")
                {
                    // PDF文件在浏览器中预览
                    return File(result.FileData, "application/pdf");
                }
                else
                {
                    // 其他文件类型提供下载
                    return File(result.FileData, result.ContentType, result.FileName);
                }
            }
            else
            {
                return NotFound(new { message = result.Message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"文件预览失败: {token}");
            return StatusCode(500, new { message = "文件预览失败" });
        }
    }

    /// <summary>
    /// 获取用户文件列表
    /// </summary>
    [HttpGet("myfiles")]
    public async Task<IActionResult> GetMyFiles()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { message = "用户未认证" });
            }

            var fileGrain = _clusterClient.GetGrain<IFileGrain>(userId.ToString());
            var files = await fileGrain.GetUserFilesAsync(userId);

            return Ok(new
            {
                success = true,
                files = files.Select(f => new
                {
                    id = f.id,
                    fileName = f.file_name,
                    fileSize = f.file_size,
                    contentType = f.content_type,
                    uploadTime = f.upload_time,
                    description = f.description,
                    isPublic = f.is_public
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户文件列表失败");
            return StatusCode(500, new { success = false, message = "获取文件列表失败" });
        }
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    [HttpDelete("{fileId}")]
    public async Task<IActionResult> DeleteFile(int fileId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { message = "用户未认证" });
            }

            var fileGrain = _clusterClient.GetGrain<IFileGrain>(userId.ToString());
            var success = await fileGrain.DeleteFileAsync(fileId);

            if (success)
            {
                return Ok(new { success = true, message = "文件删除成功" });
            }
            else
            {
                return NotFound(new { success = false, message = "文件不存在或删除失败" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"文件删除失败: {fileId}");
            return StatusCode(500, new { success = false, message = "文件删除失败" });
        }
    }

    #region 私有方法

    private int GetCurrentUserId()
    {
        // 从JWT Token中获取用户ID
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return 0;
    }

    private (int fileId, DateTime expiryTime) ParsePreviewToken(string token)
    {
        try
        {
            var tokenBytes = Convert.FromBase64String(token);
            var tokenData = Encoding.UTF8.GetString(tokenBytes);
            var parts = tokenData.Split('|');

            if (parts.Length == 2 && int.TryParse(parts[0], out var fileId))
            {
                var expiryTime = DateTime.Parse(parts[1]);
                return (fileId, expiryTime);
            }
        }
        catch
        {
            // Token解析失败
        }

        return (0, DateTime.MinValue);
    }

    #endregion
}