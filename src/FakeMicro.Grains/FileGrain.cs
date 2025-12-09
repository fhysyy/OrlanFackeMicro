using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Entities;
using FakeMicro.Interfaces;
using FakeMicro.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using System.Security.Cryptography;
using System.Text;
using FileInfo = FakeMicro.Entities.FileInfo;

namespace FakeMicro.Grains;

/// <summary>
/// 文件管理Grain实现
/// 优化版本，继承自OrleansGrainBase，提供更好的异常处理和性能监控
/// </summary>
public class FileGrain : OrleansGrainBase, IFileGrain
{
    private readonly IRepository<FileInfo, long> _fileRepository;
    private readonly FileStorageConfig _storageConfig;

    public FileGrain(
        IRepository<FileInfo, long> fileRepository,
        IOptions<FileStorageConfig> storageConfig,
        ILogger<FileGrain> logger) : base(logger)
    {
        _fileRepository = fileRepository;
        _storageConfig = storageConfig.Value;
    }

    public async Task<FileUploadResult> UploadFileAsync(FileUploadRequest request)
    {
        return await SafeExecuteAsync<FileUploadResult>(nameof(UploadFileAsync), async () =>
        {
            // 验证文件大小
            if (request.FileData.Length > _storageConfig.MaxFileSize)
            {
                return new FileUploadResult
                {
                    Success = false,
                    Message = $"文件大小超过限制，最大允许 {_storageConfig.MaxFileSize / 1024 / 1024}MB"
                };
            }

            // 验证文件类型
            if (!_storageConfig.AllowedFileTypes.Contains(request.ContentType))
            {
                return new FileUploadResult
                {
                    Success = false,
                    Message = $"不支持的文件类型: {request.ContentType}"
                };
            }

            // 计算文件哈希
            var fileHash = CalculateFileHash(request.FileData);

            // 检查是否已存在相同文件
            var allFiles = await _fileRepository.GetAllAsync();
            var existingFile = allFiles.FirstOrDefault(f => f.file_hash == fileHash && !f.is_deleted);

            if (existingFile != null)
            {
                var existingPreviewResult = await GeneratePreviewUrlAsync(existingFile.id);
                return new FileUploadResult
                {
                    Success = true,
                    FileId = existingFile.id,
                    Message = "文件已存在，返回现有文件",
                    PreviewUrl = existingPreviewResult.Success ? existingPreviewResult.PreviewUrl : string.Empty
                };
            }

            // 生成存储路径
            var storagePath = GenerateStoragePath(request.FileName, request.UserId);

            // 保存文件到存储
            await SaveFileToStorage(storagePath, request.FileData);

            // 创建文件记录
            var fileInfo = new FileInfo
            {
                file_name = request.FileName,
                file_path = storagePath,
                file_size = request.FileData.Length,
                mime_type = request.ContentType,
                file_hash = fileHash,
                uploader_id = request.UserId,
                is_public = request.IsPublic,
                description = request.Description ?? string.Empty
            };

            await _fileRepository.AddAsync(fileInfo);
            await _fileRepository.SaveChangesAsync();

            var newPreviewResult = await GeneratePreviewUrlAsync(fileInfo.id);
            return new FileUploadResult
            {
                Success = true,
                FileId = fileInfo.id,
                Message = "文件上传成功",
                PreviewUrl = newPreviewResult.Success ? newPreviewResult.PreviewUrl : string.Empty
            };
        }, request.FileName, request.ContentType);
    }

