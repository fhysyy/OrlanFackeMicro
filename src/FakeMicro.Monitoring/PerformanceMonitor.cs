using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FakeMicro.Monitoring;

/// <summary>
/// 性能监控服务
/// 提供Orleans环境下SQL性能监控和Grain调用跟踪
/// </summary>
public class PerformanceMonitor
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly ConcurrentDictionary<string, PerformanceMetrics> _metrics = new ConcurrentDictionary<string, PerformanceMetrics>();
    private readonly ConcurrentDictionary<string, long> _activeOperations = new ConcurrentDictionary<string, long>();
    
    // 监控统计信息
    private long _totalGrainCalls = 0;
    private long _totalSqlQueries = 0;
    private long _slowQueries = 0;
    private long _failedCalls = 0;

    /// <summary>
    /// 性能指标类
    /// </summary>
    public class PerformanceMetrics
    {
        public string Name { get; set; } = string.Empty;
        public long CallCount { get; set; }
        public long TotalDurationMs { get; set; }
        public long AverageDurationMs => CallCount > 0 ? TotalDurationMs / CallCount : 0;
        public long FailedCount { get; set; }
        public double SuccessRate => CallCount > 0 ? (1.0 - (double)FailedCount / CallCount) * 100 : 100;
        public DateTime LastCallTime { get; set; }
        public List<long> RecentDurations { get; set; } = new List<long>();
        public long MaxRecentDuration { get; set; }
        public long MinRecentDuration { get; set; }
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 记录Grain调用开始
    /// </summary>
    public IDisposable TrackGrainCall(string grainType, string grainKey, string operationName)
    {
        var operationId = $"{grainType}.{operationName}";
        var stopwatch = Stopwatch.StartNew();
        
        Interlocked.Increment(ref _totalGrainCalls);
        _activeOperations.AddOrUpdate(operationId, 1, (key, value) => value + 1);
        
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
        _activeOperations.AddOrUpdate(operationId, 1, (key, value) => value + 1);
        
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
        
        var metrics = _metrics.GetOrAdd(operationId, key => new PerformanceMetrics { Name = key });
        
        lock (metrics)
        {
            metrics.CallCount++;
            metrics.TotalDurationMs += duration;
            metrics.LastCallTime = DateTime.UtcNow;
            
            // 维护最近10次调用的时长记录
            metrics.RecentDurations.Add(duration);
            if (metrics.RecentDurations.Count > 10)
            {
                metrics.RecentDurations.RemoveAt(0);
            }
            
            if (metrics.RecentDurations.Count > 0)
            {
                metrics.MaxRecentDuration = metrics.RecentDurations.Max();
                metrics.MinRecentDuration = metrics.RecentDurations.Min();
            }
            
            if (exception != null)
            {
                metrics.FailedCount++;
                Interlocked.Increment(ref _failedCalls);
            }
        }
        
        // 记录慢查询（超过1秒）
        if (duration > 1000)
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
        else if (duration > 500)
        {
            _logger.LogInformation("Grain调用完成: {OperationId}, GrainKey={GrainKey}, Duration={Duration}ms", 
                operationId, grainKey, duration);
        }
        else
        {
            _logger.LogDebug("Grain调用完成: {OperationId}, GrainKey={GrainKey}, Duration={Duration}ms", 
                operationId, grainKey, duration);
        }
    }

    /// <summary>
    /// 完成SQL查询跟踪
    /// </summary>
    private void CompleteSqlQuery(string operationId, string sql, Dictionary<string, object>? parameters, Stopwatch stopwatch, Exception? exception)
    {
        stopwatch.Stop();
        var duration = stopwatch.ElapsedMilliseconds;
        
        _activeOperations.AddOrUpdate(operationId, 0, (key, value) => Math.Max(0, value - 1));
        
        var metrics = _metrics.GetOrAdd(operationId, key => new PerformanceMetrics { Name = key });
        
        lock (metrics)
        {
            metrics.CallCount++;
            metrics.TotalDurationMs += duration;
            metrics.LastCallTime = DateTime.UtcNow;
            
            if (exception != null)
            {
                metrics.FailedCount++;
            }
        }
        
        // 记录慢查询（超过500ms）
        if (duration > 500)
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
        _metrics.Clear();
        _activeOperations.Clear();
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
        public Dictionary<string, long> ActiveOperations { get; set; } = new Dictionary<string, long>();
        public Dictionary<string, PerformanceMetrics> Metrics { get; set; } = new Dictionary<string, PerformanceMetrics>();
        
        public double GrainCallSuccessRate => TotalGrainCalls > 0 ? (1.0 - (double)FailedCalls / TotalGrainCalls) * 100 : 100;
        public double SlowQueryRate => TotalSqlQueries > 0 ? (double)SlowQueries / TotalSqlQueries * 100 : 0;
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
}