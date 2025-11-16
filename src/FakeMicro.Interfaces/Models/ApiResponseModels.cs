using Orleans;

namespace FakeMicro.Interfaces.Models
{
    /// <summary>
    /// 通用API响应模型
    /// </summary>
    [GenerateSerializer]
    public class ApiResponse<T>
    {
        [Id(0)]
        public bool Success { get; set; }
        [Id(1)]
        public string? Message { get; set; }
        [Id(2)]
        public T? Data { get; set; }
    }

    /// <summary>
    /// 通用错误响应
    /// </summary>
    [GenerateSerializer]
    public class ErrorResponse
    {
        [Id(0)]
        public bool Success { get; set; } = false;
        [Id(1)]
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 通用成功响应
    /// </summary>
    [GenerateSerializer]
    public class SuccessResponse
    {
        [Id(0)]
        public bool Success { get; set; } = true;
        [Id(1)]
        public string Message { get; set; } = "操作成功";
    }

    /// <summary>
    /// 分页请求参数
    /// </summary>
    [GenerateSerializer]
    public class PaginationRequest
    {
        [Id(0)]
        public int Page { get; set; } = 1;
        [Id(1)]
        public int PageSize { get; set; } = 10;
        [Id(2)]
        public string? Keyword { get; set; }
    }
}