    public async Task<FileDownloadResult> DownloadFileAsync(long fileId)
    {
        return await SafeExecuteAsync<FileDownloadResult>(nameof(DownloadFileAsync), async () =>
        {
            var fileInfo = await _fileRepository.GetByIdAsync(fileId);
            if (fileInfo == null || fileInfo.is_deleted)
            {
                return new FileDownloadResult
                {
                    Success = false,
                    Message = "文件不存在或已被删除"
                };
            }

            // 检查权限
            if (!fileInfo.is_public)
            {
                // 获取当前用户ID（从请求上下文中获取）
                var currentUserId = GetCurrentUserId();
                
                // 验证用户权限：文件所有者或管理员可以下载
                if (currentUserId != fileInfo.uploader_id && !await IsUserAdminAsync(currentUserId))
                {
                    return new FileDownloadResult
                    {
                        Success = false,
                        Message = "没有权限下载此文件"
                    };
                }
            }

            // 从存储中读取文件
            var fileData = await ReadFileFromStorage(fileInfo.file_path);

            // 更新访问统计
            fileInfo.UpdatedAt = DateTime.UtcNow;
            await _fileRepository.UpdateAsync(fileInfo);
            await _fileRepository.SaveChangesAsync();

            return new FileDownloadResult
            {
                Success = true,
                FileData = fileData,
                FileName = fileInfo.file_name,
                ContentType = fileInfo.mime_type,
                Message = "文件下载成功"
            };
        }, fileId);
    }

    public async Task<FileInfo> GetFileInfoAsync(long fileId)
    {
        var fileInfo = await _fileRepository.GetByIdAsync(fileId);
        return fileInfo?.is_deleted == true ? new FileInfo() : fileInfo ?? new FileInfo();
    }

    public async Task<FilePreviewResult> GeneratePreviewUrlAsync(long fileId)
    {
        return await SafeExecuteAsync<FilePreviewResult>(nameof(GeneratePreviewUrlAsync), async () =>
        {
            var fileInfo = await GetFileInfoAsync(fileId);
            if (fileInfo == null || string.IsNullOrEmpty(fileInfo.file_name))
            {
                return new FilePreviewResult
                {
                    Success = false,
                    Message = "文件不存在"
                };
            }

            // 生成预览令牌（包含文件ID和过期时间）
            var token = GeneratePreviewToken(fileId, 60); // 默认60分钟过期
            var previewUrl = $"{_storageConfig.PreviewBaseUrl}?token={token}";

            return new FilePreviewResult
            {
                Success = true,
                PreviewUrl = previewUrl,
                ExpiryTime = DateTime.UtcNow.AddMinutes(60), // 默认60分钟过期
                Message = "预览地址生成成功"
            };
        }, fileId);
    }

    public async Task<bool> DeleteFileAsync(long fileId)
    {
        return await SafeExecuteAsync<bool>(nameof(DeleteFileAsync), async () =>
        {
            // 检查权限
            var currentUserId = GetCurrentUserId();
            var fileInfo = await _fileRepository.GetByIdAsync(fileId);
            if (fileInfo == null || fileInfo.is_deleted)
            {
                return false;
            }

            // 验证用户权限：文件所有者或管理员可以删除
            if (currentUserId != fileInfo.uploader_id && !await IsUserAdminAsync(currentUserId))
            {
                _logger.LogWarning("用户 {UserId} 尝试删除文件 {FileId} 但无权限", currentUserId, fileId);
                return false;
            }

            // 添加软删除标记和时间戳
            fileInfo.is_deleted = true;
            fileInfo.deleted_at = DateTime.UtcNow;
            await _fileRepository.UpdateAsync(fileInfo);
            await _fileRepository.SaveChangesAsync();

            // 注册文件删除定时任务
            await RegisterFileDeletionTask(fileInfo.id, fileInfo.file_path);

            return true;
        }, fileId);
    }

    public async Task<List<FileInfo>> GetUserFilesAsync(int userId)
    {
        return await SafeExecuteAsync<List<FileInfo>>(nameof(GetUserFilesAsync), async () =>
        {
            var files = await _fileRepository.GetAllAsync();
            return files
                .Where(f => f.uploader_id == userId && !f.is_deleted)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();
        }, userId);
    }

    #region 私有方法

