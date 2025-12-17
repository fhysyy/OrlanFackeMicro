using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FakeMicro.Monitoring;

/// <summary>
    /// 监控配置选项
    /// </summary>
    public class MonitoringOptions
    {
        public int SlowQueryThresholdMs { get; set; } = 500;
        public int SlowGrainCallThresholdMs { get; set; } = 1000;
        public int MaxRecentDurationsCount { get; set; } = 100;
        public int AlertThresholds { get; set; } = 5;
        public TimeSpan AlertWindow { get; set; } = TimeSpan.FromMinutes(5);
        public bool EnableAlerting { get; set; } = true;
        public bool EnablePersistence { get; set; } = false;
    }

    /// <summary>
    /// 告警信息
    /// </summary>
    public class AlertInfo
    {
        public string AlertId { get; set; } = Guid.NewGuid().ToString();
        public string AlertType { get; set; }
        public string OperationId { get; set; }
        public double MetricValue { get; set; }
        public double ThresholdValue { get; set; }
        public DateTime TriggerTime { get; set; }
        public string Message { get; set; }
        public AlertSeverity Severity { get; set; }
    }

    /// <summary>
    /// 告警严重程度
    /// </summary>
    public enum AlertSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// 性能监控服务
    /// 提供Orleans环境下SQL性能监控和Grain调用跟踪
    /// </summary>
    public class PerformanceMonitor
    {
        private readonly ILogger<PerformanceMonitor> _logger;
        private readonly ConcurrentDictionary<string, PerformanceMetrics> _metrics = new ConcurrentDictionary<string, PerformanceMetrics>();
        private readonly ConcurrentDictionary<string, long> _activeOperations = new ConcurrentDictionary<string, long>();
        private readonly ConcurrentDictionary<string, List<DateTime>> _recentFailures = new ConcurrentDictionary<string, List<DateTime>>();
        private readonly ConcurrentDictionary<string, DateTime> _recentAlerts = new ConcurrentDictionary<string, DateTime>();
        private readonly object _metricsLock = new object();
        private readonly MonitoringOptions _options;
        
        // 监控统计信息
        private long _totalGrainCalls = 0;
        private long _totalSqlQueries = 0;
        private long _slowQueries = 0;
        private long _failedCalls = 0;
        private long _totalAlerts = 0;
        private long _currentConcurrentCalls = 0;
        private long _peakConcurrentCalls = 0;

    /// <summary>
    /// 性能指标类
    /// </summary>
    public class PerformanceMetrics
    {
        public string Name { get; set; } = string.Empty;
        public long CallCount { get; set; }
        public long TotalDurationMs { get; set; }
        public long AverageDurationMs => CallCount > 0 ? TotalDurationMs / CallCount : 0;
        public long MedianDurationMs { get; set; }
        public long P95DurationMs { get; set; }
        public long P99DurationMs { get; set; }
        public long FailedCount { get; set; }
        public double SuccessRate => CallCount > 0 ? (1.0 - (double)FailedCount / CallCount) * 100 : 100;
        public DateTime LastCallTime { get; set; }
        public List<long> RecentDurations { get; set; } = new List<long>();
        public long MaxRecentDuration { get; set; }
        public long MinRecentDuration { get; set; }
        public long PeakConcurrentCalls { get; set; }
        public DateTime LastPeakTime { get; set; }
    }

    /// <summary>
        /// 构造函数
        /// </summary>
        public PerformanceMonitor(ILogger<PerformanceMonitor> logger, Action<MonitoringOptions>? configureOptions = null)
        {
            _logger = logger;
            _options = new MonitoringOptions();
            configureOptions?.Invoke(_options);
        }

    /// <summary>
        /// 记录Grain调用开始
        /// </summary>
        public IDisposable TrackGrainCall(string grainType, string grainKey, string operationName)
        {
            var operationId = $"{grainType}.{operationName}";
            var stopwatch = Stopwatch.StartNew();
            
            Interlocked.Increment(ref _totalGrainCalls);
            var currentActive = _activeOperations.AddOrUpdate(operationId, 1, (key, value) => value + 1);
            
            // 更新并发调用统计
            var currentConcurrent = Interlocked.Increment(ref _currentConcurrentCalls);
            var peakConcurrent = Interlocked.Read(ref _peakConcurrentCalls);
            if (currentConcurrent > peakConcurrent)
            {
                Interlocked.CompareExchange(ref _peakConcurrentCalls, currentConcurrent, peakConcurrent);
            }
            
            // 更新操作峰值
            var metrics = _metrics.GetOrAdd(operationId, key => new PerformanceMetrics { Name = key });
            lock (metrics)
            {
                if (currentActive > metrics.PeakConcurrentCalls)
                {
                    metrics.PeakConcurrentCalls = currentActive;
                    metrics.LastPeakTime = DateTime.UtcNow;
                }
            }
            
            return new DisposableOperation(() => CompleteGrainCall(operationId, grainKey, stopwatch, null));
        }

    /// <summary>
        /// 记录SQL查询开始
        /// </summary>
        public IDisposable TrackSqlQuery(string sqlType, string sql, Dictionary<string, object>? parameters = null)
        {
            var operationId = $"SQL.{sqlType}";
            var stopwatch = Stopwatch.StartNew();
            
            Interlocked.Increment(ref _totalSqlQueries);
            var currentActive = _activeOperations.AddOrUpdate(operationId, 1, (key, value) => value + 1);
            
            // 更新并发调用统计
            var currentConcurrent = Interlocked.Increment(ref _currentConcurrentCalls);
            var peakConcurrent = Interlocked.Read(ref _peakConcurrentCalls);
            if (currentConcurrent > peakConcurrent)
            {
                Interlocked.CompareExchange(ref _peakConcurrentCalls, currentConcurrent, peakConcurrent);
            }
            
            // 更新操作峰值
            var metrics = _metrics.GetOrAdd(operationId, key => new PerformanceMetrics { Name = key });
            lock (metrics)
            {
                if (currentActive > metrics.PeakConcurrentCalls)
                {
                    metrics.PeakConcurrentCalls = currentActive;
                    metrics.LastPeakTime = DateTime.UtcNow;
                }
            }
            
            return new DisposableOperation(() => CompleteSqlQuery(operationId, sql, parameters, stopwatch, null));
        }

    /// <summary>
        /// 完成Grain调用跟踪
        /// </summary>
        private void CompleteGrainCall(string operationId, string grainKey, Stopwatch stopwatch, Exception? exception)
        {
            stopwatch.Stop();
            var duration = stopwatch.ElapsedMilliseconds;
            
            _activeOperations.AddOrUpdate(operationId, 0, (key, value) => Math.Max(0, value - 1));
            Interlocked.Decrement(ref _currentConcurrentCalls);
            
            var metrics = _metrics.GetOrAdd(operationId, key => new PerformanceMetrics { Name = key });
            
            lock (metrics)
            {
                metrics.CallCount++;
                metrics.TotalDurationMs += duration;
                metrics.LastCallTime = DateTime.UtcNow;
                
                // 维护最近调用的时长记录
                metrics.RecentDurations.Add(duration);
                if (metrics.RecentDurations.Count > _options.MaxRecentDurationsCount)
                {
                    metrics.RecentDurations.RemoveAt(0);
                }
                
                // 计算中位数和百分位数
                if (metrics.RecentDurations.Count > 0)
                {
                    metrics.MaxRecentDuration = metrics.RecentDurations.Max();
                    metrics.MinRecentDuration = metrics.RecentDurations.Min();
                    metrics.MedianDurationMs = CalculateMedian(metrics.RecentDurations);
                    metrics.P95DurationMs = CalculatePercentile(metrics.RecentDurations, 95);
                    metrics.P99DurationMs = CalculatePercentile(metrics.RecentDurations, 99);
                }
                
                if (exception != null)
                {
                    metrics.FailedCount++;
                    Interlocked.Increment(ref _failedCalls);
                    
                    // 记录失败时间，用于告警
                    RecordFailure(operationId);
                }
            }
            
            // 检查慢调用
            if (duration > _options.SlowGrainCallThresholdMs)
            {
                Interlocked.Increment(ref _slowQueries);
                _logger.LogWarning("慢Grain调用: {OperationId}, GrainKey={GrainKey}, Duration={Duration}ms", 
                    operationId, grainKey, duration);
            }
            
            if (exception != null)
            {
                _logger.LogError(exception, "Grain调用失败: {OperationId}, GrainKey={GrainKey}, Duration={Duration}ms", 
                    operationId, grainKey, duration);
            }
            else if (duration > _options.SlowGrainCallThresholdMs / 2)
            {
                _logger.LogInformation("Grain调用完成: {OperationId}, GrainKey={GrainKey}, Duration={Duration}ms", 
                    operationId, grainKey, duration);
            }
            else
            {
                _logger.LogDebug("Grain调用完成: {OperationId}, GrainKey={GrainKey}, Duration={Duration}ms", 
                    operationId, grainKey, duration);
            }
            
            // 检查告警
            CheckAlerts(operationId);
        }

    /// <summary>
        /// 完成SQL查询跟踪
        /// </summary>
        private void CompleteSqlQuery(string operationId, string sql, Dictionary<string, object>? parameters, Stopwatch stopwatch, Exception? exception)
        {
            stopwatch.Stop();
            var duration = stopwatch.ElapsedMilliseconds;
            
            _activeOperations.AddOrUpdate(operationId, 0, (key, value) => Math.Max(0, value - 1));
            Interlocked.Decrement(ref _currentConcurrentCalls);
            
            var metrics = _metrics.GetOrAdd(operationId, key => new PerformanceMetrics { Name = key });
            
            lock (metrics)
            {
                metrics.CallCount++;
                metrics.TotalDurationMs += duration;
                metrics.LastCallTime = DateTime.UtcNow;
                
                // 维护最近查询的时长记录
                metrics.RecentDurations.Add(duration);
                if (metrics.RecentDurations.Count > _options.MaxRecentDurationsCount)
                {
                    metrics.RecentDurations.RemoveAt(0);
                }
                
                // 计算中位数和百分位数
                if (metrics.RecentDurations.Count > 0)
                {
                    metrics.MaxRecentDuration = metrics.RecentDurations.Max();
                    metrics.MinRecentDuration = metrics.RecentDurations.Min();
                    metrics.MedianDurationMs = CalculateMedian(metrics.RecentDurations);
                    metrics.P95DurationMs = CalculatePercentile(metrics.RecentDurations, 95);
                    metrics.P99DurationMs = CalculatePercentile(metrics.RecentDurations, 99);
                }
                
                if (exception != null)
                {
                    metrics.FailedCount++;
                    Interlocked.Increment(ref _failedCalls);
                    
                    // 记录失败时间，用于告警
                    RecordFailure(operationId);
                }
            }
            
            // 记录慢查询
            if (duration > _options.SlowQueryThresholdMs)
            {
                Interlocked.Increment(ref _slowQueries);
                _logger.LogWarning("慢SQL查询: {OperationId}, Duration={Duration}ms, SQL={SQL}", 
                    operationId, duration, sql);
            }
            
            if (exception != null)
            {
                _logger.LogError(exception, "SQL查询失败: {OperationId}, Duration={Duration}ms, SQL={SQL}", 
                    operationId, duration, sql);
            }
            
            // 检查告警
            CheckAlerts(operationId);
        }

    /// <summary>
        /// 记录失败时间
        /// </summary>
        private void RecordFailure(string operationId)
        {
            var failures = _recentFailures.GetOrAdd(operationId, key => new List<DateTime>());
            lock (failures)
            {
                failures.Add(DateTime.UtcNow);
                
                // 清理过期的失败记录
                var cutoffTime = DateTime.UtcNow - _options.AlertWindow;
                failures.RemoveAll(time => time < cutoffTime);
            }
        }
        
        /// <summary>
        /// 检查告警条件
        /// </summary>
        private void CheckAlerts(string operationId)
        {
            if (!_options.EnableAlerting)
                return;
            
            var failures = _recentFailures.GetOrAdd(operationId, key => new List<DateTime>());
            lock (failures)
            {
                var cutoffTime = DateTime.UtcNow - _options.AlertWindow;
                var recentFailures = failures.Count(time => time >= cutoffTime);
                
                // 检查是否达到告警阈值
                if (recentFailures >= _options.AlertThresholds)
                {
                    // 检查是否已经发送过告警
                    var lastAlertTime = _recentAlerts.GetValueOrDefault(operationId, DateTime.MinValue);
                    if (DateTime.UtcNow - lastAlertTime > _options.AlertWindow)
                    {
                        // 发送告警
                        var alert = new AlertInfo
                        {
                            AlertType = "FailureRateExceeded",
                            OperationId = operationId,
                            MetricValue = recentFailures,
                            ThresholdValue = _options.AlertThresholds,
                            TriggerTime = DateTime.UtcNow,
                            Message = $"Operation {operationId} has {recentFailures} failures in the last {_options.AlertWindow.TotalMinutes} minutes, exceeding threshold of {_options.AlertThresholds}",
                            Severity = AlertSeverity.Error
                        };
                        
                        SendAlert(alert);
                        _recentAlerts[operationId] = DateTime.UtcNow;
                        Interlocked.Increment(ref _totalAlerts);
                    }
                }
            }
        }
        
        /// <summary>
        /// 发送告警
        /// </summary>
        protected virtual void SendAlert(AlertInfo alert)
        {
            // 默认实现：记录日志
            switch (alert.Severity)
            {
                case AlertSeverity.Info:
                    _logger.LogInformation("[ALERT] {Message}", alert.Message);
                    break;
                case AlertSeverity.Warning:
                    _logger.LogWarning("[ALERT] {Message}", alert.Message);
                    break;
                case AlertSeverity.Error:
                    _logger.LogError("[ALERT] {Message}", alert.Message);
                    break;
                case AlertSeverity.Critical:
                    _logger.LogCritical("[ALERT] {Message}", alert.Message);
                    break;
            }
            
            // 可以扩展为发送到外部系统（如Prometheus Alertmanager、Slack等）
        }
        
        /// <summary>
        /// 计算中位数
        /// </summary>
        private static long CalculateMedian(List<long> durations)
        {
            if (durations.Count == 0)
                return 0;
                
            var sorted = durations.OrderBy(d => d).ToList();
            var middle = sorted.Count / 2;
            
            if (sorted.Count % 2 == 0)
            {
                return (sorted[middle - 1] + sorted[middle]) / 2;
            }
            else
            {
                return sorted[middle];
            }
        }
        
        /// <summary>
        /// 计算百分位数
        /// </summary>
        private static long CalculatePercentile(List<long> durations, int percentile)
        {
            if (durations.Count == 0)
                return 0;
                
            var sorted = durations.OrderBy(d => d).ToList();
            var index = (int)Math.Ceiling((percentile / 100.0) * sorted.Count) - 1;
            index = Math.Max(0, Math.Min(index, sorted.Count - 1));
            
            return sorted[index];
        }
        
        /// <summary>
        /// 获取性能监控报告
        /// </summary>
        public PerformanceReport GetPerformanceReport()
        {
            var report = new PerformanceReport
            {
                TotalGrainCalls = Interlocked.Read(ref _totalGrainCalls),
                TotalSqlQueries = Interlocked.Read(ref _totalSqlQueries),
                SlowQueries = Interlocked.Read(ref _slowQueries),
                FailedCalls = Interlocked.Read(ref _failedCalls),
                TotalAlerts = Interlocked.Read(ref _totalAlerts),
                CurrentConcurrentCalls = Interlocked.Read(ref _currentConcurrentCalls),
                PeakConcurrentCalls = Interlocked.Read(ref _peakConcurrentCalls),
                ActiveOperations = new Dictionary<string, long>(_activeOperations),
                Metrics = new Dictionary<string, PerformanceMetrics>()
            };
            
            foreach (var kvp in _metrics)
            {
                var metrics = new PerformanceMetrics();
                lock (kvp.Value)
            {
                metrics.Name = kvp.Value.Name;
                metrics.CallCount = kvp.Value.CallCount;
                metrics.TotalDurationMs = kvp.Value.TotalDurationMs;
                metrics.FailedCount = kvp.Value.FailedCount;
                metrics.LastCallTime = kvp.Value.LastCallTime;
                metrics.RecentDurations = new List<long>(kvp.Value.RecentDurations);
                metrics.MaxRecentDuration = kvp.Value.MaxRecentDuration;
                metrics.MinRecentDuration = kvp.Value.MinRecentDuration;
            }
            report.Metrics[kvp.Key] = metrics;
        }
        
        return report;
    }

    /// <summary>
        /// 重置监控数据
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref _totalGrainCalls, 0);
            Interlocked.Exchange(ref _totalSqlQueries, 0);
            Interlocked.Exchange(ref _slowQueries, 0);
            Interlocked.Exchange(ref _failedCalls, 0);
            Interlocked.Exchange(ref _totalAlerts, 0);
            Interlocked.Exchange(ref _currentConcurrentCalls, 0);
            Interlocked.Exchange(ref _peakConcurrentCalls, 0);
            _metrics.Clear();
            _activeOperations.Clear();
            _recentFailures.Clear();
            _recentAlerts.Clear();
        }

    /// <summary>
    /// 性能报告类
    /// </summary>
    public class PerformanceReport
    {
        public long TotalGrainCalls { get; set; }
        public long TotalSqlQueries { get; set; }
        public long SlowQueries { get; set; }
        public long FailedCalls { get; set; }
        public long TotalAlerts { get; set; }
        public long CurrentConcurrentCalls { get; set; }
        public long PeakConcurrentCalls { get; set; }
        public Dictionary<string, long> ActiveOperations { get; set; } = new Dictionary<string, long>();
        public Dictionary<string, PerformanceMetrics> Metrics { get; set; } = new Dictionary<string, PerformanceMetrics>();
        
        public double GrainCallSuccessRate => TotalGrainCalls > 0 ? (1.0 - (double)FailedCalls / TotalGrainCalls) * 100 : 100;
        public double SlowQueryRate => TotalSqlQueries > 0 ? (double)SlowQueries / TotalSqlQueries * 100 : 0;
        public double OverallSuccessRate => (TotalGrainCalls + TotalSqlQueries) > 0 ? 
            (1.0 - (double)FailedCalls / (TotalGrainCalls + TotalSqlQueries)) * 100 : 100;
        public double AverageGrainCallDuration => TotalGrainCalls > 0 ? 
            Metrics.Where(m => m.Key.Contains(".")).Average(m => m.Value.AverageDurationMs) : 0;
        public double AverageSqlQueryDuration => TotalSqlQueries > 0 ? 
            Metrics.Where(m => m.Key.StartsWith("SQL.")).Average(m => m.Value.AverageDurationMs) : 0;
    }

    /// <summary>
    /// 可释放的操作跟踪类
    /// </summary>
    private class DisposableOperation : IDisposable
    {
        private readonly Action _onDispose;
        
        public DisposableOperation(Action onDispose)
        {
            _onDispose = onDispose;
        }
        
        public void Dispose()
        {
            _onDispose();
        }
    }
}

