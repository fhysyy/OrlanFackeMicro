using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FakeMicro.Utilities
{
    /// <summary>
    /// 性能监控工具
    /// </summary>
    public static class PerformanceMonitor
    {
        private static readonly Dictionary<string, PerformanceCounter> _counters = new Dictionary<string, PerformanceCounter>();
        private static readonly object _lockObject = new object();
        
        /// <summary>
        /// 性能计数器
        /// </summary>
        public class PerformanceCounter
        {
            public string Name { get; set; }
            public long TotalCount { get; set; }
            public long ErrorCount { get; set; }
            public double AverageDuration { get; set; }
            public double MinDuration { get; set; } = double.MaxValue;
            public double MaxDuration { get; set; }
            public DateTime LastUpdated { get; set; } = DateTime.Now;
            public List<double> RecentDurations { get; set; } = new List<double>();
            
            public void RecordDuration(double duration, bool isError = false)
            {
                lock (this)
                {
                    TotalCount++;
                    if (isError) ErrorCount++;
                    
                    RecentDurations.Add(duration);
                    if (RecentDurations.Count > 100) RecentDurations.RemoveAt(0);
                    
                    AverageDuration = RecentDurations.Average();
                    MinDuration = Math.Min(MinDuration, duration);
                    MaxDuration = Math.Max(MaxDuration, duration);
                    LastUpdated = DateTime.Now;
                }
            }
        }
        
        /// <summary>
        /// 性能监控配置
        /// </summary>
        public class PerformanceMonitorConfig
        {
            public bool EnableMemoryMonitoring { get; set; } = true;
            public bool EnableCpuMonitoring { get; set; } = true;
            public int SamplingIntervalMs { get; set; } = 1000;
            public int MaxHistorySize { get; set; } = 1000;
        }
        
        private static PerformanceMonitorConfig _config = new PerformanceMonitorConfig();
        private static Timer? _monitoringTimer;
        private static readonly List<PerformanceMetric> _metricsHistory = new List<PerformanceMetric>();
        
        /// <summary>
        /// 性能指标
        /// </summary>
        public class PerformanceMetric
        {
            public DateTime Timestamp { get; set; }
            public double CpuUsage { get; set; }
            public long MemoryUsage { get; set; }
            public int ThreadCount { get; set; }
            public int GcCollectionCount0 { get; set; }
            public int GcCollectionCount1 { get; set; }
            public int GcCollectionCount2 { get; set; }
        }
        
        /// <summary>
        /// 配置性能监控
        /// </summary>
        public static void Configure(Action<PerformanceMonitorConfig> configure)
        {
            lock (_lockObject)
            {
                configure?.Invoke(_config);
                StartMonitoring();
            }
        }
        
        /// <summary>
        /// 开始性能监控
        /// </summary>
        private static void StartMonitoring()
        {
            _monitoringTimer?.Dispose();
            _monitoringTimer = new Timer(CollectMetrics, null, 0, _config.SamplingIntervalMs);
        }
        
        /// <summary>
        /// 收集性能指标
        /// </summary>
        private static void CollectMetrics(object? state)
        {
            try
            {
                var metric = new PerformanceMetric
                {
                    Timestamp = DateTime.Now,
                    MemoryUsage = GC.GetTotalMemory(false),
                    ThreadCount = Process.GetCurrentProcess().Threads.Count,
                    GcCollectionCount0 = GC.CollectionCount(0),
                    GcCollectionCount1 = GC.CollectionCount(1),
                    GcCollectionCount2 = GC.CollectionCount(2)
                };
                
                lock (_metricsHistory)
                {
                    _metricsHistory.Add(metric);
                    if (_metricsHistory.Count > _config.MaxHistorySize)
                        _metricsHistory.RemoveAt(0);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Error("性能监控收集指标失败", ex);
            }
        }
        
        /// <summary>
        /// 获取性能计数器
        /// </summary>
        public static PerformanceCounter GetCounter(string name)
        {
            lock (_lockObject)
            {
                if (!_counters.ContainsKey(name))
                {
                    _counters[name] = new PerformanceCounter { Name = name };
                }
                return _counters[name];
            }
        }
        
        /// <summary>
        /// 记录操作持续时间
        /// </summary>
        public static void RecordOperation(string operationName, double durationMs, bool isError = false)
        {
            var counter = GetCounter(operationName);
            counter.RecordDuration(durationMs, isError);
        }
        
        /// <summary>
        /// 获取所有性能计数器
        /// </summary>
        public static Dictionary<string, PerformanceCounter> GetAllCounters()
        {
            lock (_lockObject)
            {
                return new Dictionary<string, PerformanceCounter>(_counters);
            }
        }
        
        /// <summary>
        /// 获取性能指标历史
        /// </summary>
        public static List<PerformanceMetric> GetMetricsHistory()
        {
            lock (_metricsHistory)
            {
                return new List<PerformanceMetric>(_metricsHistory);
            }
        }
        
        /// <summary>
        /// 性能监控范围
        /// </summary>
        public class PerformanceScope : IDisposable
        {
            private readonly string _operationName;
            private readonly Stopwatch _stopwatch;
            private bool _disposed = false;
            
            public PerformanceScope(string operationName)
            {
                _operationName = operationName;
                _stopwatch = Stopwatch.StartNew();
            }
            
            public void Dispose()
            {
                if (_disposed) return;
                
                _stopwatch.Stop();
                RecordOperation(_operationName, _stopwatch.Elapsed.TotalMilliseconds);
                _disposed = true;
            }
            
            public void MarkAsError()
            {
                _stopwatch.Stop();
                RecordOperation(_operationName, _stopwatch.Elapsed.TotalMilliseconds, true);
                _disposed = true;
            }
        }
        
        /// <summary>
        /// 创建性能监控范围
        /// </summary>
        public static PerformanceScope CreateScope(string operationName)
        {
            return new PerformanceScope(operationName);
        }
        
        /// <summary>
        /// 生成性能报告
        /// </summary>
        public static PerformanceReport GenerateReport()
        {
            var counters = GetAllCounters();
            var metrics = GetMetricsHistory();
            
            return new PerformanceReport
            {
                Timestamp = DateTime.Now,
                Counters = counters,
                Metrics = metrics,
                Summary = GenerateSummary(counters, metrics)
            };
        }
        
        /// <summary>
        /// 生成性能摘要
        /// </summary>
        private static PerformanceSummary GenerateSummary(Dictionary<string, PerformanceCounter> counters, List<PerformanceMetric> metrics)
        {
            var summary = new PerformanceSummary();
            
            if (metrics.Any())
            {
                summary.AverageMemoryUsage = metrics.Average(m => m.MemoryUsage);
                summary.MaxMemoryUsage = metrics.Max(m => m.MemoryUsage);
                summary.AverageThreadCount = metrics.Average(m => m.ThreadCount);
            }
            
            if (counters.Any())
            {
                summary.TotalOperations = counters.Values.Sum(c => c.TotalCount);
                summary.TotalErrors = counters.Values.Sum(c => c.ErrorCount);
                summary.ErrorRate = summary.TotalOperations > 0 ? (double)summary.TotalErrors / summary.TotalOperations : 0;
            }
            
            return summary;
        }
    }
    
    /// <summary>
    /// 性能报告
    /// </summary>
    public class PerformanceReport
    {
        public DateTime Timestamp { get; set; }
        public Dictionary<string, PerformanceMonitor.PerformanceCounter> Counters { get; set; } = new Dictionary<string, PerformanceMonitor.PerformanceCounter>();
        public List<PerformanceMonitor.PerformanceMetric> Metrics { get; set; } = new List<PerformanceMonitor.PerformanceMetric>();
        public PerformanceSummary Summary { get; set; } = new PerformanceSummary();
    }
    
    /// <summary>
    /// 性能摘要
    /// </summary>
    public class PerformanceSummary
    {
        public long TotalOperations { get; set; }
        public long TotalErrors { get; set; }
        public double ErrorRate { get; set; }
        public double AverageMemoryUsage { get; set; }
        public long MaxMemoryUsage { get; set; }
        public double AverageThreadCount { get; set; }
    }
}