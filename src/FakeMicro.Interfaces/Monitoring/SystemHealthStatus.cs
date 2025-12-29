using System;
using System.Collections.Generic;
using Orleans;

namespace FakeMicro.Interfaces.Monitoring
{
    /// <summary>
    /// 系统健康状态
    /// </summary>
    [GenerateSerializer]
    public class SystemHealthStatus
    {
        [Id(0)]
        public bool IsHealthy { get; set; }
        [Id(1)]
        public string Status { get; set; }
        [Id(2)]
        public DateTime LastCheckTime { get; set; }
        [Id(3)]
        public Dictionary<string, string> Details { get; set; } = new();
        [Id(4)]
        public List<AlertInfo> ActiveAlerts { get; set; } = new();
    }

    /// <summary>
    /// 系统指标报告
    /// </summary>
    [GenerateSerializer]
    public class SystemMetricsReport
    {
        [Id(0)]
        public DateTime ReportTime { get; set; }
        [Id(1)]
        public Dictionary<string, MetricStats> Metrics { get; set; } = new();
        [Id(2)]
        public int ActiveGrains { get; set; }
        [Id(3)]
        public double MemoryUsageMB { get; set; }
        [Id(4)]
        public TimeSpan Uptime { get; set; }
        [Id(5)]
        public int TotalRequests { get; set; }
        [Id(6)]
        public int ErrorRequests { get; set; }
        [Id(7)]
        public double ErrorRate { get; set; }
    }

    /// <summary>
    /// 指标统计
    /// </summary>
    [GenerateSerializer]
    public class MetricStats
    {
        [Id(0)]
        public double Average { get; set; }
        [Id(1)]
        public double Min { get; set; }
        [Id(2)]
        public double Max { get; set; }
        [Id(3)]
        public int Count { get; set; }
        [Id(4)]
        public DateTime LastUpdate { get; set; }
        [Id(5)]
        public double P95 { get; set; }
        [Id(6)]
        public double P99 { get; set; }
        [Id(7)]
        public List<double> RecentValues { get; set; } = new List<double>(10);
    }

    /// <summary>
    /// 告警信息
    /// </summary>
    [GenerateSerializer]
    public class AlertInfo
    {
        [Id(0)]
        public string AlertId { get; set; }
        [Id(1)]
        public string AlertName { get; set; }
        [Id(2)]
        public AlertLevel Level { get; set; }
        [Id(3)]
        public string Description { get; set; }
        [Id(4)]
        public DateTime TriggerTime { get; set; }
        [Id(5)]
        public Dictionary<string, string> Details { get; set; } = new();
    }

    /// <summary>
    /// 告警级别
    /// </summary>
    [GenerateSerializer]
    public enum AlertLevel
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Critical = 3
    }

    /// <summary>
    /// 告警配置
    /// </summary>
    [GenerateSerializer]
    public class AlertConfiguration
    {
        [Id(0)]
        public string MetricName { get; set; }
        [Id(1)]
        public double Threshold { get; set; }
        [Id(2)]
        public AlertLevel Level { get; set; }
        [Id(3)]
        public string Description { get; set; }
        [Id(4)]
        public bool Enabled { get; set; }
        [Id(5)]
        public int DurationSeconds { get; set; } // 持续时间超过阈值才触发告警
    }
}