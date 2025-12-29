using System.Diagnostics.Metrics;
using FakeMicro.Monitoring;
using Orleans.Runtime;

namespace FakeMicro.Monitoring.OpenTelemetry;

/// <summary>
/// Orleans监控仪表化 - 将Orleans性能指标导出到OpenTelemetry
/// </summary>
public class OrleansMonitoringInstrumentation : IDisposable
{
    private readonly Meter _meter;
    private readonly PerformanceMonitor _performanceMonitor;
    private readonly Timer _metricsCollectionTimer;
    
    // 指标定义
    private readonly Counter<long> _totalGrainCallsCounter;
    private readonly Counter<long> _totalSqlQueriesCounter;
    private readonly Counter<long> _slowQueriesCounter;
    private readonly Counter<long> _failedCallsCounter;
    private readonly Counter<long> _totalAlertsCounter;
    private readonly Gauge<long> _currentConcurrentCallsGauge;
    private readonly Gauge<long> _peakConcurrentCallsGauge;
    private readonly Histogram<double> _grainCallDurationHistogram;
    private readonly Histogram<double> _sqlQueryDurationHistogram;
    
    // 数据库连接池指标
    private readonly Gauge<long> _postgreSqlActiveConnectionsGauge;
    private readonly Gauge<long> _postgreSqlIdleConnectionsGauge;
    private readonly Gauge<long> _postgreSqlMaxConnectionsGauge;
    private readonly Gauge<long> _mongoDbActiveConnectionsGauge;
    private readonly Gauge<long> _mongoDbIdleConnectionsGauge;
    private readonly Gauge<long> _mongoDbMaxConnectionsGauge;
    
    public OrleansMonitoringInstrumentation(PerformanceMonitor performanceMonitor)
    {
        _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
        
        // 创建仪表
        _meter = new Meter("FakeMicro.Orleans.Monitoring", "1.0.0");
        
        // 注册指标
        _totalGrainCallsCounter = _meter.CreateCounter<long>("orleans.grain.calls.total", "calls", "Total number of grain calls");
        _totalSqlQueriesCounter = _meter.CreateCounter<long>("orleans.sql.queries.total", "queries", "Total number of SQL queries");
        _slowQueriesCounter = _meter.CreateCounter<long>("orleans.sql.queries.slow", "queries", "Number of slow SQL queries");
        _failedCallsCounter = _meter.CreateCounter<long>("orleans.calls.failed", "calls", "Number of failed grain calls and SQL queries");
        _totalAlertsCounter = _meter.CreateCounter<long>("orleans.alerts.total", "alerts", "Total number of alerts generated");
        _currentConcurrentCallsGauge = _meter.CreateGauge<long>("orleans.calls.concurrent.current", "calls", "Current number of concurrent calls");
        _peakConcurrentCallsGauge = _meter.CreateGauge<long>("orleans.calls.concurrent.peak", "calls", "Peak number of concurrent calls");
        _grainCallDurationHistogram = _meter.CreateHistogram<double>("orleans.grain.call.duration", "ms", "Duration of grain calls");
        _sqlQueryDurationHistogram = _meter.CreateHistogram<double>("orleans.sql.query.duration", "ms", "Duration of SQL queries");
        
        // 注册数据库连接池指标
        _postgreSqlActiveConnectionsGauge = _meter.CreateGauge<long>("database.postgresql.connections.active", "connections", "Current number of active PostgreSQL connections");
        _postgreSqlIdleConnectionsGauge = _meter.CreateGauge<long>("database.postgresql.connections.idle", "connections", "Current number of idle PostgreSQL connections");
        _postgreSqlMaxConnectionsGauge = _meter.CreateGauge<long>("database.postgresql.connections.max", "connections", "Maximum number of PostgreSQL connections allowed");
        _mongoDbActiveConnectionsGauge = _meter.CreateGauge<long>("database.mongodb.connections.active", "connections", "Current number of active MongoDB connections");
        _mongoDbIdleConnectionsGauge = _meter.CreateGauge<long>("database.mongodb.connections.idle", "connections", "Current number of idle MongoDB connections");
        _mongoDbMaxConnectionsGauge = _meter.CreateGauge<long>("database.mongodb.connections.max", "connections", "Maximum number of MongoDB connections allowed");
        
        // 定期收集指标
        _metricsCollectionTimer = new Timer(CollectMetrics, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
    }
    
    private void CollectMetrics(object state)
    {
        try
        {
            var report = _performanceMonitor.GetPerformanceReport();
            
            // 更新计数器
            _totalGrainCallsCounter.Add(report.TotalGrainCalls);
            _totalSqlQueriesCounter.Add(report.TotalSqlQueries);
            _slowQueriesCounter.Add(report.SlowQueries);
            _failedCallsCounter.Add(report.FailedCalls);
            _totalAlertsCounter.Add(report.TotalAlerts);
            
            // 更新仪表
            _currentConcurrentCallsGauge.Record(report.CurrentConcurrentCalls);
            _peakConcurrentCallsGauge.Record(report.PeakConcurrentCalls);
            
            // 记录直方图数据
            foreach (var metric in report.Metrics)
            {
                if (metric.Key.StartsWith("SQL."))
                {
                    // SQL查询指标
                    _sqlQueryDurationHistogram.Record(metric.Value.AverageDurationMs, 
                        new KeyValuePair<string, object>("query_type", metric.Key.Substring(4)));
                }
                else
                {
                    // Grain调用指标
                    _grainCallDurationHistogram.Record(metric.Value.AverageDurationMs, 
                        new KeyValuePair<string, object>("grain_operation", metric.Key));
                }
            }
            
            // 更新数据库连接池指标
            _postgreSqlActiveConnectionsGauge.Record(report.PostgreSqlActiveConnections);
            _postgreSqlIdleConnectionsGauge.Record(report.PostgreSqlIdleConnections);
            _postgreSqlMaxConnectionsGauge.Record(report.PostgreSqlMaxConnections);
            _mongoDbActiveConnectionsGauge.Record(report.MongoDbActiveConnections);
            _mongoDbIdleConnectionsGauge.Record(report.MongoDbIdleConnections);
            _mongoDbMaxConnectionsGauge.Record(report.MongoDbMaxConnections);
        }
        catch (Exception ex)
        {
            // 记录错误但不影响主流程
            Console.WriteLine($"Error collecting Orleans metrics: {ex.Message}");
        }
    }
    
    public void Dispose()
    {
        _metricsCollectionTimer?.Dispose();
        _meter?.Dispose();
    }
}