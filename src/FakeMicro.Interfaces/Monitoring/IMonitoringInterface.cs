using System.Threading.Tasks;

namespace FakeMicro.Interfaces.Monitoring
{
    /// <summary>
    /// 监控服务接口
    /// </summary>
    public interface IMonitoringInterface
    {
        /// <summary>
        /// 获取系统健康状态
        /// </summary>
        Task<SystemHealthStatus> GetHealthStatusAsync();

        /// <summary>
        /// 获取性能指标报告
        /// </summary>
        Task<SystemMetricsReport> GetMetricsReportAsync();

        /// <summary>
        /// 重置监控指标
        /// </summary>
        Task ResetMetricsAsync();

        /// <summary>
        /// 报告性能指标
        /// </summary>
        Task ReportMetricAsync(string metricName, double value, string grainType = null);
    }



   
}