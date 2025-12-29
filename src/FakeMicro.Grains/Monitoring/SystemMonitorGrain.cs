using FakeMicro.Interfaces.Monitoring;
using FakeMicro.Interfaces.Services;
using Orleans;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;

namespace FakeMicro.Grains.Monitoring
{
    public class SystemMonitorGrain : Grain, ISystemMonitorGrain
    {
        private readonly ILoggerService _logger;
        private readonly ConcurrentDictionary<string, List<double>> _metrics;
        private readonly ConcurrentDictionary<string, DateTime> _lastActivity;
        private readonly ConcurrentDictionary<string, AlertConfiguration> _alertConfigs;
        private readonly ConcurrentDictionary<string, AlertInfo> _activeAlerts;
        private readonly DateTime _startTime;
        private int _totalRequests;
        private int _errorRequests;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志服务</param>
        /// <exception cref="ArgumentNullException">当日志服务为null时抛出</exception>
        public SystemMonitorGrain(ILoggerService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metrics = new ConcurrentDictionary<string, List<double>>();
            _lastActivity = new ConcurrentDictionary<string, DateTime>();
            _alertConfigs = new ConcurrentDictionary<string, AlertConfiguration>();
            _activeAlerts = new ConcurrentDictionary<string, AlertInfo>();
            _startTime = DateTime.UtcNow;
            _totalRequests = 0;
            _errorRequests = 0;
            
            // 初始化默认告警配置
            InitializeDefaultAlerts();
        }

        /// <summary>
        /// 初始化默认告警配置
        /// </summary>
        private void InitializeDefaultAlerts()
        {
            // 高响应时间告警
            _alertConfigs.TryAdd("HighResponseTime", new AlertConfiguration
            {
                MetricName = "ResponseTime",
                Threshold = 1000,
                Level = AlertLevel.Warning,
                Description = "响应时间超过1秒",
                Enabled = true,
                DurationSeconds = 30
            });
            
            // 内存使用过高告警
            _alertConfigs.TryAdd("MemoryUsage", new AlertConfiguration
            {
                MetricName = "MemoryUsage",
                Threshold = 500,
                Level = AlertLevel.Error,
                Description = "内存使用超过500MB",
                Enabled = true,
                DurationSeconds = 60
            });
            
            // 高错误率告警
            _alertConfigs.TryAdd("HighErrorRate", new AlertConfiguration
            {
                MetricName = "ErrorRate",
                Threshold = 0.1,
                Level = AlertLevel.Error,
                Description = "错误率超过10%",
                Enabled = true,
                DurationSeconds = 60
            });
        }

        /// <summary>
        /// Grain激活时的回调方法
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        public override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("系统监控Grain已激活: {GrainId}", this.GetPrimaryKeyString());
            return base.OnActivateAsync(cancellationToken);
        }

        /// <summary>
        /// 报告性能指标
        /// </summary>
        /// <param name="metricName">指标名称</param>
        /// <param name="value">指标值</param>
        /// <param name="grainType">可选，指标来源的Grain类型</param>
        /// <returns>异步操作结果</returns>
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

