using FakeMicro.Interfaces.Monitoring;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FakeMicro.Grains.Monitoring
{
    /// <summary>
    /// 性能监控工具类 - 提供性能测量和指标报告的扩展方法
    /// </summary>
    public static class PerformanceMonitor
    {
        /// <summary>
        /// 测量异步方法的执行时间并自动报告指标
        /// </summary>
        /// <typeparam name="T">方法返回值类型</typeparam>
        /// <param name="monitor">系统监控Grain实例</param>
        /// <param name="operationName">操作名称</param>
        /// <param name="operation">要执行的异步操作</param>
        /// <param name="grainType">可选，Grain类型</param>
        /// <returns>操作的返回结果</returns>
        /// <remarks>
        /// 该方法会：
        /// 1. 测量操作执行时间
        /// 2. 根据操作结果报告响应时间或错误时间指标
        /// 3. 更新请求统计信息
        /// </remarks>
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
                await monitor.ReportRequestStatsAsync(1, 0);
                
                return result;
            }
            catch (Exception)
            {
                stopwatch.Stop();
                await monitor.ReportMetricAsync($"{operationName}.ErrorTime", 
                    stopwatch.ElapsedMilliseconds, grainType);
                await monitor.ReportRequestStatsAsync(1, 1);
                throw;
            }
        }

        /// <summary>
        /// 测量无返回值异步方法的执行时间并自动报告指标
        /// </summary>
        /// <param name="monitor">系统监控Grain实例</param>
        /// <param name="operationName">操作名称</param>
        /// <param name="operation">要执行的异步操作</param>
        /// <param name="grainType">可选，Grain类型</param>
        /// <returns>异步操作结果</returns>
        /// <remarks>
        /// 该方法会：
        /// 1. 测量操作执行时间
        /// 2. 根据操作结果报告响应时间或错误时间指标
        /// 3. 更新请求统计信息
        /// </remarks>
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
                await monitor.ReportRequestStatsAsync(1, 0);
            }
            catch (Exception)
            {
                stopwatch.Stop();
                await monitor.ReportMetricAsync($"{operationName}.ErrorTime", 
                    stopwatch.ElapsedMilliseconds, grainType);
                await monitor.ReportRequestStatsAsync(1, 1);
                throw;
            }
        }
    }
}