using FakeMicro.Grains.Monitoring;
using FakeMicro.Interfaces.Monitoring;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FakeMicro.Api.Controllers
{
    /// <summary>
    /// 系统监控控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MonitoringController : ControllerBase
    {
        private readonly IClusterClient _clusterClient;

        public MonitoringController(IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        /// <summary>
        /// 获取系统健康状态
        /// </summary>
        [HttpGet("health")]
        public async Task<ActionResult<SystemHealthStatus>> GetHealthStatus()
        {
            try
            {
                var monitor = _clusterClient.GetGrain<ISystemMonitorGrain>("system");
                var status = await monitor.GetHealthStatusAsync();
                
                return Ok(status);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new SystemHealthStatus 
                { 
                    IsHealthy = false, 
                    Status = $"健康检查失败: {ex.Message}" 
                });
            }
        }

        /// <summary>
        /// 获取性能指标报告
        /// </summary>
        [HttpGet("metrics")]
        public async Task<ActionResult<SystemMetricsReport>> GetMetricsReport()
        {
            try
            {
                var monitor = _clusterClient.GetGrain<ISystemMonitorGrain>("system");
                var report = await monitor.GetMetricsReportAsync();
                
                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new SystemMetricsReport 
                { 
                    ReportTime = DateTime.UtcNow,
                    Metrics = new Dictionary<string, MetricStats>()
                });
            }
        }

        /// <summary>
        /// 重置监控指标
        /// </summary>
        [HttpPost("reset")]
        public async Task<ActionResult> ResetMetrics()
        {
            try
            {
                var monitor = _clusterClient.GetGrain<ISystemMonitorGrain>("system");
                await monitor.ResetMetricsAsync();
                
                return Ok(new { message = "指标数据已重置" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"重置失败: {ex.Message}" });
            }
        }

        /// <summary>
        /// 获取系统统计信息
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetSystemStats()
        {
            try
            {
                var monitor = _clusterClient.GetGrain<ISystemMonitorGrain>("system");
                var report = await monitor.GetMetricsReportAsync();
                
                var stats = new
                {
                    Uptime = report.Uptime,
                    MemoryUsage = $"{report.MemoryUsageMB:F2} MB",
                    ActiveGrains = report.ActiveGrains,
                    TotalMetrics = report.Metrics.Count,
                    ReportTime = report.ReportTime,
                    TopMetrics = report.Metrics
                        .OrderByDescending(m => m.Value.Count)
                        .Take(10)
                        .ToDictionary(m => m.Key, m => new 
                        {
                            Average = m.Value.Average,
                            Count = m.Value.Count
                        })
                };
                
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"获取统计信息失败: {ex.Message}" });
            }
        }
    }
}