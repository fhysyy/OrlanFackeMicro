using FakeMicro.Entities;
using Orleans;
using FileInfo = FakeMicro.Entities.FileInfo;

namespace FakeMicro.Interfaces;

/// <summary>
/// 文件管理Grain接口
/// </summary>
public interface IFileGrain :IGrainWithStringKey
{
    /// <summary>
    /// 上传文件
    /// </summary>
    Task<FileUploadResult> UploadFileAsync(FileUploadRequest request);
    
    /// <summary>
    /// 下载文件
    /// </summary>
    Task<FileDownloadResult> DownloadFileAsync(long fileId);
    
    /// <summary>
    /// 获取文件信息
    /// </summary>
    Task<FileInfo> GetFileInfoAsync(long fileId);
    
    /// <summary>
    /// 生成预览地址
    /// </summary>
    Task<FilePreviewResult> GeneratePreviewUrlAsync(long fileId);
    
    /// <summary>
    /// 删除文件
    /// </summary>
    Task<bool> DeleteFileAsync(long fileId);
    
    /// <summary>
    /// 获取用户文件列表
    /// </summary>
    Task<List<FileInfo>> GetUserFilesAsync(int userId);
}

/// <summary>
/// 文件上传请求
/// </summary>
[GenerateSerializer]
public record FileUploadRequest
{
    [Id(0)] public string FileName { get; set; }
    [Id(1)] public byte[] FileData { get; set; }
    [Id(2)] public string ContentType { get; set; }
    [Id(3)] public int UserId { get; set; }
    [Id(4)] public string Description { get; set; }
    [Id(5)] public bool IsPublic { get; set; }
    [Id(6)] public int PreviewExpiryMinutes { get; set; } = 60;
}

/// <summary>
/// 文件上传结果
/// </summary>
[GenerateSerializer]
public record FileUploadResult
{
    [Id(0)] public bool Success { get; set; }
    [Id(1)] public long FileId { get; set; }
    [Id(2)] public string Message { get; set; }
    [Id(3)] public string PreviewUrl { get; set; }
}

/// <summary>
/// 文件下载结果
/// </summary>
[GenerateSerializer]
public record FileDownloadResult
{
    [Id(0)] public bool Success { get; set; }
    [Id(1)] public byte[] FileData { get; set; }
    [Id(2)] public string FileName { get; set; }
    [Id(3)] public string ContentType { get; set; }
    [Id(4)] public string Message { get; set; }
}

/// <summary>
/// 文件预览结果
/// </summary>
[GenerateSerializer]
public record FilePreviewResult
{
    [Id(0)] public bool Success { get; set; }
    [Id(1)] public string PreviewUrl { get; set; }
    [Id(2)] public DateTime ExpiryTime { get; set; }
    [Id(3)] public string Message { get; set; }
}