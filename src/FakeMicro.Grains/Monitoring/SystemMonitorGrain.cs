using FakeMicro.Interfaces.Monitoring;
using FakeMicro.Interfaces.Services;
using Orleans;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FakeMicro.Grains.Monitoring
{
    /// <summary>
    /// 系统监控Grain - 收集和报告系统性能指标
    /// </summary>
    public interface ISystemMonitorGrain : IGrainWithStringKey
    {
        /// <summary>
        /// 报告性能指标
        /// </summary>
        Task ReportMetricAsync(string metricName, double value, string grainType = null);

        /// <summary>
        /// 获取系统健康状态
        /// </summary>
        Task<SystemHealthStatus> GetHealthStatusAsync();

        /// <summary>
        /// 获取性能指标报告
        /// </summary>
        Task<SystemMetricsReport> GetMetricsReportAsync();

        /// <summary>
        /// 重置指标数据
        /// </summary>
        Task ResetMetricsAsync();
    }





    public class SystemMonitorGrain : Grain, ISystemMonitorGrain
    {
        private readonly ILoggerService _logger;
        private readonly ConcurrentDictionary<string, List<double>> _metrics;
        private readonly ConcurrentDictionary<string, DateTime> _lastActivity;
        private readonly DateTime _startTime;

        public SystemMonitorGrain(ILoggerService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metrics = new ConcurrentDictionary<string, List<double>>();
            _lastActivity = new ConcurrentDictionary<string, DateTime>();
            _startTime = DateTime.UtcNow;
        }

        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("系统监控Grain已激活: {GrainId}", this.GetPrimaryKeyString());
            return base.OnActivateAsync(cancellationToken);
        }

        public Task ReportMetricAsync(string metricName, double value, string grainType = null)
        {
            try
            {
                var key = string.IsNullOrEmpty(grainType) ? metricName : $"{grainType}.{metricName}";
                
                var metrics = _metrics.GetOrAdd(key, _ => new List<double>());
                
                // 限制指标数量，避免内存泄漏
                if (metrics.Count > 1000)
                {
                    metrics.RemoveRange(0, metrics.Count - 500);
                }
                
                metrics.Add(value);
                _lastActivity[key] = DateTime.UtcNow;

                // 记录异常指标
                if (value > 1000 && metricName.Contains("ResponseTime"))
                {
                    _logger.LogWarning("检测到异常响应时间: {MetricName} = {Value}ms", key, value);
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "报告指标失败: {MetricName}", metricName);
                return Task.CompletedTask; // 监控失败不应该影响主流程
            }
        }

        public Task<SystemHealthStatus> GetHealthStatusAsync()
        {
            var status = new SystemHealthStatus
            {
                LastCheckTime = DateTime.UtcNow,
                IsHealthy = true,
                Status = "正常"
            };

            try
            {
                // 检查系统运行时间
                var uptime = DateTime.UtcNow - _startTime;
                status.Details["Uptime"] = uptime.ToString();

                // 检查内存使用情况（简化实现）
                var memoryUsage = Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0;
                status.Details["MemoryUsage"] = $"{memoryUsage:F2} MB";

                // 检查活动Grain数量
                var activeGrainTypes = _lastActivity.Keys
                    .Where(k => (DateTime.UtcNow - _lastActivity[k]).TotalMinutes < 5)
                    .Select(k => k.Split('.')[0])
                    .Distinct()
                    .Count();
                status.Details["ActiveGrainTypes"] = activeGrainTypes.ToString();

                // 健康检查规则
                if (memoryUsage > 500) // 超过500MB
                {
                    status.IsHealthy = false;
                    status.Status = $"内存使用过高: {memoryUsage:F2}MB";
                }
                else if (uptime.TotalHours > 24 && activeGrainTypes == 0)
                {
                    status.IsHealthy = false;
                    status.Status = "长时间运行但无活动Grain";
                }

                return Task.FromResult(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取健康状态失败");
                status.IsHealthy = false;
                status.Status = $"健康检查失败: {ex.Message}";
                return Task.FromResult(status);
            }
        }

        public Task<SystemMetricsReport> GetMetricsReportAsync()
        {
            var report = new SystemMetricsReport
            {
                ReportTime = DateTime.UtcNow,
                Uptime = DateTime.UtcNow - _startTime
            };

            try
            {
                // 收集所有指标统计
                foreach (var kvp in _metrics)
                {
                    var values = kvp.Value;
                    if (values.Any())
                    {
                        report.Metrics[kvp.Key] = new MetricStats
                        {
                            Average = values.Average(),
                            Min = values.Min(),
                            Max = values.Max(),
                            Count = values.Count,
                            LastUpdate = _lastActivity.GetValueOrDefault(kvp.Key, DateTime.MinValue)
                        };
                    }
                }

                // 计算活动Grain数量
                report.ActiveGrains = _lastActivity.Count(kvp => 
                    (DateTime.UtcNow - kvp.Value).TotalMinutes < 5);

                // 获取内存使用情况
                report.MemoryUsageMB = Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0;

                return Task.FromResult(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成指标报告失败");
                return Task.FromResult(report);
            }
        }

        public Task ResetMetricsAsync()
        {
            _metrics.Clear();
            _lastActivity.Clear();
            _logger.LogInformation("指标数据已重置");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 性能监控工具类
    /// </summary>
    public static class PerformanceMonitor
    {
        /// <summary>
        /// 测量方法执行时间并报告指标
        /// </summary>
        public static async Task<T> MeasureAsync<T>(this ISystemMonitorGrain monitor, 
            string operationName, Func<Task<T>> operation, string grainType = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await operation();
                stopwatch.Stop();
                
                await monitor.ReportMetricAsync($"{operationName}.ResponseTime", 
                    stopwatch.ElapsedMilliseconds, grainType);
                
                return result;
            }
            catch (Exception)
            {
                stopwatch.Stop();
                await monitor.ReportMetricAsync($"{operationName}.ErrorTime", 
                    stopwatch.ElapsedMilliseconds, grainType);
                throw;
            }
        }

        /// <summary>
        /// 测量无返回值方法的执行时间
        /// </summary>
        public static async Task MeasureAsync(this ISystemMonitorGrain monitor,
            string operationName, Func<Task> operation, string grainType = null)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await operation();
                stopwatch.Stop();
                
                await monitor.ReportMetricAsync($"{operationName}.ResponseTime", 
                    stopwatch.ElapsedMilliseconds, grainType);
            }
            catch (Exception)
            {
                stopwatch.Stop();
                await monitor.ReportMetricAsync($"{operationName}.ErrorTime", 
                    stopwatch.ElapsedMilliseconds, grainType);
                throw;
            }
        }
    }
}