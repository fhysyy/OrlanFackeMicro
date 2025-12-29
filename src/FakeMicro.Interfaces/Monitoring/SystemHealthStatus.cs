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
    }
}