    private string CalculateFileHash(byte[] fileData)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(fileData);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    /// <summary>
    /// 获取当前用户ID（简化的实现，实际项目中应从认证上下文中获取）
    /// </summary>
    private int GetCurrentUserId()
    {
        // 这里简化实现，实际项目中应从请求上下文中获取认证用户ID
        // 假设有一个默认的管理员用户ID或从请求中获取
        return 1; // 简化实现
    }

    /// <summary>
    /// 检查用户是否为管理员
    /// </summary>
    private async Task<bool> IsUserAdminAsync(int userId)
    {
        // 简化实现，实际项目中应查询用户角色信息
        // 这里假设用户ID为1的是管理员
        return userId == 1;
    }

    /// <summary>
    /// 注册文件删除定时任务
    /// </summary>
    private async Task RegisterFileDeletionTask(long fileId, string filePath)
    {
        try
        {
            // 创建定时任务，在30天后实际删除物理文件
            var taskName = $"FileDeletion_{fileId}";
            var deletionTime = DateTime.UtcNow.AddDays(30);
            
            // 这里可以注册一个后台任务或定时任务
            // 简化实现：记录到日志，实际项目中应使用定时任务框架
            _logger.LogInformation("文件删除任务已注册: 文件ID {FileId} 将在 {DeletionTime} 实际删除", 
                fileId, deletionTime);
                
            // 实际项目中，可以集成 Quartz.NET 或其他定时任务框架
            // await SchedulePhysicalFileDeletion(fileId, filePath, deletionTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册文件删除定时任务失败: {FileId}", fileId);
        }
    }

    /// <summary>
    /// 获取当前用户ID（简化的实现，实际项目中应从认证上下文中获取）
    /// </summary>
  

    private string GenerateStoragePath(string fileName, int userId)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var fileExtension = Path.GetExtension(fileName);
        var randomString = Guid.NewGuid().ToString("N").Substring(0, 8);
        
        return Path.Combine(
            userId.ToString(),
            timestamp + "_" + randomString + fileExtension
        ).Replace("\\", "/");
    }

    private async Task SaveFileToStorage(string storagePath, byte[] fileData)
    {
        var fullPath = Path.Combine(_storageConfig.LocalStoragePath, storagePath);
        var directory = Path.GetDirectoryName(fullPath);
        
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(fullPath, fileData);
    }

    private async Task<byte[]> ReadFileFromStorage(string storagePath)
    {
        var fullPath = Path.Combine(_storageConfig.LocalStoragePath, storagePath);
        return await File.ReadAllBytesAsync(fullPath);
    }

    private string GeneratePreviewToken(long fileId, int expiryMinutes)
    {
        var tokenData = $"{fileId}|{DateTime.UtcNow.AddMinutes(expiryMinutes):O}";
        var tokenBytes = Encoding.UTF8.GetBytes(tokenData);
        return Convert.ToBase64String(tokenBytes);
    }

    #endregion

    #region SafeExecuteAsync 方法

    private async Task<T> SafeExecuteAsync<T>(string methodName, Func<Task<T>> operation, params object[] args)
    {
        try
        {
            LogTrace("开始执行方法: {MethodName}, 参数: {@Args}", methodName, args);
            var result = await operation();
            LogTrace("方法执行成功: {MethodName}", methodName);
            return result;
        }
        catch (Exception ex)
        {
            LogError(ex, "方法执行失败: {MethodName}, 参数: {@Args}", methodName, args);
            // 根据返回类型创建适当的失败结果
            if (typeof(T) == typeof(bool))
            {
                return (T)(object)false;
            }
            throw;
        }
    }

    private async Task SafeExecuteAsync(string methodName, Func<Task> operation, params object[] args)
    {
        try
        {
            LogTrace("开始执行方法: {MethodName}, 参数: {@Args}", methodName, args);
            await operation();
            LogTrace("方法执行成功: {MethodName}", methodName);
        }
        catch (Exception ex)
        {
            LogError(ex, "方法执行失败: {MethodName}, 参数: {@Args}", methodName, args);
            throw;
        }
    }

    #endregion
}