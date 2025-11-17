using Orleans;
using System.Collections.Generic;

namespace FakeMicro.Interfaces.Models
{
    /// <summary>
    /// 创建定时任务请求
    /// </summary>
    [GenerateSerializer]
    public class CreateRecurringJobRequest
    {
        [Id(0)]
        public string JobId { get; set; } = string.Empty;
        
        [Id(1)]
        public string CronExpression { get; set; } = string.Empty;
        
        [Id(2)]
        public string? Description { get; set; }
    }

    /// <summary>
    /// 更新定时任务请求
    /// </summary>
    [GenerateSerializer]
    public class UpdateRecurringJobRequest
    {
        [Id(0)]
        public string CronExpression { get; set; } = string.Empty;
        
        [Id(1)]
        public string? Description { get; set; }
    }

    /// <summary>
    /// 创建后台任务请求
    /// </summary>
    [GenerateSerializer]
    public class CreateBackgroundJobRequest
    {
        [Id(0)]
        public string JobName { get; set; } = string.Empty;
        
        [Id(1)]
        public string? Parameters { get; set; }
    }

    /// <summary>
    /// 创建Orleans任务请求
    /// </summary>
    [GenerateSerializer]
    public class CreateOrleansJobRequest
    {
        [Id(0)]
        public string GrainType { get; set; } = string.Empty;
        
        [Id(1)]
        public string Operation { get; set; } = string.Empty;
        
        [Id(2)]
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// 任务状态响应
    /// </summary>
    [GenerateSerializer]
    public class JobStatusResponse
    {
        [Id(0)]
        public string JobId { get; set; } = string.Empty;
        
        [Id(1)]
        public string State { get; set; } = string.Empty;
        
        [Id(2)]
        public DateTime CreatedAt { get; set; }
        
        [Id(3)]
        public List<JobHistoryItem> History { get; set; } = new();
    }

    /// <summary>
    /// 任务历史项
    /// </summary>
    [GenerateSerializer]
    public class JobHistoryItem
    {
        [Id(0)]
        public string StateName { get; set; } = string.Empty;
        
        [Id(1)]
        public DateTime CreatedAt { get; set; }
        
        [Id(2)]
        public string? Reason { get; set; }
    }
}