/// <summary>
/// Grain调用性能监控扩展方法
/// </summary>
public static class GrainMonitoringExtensions
{
    /// <summary>
    /// 使用性能监控执行Grain操作
    /// </summary>
    public static async Task<TResult> ExecuteWithMonitoringAsync<TResult>(
        this Grain grain,
        PerformanceMonitor monitor,
        Func<Task<TResult>> operation,
        string operationName)
    {
        var grainType = grain.GetType().Name;
        var grainKey = grain.GetPrimaryKeyString();
        
        using (monitor.TrackGrainCall(grainType, grainKey, operationName))
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                // 异常处理在Dispose中完成
                throw;
            }
        }
    }

    /// <summary>
    /// 使用性能监控执行SQL操作
    /// </summary>
    public static async Task<TResult> ExecuteSqlWithMonitoringAsync<TResult>(
        this PerformanceMonitor monitor,
        string sqlType,
        string sql,
        Func<Task<TResult>> operation,
        Dictionary<string, object>? parameters = null)
    {
        using (monitor.TrackSqlQuery(sqlType, sql, parameters))
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                // 异常处理在Dispose中完成
                throw;
            }
        }
    }
    
    /// <summary>
    /// 使用性能监控执行同步Grain操作
    /// </summary>
    public static TResult ExecuteWithMonitoring<TResult>(
        this Grain grain,
        PerformanceMonitor monitor,
        Func<TResult> operation,
        string operationName)
    {
        var grainType = grain.GetType().Name;
        var grainKey = grain.GetPrimaryKeyString();
        
        using (monitor.TrackGrainCall(grainType, grainKey, operationName))
        {
            try
            {
                return operation();
            }
            catch (Exception ex)
            {
                // 异常处理在Dispose中完成
                throw;
            }
        }
    }
    
    /// <summary>
    /// 使用性能监控执行同步SQL操作
    /// </summary>
    public static TResult ExecuteSqlWithMonitoring<TResult>(
        this PerformanceMonitor monitor,
        string sqlType,
        string sql,
        Func<TResult> operation,
        Dictionary<string, object>? parameters = null)
    {
        using (monitor.TrackSqlQuery(sqlType, sql, parameters))
        {
            try
            {
                return operation();
            }
            catch (Exception ex)
            {
                // 异常处理在Dispose中完成
                throw;
            }
        }
    }
}