                // 检查告警条件
                CheckAlertConditions(key, value);

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "报告指标失败: {MetricName}", metricName);
                return Task.CompletedTask; // 监控失败不应该影响主流程
            }
        }

        /// <summary>
        /// 检查告警条件是否满足
        /// </summary>
        /// <param name="metricName">指标名称</param>
        /// <param name="value">指标值</param>
        private void CheckAlertConditions(string metricName, double value)
        {
            foreach (var alertConfig in _alertConfigs.Values.Where(c => c.Enabled && metricName.Contains(c.MetricName)))
            {
                var alertKey = $"{alertConfig.MetricName}_{metricName}";
                
                if (value > alertConfig.Threshold)
                {
                    if (!_activeAlerts.ContainsKey(alertKey))
                    {
                        // 创建新告警
                        var alert = new AlertInfo
                        {
                            AlertId = Guid.NewGuid().ToString(),
                            AlertName = alertConfig.MetricName,
                            Level = alertConfig.Level,
                            Description = alertConfig.Description,
                            TriggerTime = DateTime.UtcNow,
                            Details = new Dictionary<string, string>
                            {
                                { "MetricName", metricName },
                                { "Value", value.ToString() },
                                { "Threshold", alertConfig.Threshold.ToString() }
                            }
                        };
                        
                        _activeAlerts.TryAdd(alertKey, alert);
                        _logger.LogWarning("触发告警: {AlertName} - {Description}", alertConfig.MetricName, alertConfig.Description);
                    }
                    else
                    {
                        // 更新现有告警
                        var alert = _activeAlerts[alertKey];
                        alert.Details["LastValue"] = value.ToString();
                        alert.Details["LastTriggerTime"] = DateTime.UtcNow.ToString();
                    }
                }
                else if (_activeAlerts.ContainsKey(alertKey))
                {
                    // 清除告警
                    _activeAlerts.TryRemove(alertKey, out _);
                    _logger.LogInformation("告警已恢复: {AlertName} - {Description}", alertConfig.MetricName, alertConfig.Description);
                }
            }
        }

        /// <summary>
        /// 获取系统健康状态
        /// </summary>
        /// <returns>系统健康状态</returns>
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

                // 检查内存使用情况
                var memoryUsage = Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0;
                status.Details["MemoryUsage"] = $"{memoryUsage:F2} MB";

                // 检查活动Grain类型数量
                var activeGrainTypes = _lastActivity.Keys
                    .Where(k => (DateTime.UtcNow - _lastActivity[k]).TotalMinutes < 5)
                    .Select(k => k.Split('.')[0])
                    .Distinct()
                    .Count();
                status.Details["ActiveGrainTypes"] = activeGrainTypes.ToString();
                
                // 检查错误率
                var errorRate = _totalRequests > 0 ? (double)_errorRequests / _totalRequests : 0;
                status.Details["ErrorRate"] = $"{errorRate:P2}";

                // 收集活动告警
                status.ActiveAlerts.AddRange(_activeAlerts.Values);

                // 健康检查规则
                if (memoryUsage > 500) // 超过500MB
                {
                    status.IsHealthy = false;
                    status.Status = $"内存使用过高: {memoryUsage:F2}MB";
                }
                else if (errorRate > 0.1) // 错误率超过10%
                {
                    status.IsHealthy = false;
                    status.Status = $"错误率过高: {errorRate:P2}";
                }
                else if (uptime.TotalHours > 24 && activeGrainTypes == 0)
                {
                    status.IsHealthy = false;
                    status.Status = "长时间运行但无活动Grain";
                }
                else if (status.ActiveAlerts.Any(a => a.Level >= AlertLevel.Error))
                {
                    status.IsHealthy = false;
                    status.Status = $"存在 {status.ActiveAlerts.Count(a => a.Level >= AlertLevel.Error)} 个错误级告警";
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

        /// <summary>
        /// 获取性能指标报告
        /// </summary>
        /// <returns>性能指标报告</returns>
        public Task<SystemMetricsReport> GetMetricsReportAsync()
        {
            var report = new SystemMetricsReport
            {
                ReportTime = DateTime.UtcNow,
                Uptime = DateTime.UtcNow - _startTime,
                TotalRequests = _totalRequests,
                ErrorRequests = _errorRequests,
                ErrorRate = _totalRequests > 0 ? (double)_errorRequests / _totalRequests : 0
            };

            try
            {
                // 收集所有指标统计
                foreach (var kvp in _metrics)
                {
                    var values = kvp.Value;
                    if (values.Any())
                    {
                        // 计算P95和P99分位数
                        var sortedValues = values.OrderBy(v => v).ToList();
                        var p95Index = (int)(sortedValues.Count * 0.95);
                        var p99Index = (int)(sortedValues.Count * 0.99);
                        
                        report.Metrics[kvp.Key] = new MetricStats
                        {
                            Average = values.Average(),
                            Min = values.Min(),
                            Max = values.Max(),
                            Count = values.Count,
                            LastUpdate = _lastActivity.GetValueOrDefault(kvp.Key, DateTime.MinValue),
                            P95 = sortedValues[p95Index],
                            P99 = sortedValues[p99Index],
                            RecentValues = sortedValues.Skip(Math.Max(0, sortedValues.Count - 10)).ToList()
                        };
                    }
                }

                // 计算活动Grain数量（5分钟内有活动的）
                report.ActiveGrains = _lastActivity.Count(kvp => 
                    (DateTime.UtcNow - kvp.Value).TotalMinutes < 5);

                // 获取当前内存使用情况
                report.MemoryUsageMB = Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0;

                return Task.FromResult(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成指标报告失败");
                return Task.FromResult(report);
            }
        }

        /// <summary>
        /// 重置指标数据
        /// </summary>
        /// <returns>异步操作结果</returns>
        public Task ResetMetricsAsync()
        {
            _metrics.Clear();
            _lastActivity.Clear();
            _activeAlerts.Clear();
            _totalRequests = 0;
            _errorRequests = 0;
            _logger.LogInformation("指标数据已重置");
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// 配置告警规则
        /// </summary>
        /// <param name="alertConfig">告警配置</param>
        /// <returns>异步操作结果</returns>
        public Task ConfigureAlertAsync(AlertConfiguration alertConfig)
        {
            if (alertConfig == null)
            {
                throw new ArgumentNullException(nameof(alertConfig));
            }
            
            _alertConfigs[alertConfig.MetricName] = alertConfig;
            _logger.LogInformation("已配置告警规则: {MetricName}, 阈值: {Threshold}, 级别: {Level}", 
                alertConfig.MetricName, alertConfig.Threshold, alertConfig.Level);
            
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// 获取告警配置列表
        /// </summary>
        /// <returns>告警配置列表</returns>
        public Task<List<AlertConfiguration>> GetAlertConfigurationsAsync()
        {
            return Task.FromResult(_alertConfigs.Values.ToList());
        }
        
        /// <summary>
        /// 报告请求统计
        /// </summary>
        /// <param name="totalRequests">请求总数</param>
        /// <param name="errorRequests">错误请求数</param>
        /// <returns>异步操作结果</returns>
        public Task ReportRequestStatsAsync(int totalRequests, int errorRequests)
        {
            Interlocked.Add(ref _totalRequests, totalRequests);
            Interlocked.Add(ref _errorRequests, errorRequests);
            return Task.CompletedTask;
        }
    }
}