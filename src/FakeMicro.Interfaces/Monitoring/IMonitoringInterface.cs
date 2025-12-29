using System.Collections.Generic;
using System.Threading.Tasks;

namespace FakeMicro.Interfaces.Monitoring
{
    /// <summary>
    /// 系统监控接口 - 提供性能指标收集、健康状态监控和告警管理功能
    /// </summary>
    /// <remarks>
    /// 该接口定义了系统级别的监控功能，包括：
    /// 1. 性能指标收集（响应时间、内存使用、错误率等）
    /// 2. 系统健康状态检查
    /// 3. 告警规则配置和管理
    /// 4. 性能报告生成
    /// </remarks>
    public interface IMonitoringInterface
    {
        /// <summary>
        /// 报告系统性能指标
        /// </summary>
        /// <param name="metricName">指标名称</param>
        /// <param name="value">指标值</param>
        /// <param name="grainType">指标来源的Grain类型（可选）</param>
        /// <returns>异步操作结果</returns>
        /// <remarks>
        /// 用于实时收集系统运行时性能数据，如：
        /// - 响应时间
        /// - 内存使用情况
        /// - CPU利用率
        /// - 请求处理速率
        /// - 错误率
        /// </remarks>
        Task ReportMetricAsync(string metricName, double value, string grainType = null);

        /// <summary>
        /// 获取系统当前健康状态
        /// </summary>
        /// <returns>包含系统健康状态信息的异步结果</returns>
        /// <remarks>
        /// 健康状态包括：
        /// - 系统整体运行状态
        /// - 内存使用情况
        /// - CPU利用率
        /// - 活动Grain数量
        /// - 错误率
        /// </remarks>
        Task<SystemHealthStatus> GetHealthStatusAsync();

        /// <summary>
        /// 获取系统详细性能指标报告
        /// </summary>
        /// <returns>包含所有系统指标的异步结果</returns>
        /// <remarks>
        /// 性能报告包括：
        /// - 所有已收集指标的统计数据（平均值、最小值、最大值、P95、P99等）
        /// - 系统运行时间
        /// - 请求处理统计
        /// - 错误率分析
        /// - 内存使用趋势
        /// - 活动Grain分布
        /// </remarks>
        Task<SystemMetricsReport> GetMetricsReportAsync();

        /// <summary>
        /// 重置所有已收集的性能指标
        /// </summary>
        /// <returns>异步操作结果</returns>
        /// <remarks>
        /// 此操作会清除所有历史指标数据，重新开始收集。
        /// 通常用于系统维护或测试场景。
        /// </remarks>
        Task ResetMetricsAsync();
    }

    /// <summary>
    /// 系统监控Grain接口 - 提供系统性能指标收集、健康状态监控和告警管理功能
    /// </summary>
    /// <remarks>
    /// 该Grain实现了系统级别的监控功能，包括：
    /// 1. 性能指标收集（响应时间、内存使用、错误率等）
    /// 2. 系统健康状态检查
    /// 3. 告警规则配置和管理
    /// 4. 性能报告生成
    /// </remarks>
    public interface ISystemMonitorGrain : IGrainWithStringKey
    {
        /// <summary>
        /// 报告性能指标数据
        /// </summary>
        /// <param name="metricName">指标名称</param>
        /// <param name="value">指标值</param>
        /// <param name="grainType">可选，指标来源的Grain类型</param>
        /// <returns>异步操作结果</returns>
        Task ReportMetricAsync(string metricName, double value, string grainType = null);

        /// <summary>
        /// 获取系统当前健康状态
        /// </summary>
        /// <returns>包含系统健康状态的异步结果</returns>
        /// <remarks>
        /// 健康状态检查包括：
        /// - 系统运行时间
        /// - 内存使用情况
        /// - 活动Grain数量
        /// - 错误率
        /// - 活动告警情况
        /// </remarks>
        Task<SystemHealthStatus> GetHealthStatusAsync();

        /// <summary>
        /// 获取系统性能指标报告
        /// </summary>
        /// <returns>包含详细性能指标的异步结果</returns>
        /// <remarks>
        /// 指标报告包括：
        /// - 所有收集的指标统计（平均值、最小值、最大值、P95、P99等）
        /// - 系统运行时间
        /// - 请求统计和错误率
        /// - 内存使用情况
        /// - 活动Grain数量
        /// </remarks>
        Task<SystemMetricsReport> GetMetricsReportAsync();

        /// <summary>
        /// 重置所有已收集的指标数据
        /// </summary>
        /// <returns>异步操作结果</returns>
        /// <remarks>
        /// 此操作会清除：
        /// - 所有指标数据
        /// - 活动告警
        /// - 请求统计
        /// </remarks>
        Task ResetMetricsAsync();
        
        /// <summary>
        /// 配置告警规则
        /// </summary>
        /// <param name="alertConfig">告警配置信息</param>
        /// <returns>异步操作结果</returns>
        /// <exception cref="ArgumentNullException">当告警配置为null时抛出</exception>
        Task ConfigureAlertAsync(AlertConfiguration alertConfig);
        
        /// <summary>
        /// 获取当前所有告警配置
        /// </summary>
        /// <returns>包含所有告警配置的异步结果</returns>
        Task<List<AlertConfiguration>> GetAlertConfigurationsAsync();
        
        /// <summary>
        /// 报告请求统计信息
        /// </summary>
        /// <param name="totalRequests">请求总数</param>
        /// <param name="errorRequests">错误请求数</param>
        /// <returns>异步操作结果</returns>
        Task ReportRequestStatsAsync(int totalRequests, int errorRequests);
    }
}