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
                    TotalRequests = report.TotalRequests,
                    ErrorRequests = report.ErrorRequests,
                    ErrorRate = $"{report.ErrorRate:P2}",
                    TotalMetrics = report.Metrics.Count,
                    ReportTime = report.ReportTime,
                    TopMetrics = report.Metrics
                        .OrderByDescending(m => m.Value.Count)
                        .Take(10)
                        .ToDictionary(m => m.Key, m => new 
                        {
                            Average = m.Value.Average,
                            P95 = m.Value.P95,
                            P99 = m.Value.P99,
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
        
        /// <summary>
        /// 配置告警规则
        /// </summary>
        [HttpPost("alerts/configure")]
        public async Task<ActionResult> ConfigureAlert([FromBody] AlertConfiguration alertConfig)
        {
            try
            {
                var monitor = _clusterClient.GetGrain<ISystemMonitorGrain>("system");
                await monitor.ConfigureAlertAsync(alertConfig);
                
                return Ok(new { message = "告警规则已配置" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"配置失败: {ex.Message}" });
            }
        }
        
        /// <summary>
        /// 获取告警配置
        /// </summary>
        [HttpGet("alerts/config")]
        public async Task<ActionResult<List<AlertConfiguration>>> GetAlertConfigurations()
        {
            try
            {
                var monitor = _clusterClient.GetGrain<ISystemMonitorGrain>("system");
                var configs = await monitor.GetAlertConfigurationsAsync();
                
                return Ok(configs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"获取配置失败: {ex.Message}" });
            }
        }
        
        /// <summary>
        /// 获取当前活动告警
        /// </summary>
        [HttpGet("alerts/active")]
        public async Task<ActionResult<List<AlertInfo>>> GetActiveAlerts()
        {
            try
            {
                var monitor = _clusterClient.GetGrain<ISystemMonitorGrain>("system");
                var healthStatus = await monitor.GetHealthStatusAsync();
                
                return Ok(healthStatus.ActiveAlerts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"获取活动告警失败: {ex.Message}" });
            }
        }
    }
}