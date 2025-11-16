using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FakeMicro.DatabaseAccess
{
    /// <summary>
    /// 数据库性能监控器
    /// 提供数据库操作性能监控和统计
    /// </summary>
    public class DatabasePerformanceMonitor
    {
        private readonly ILogger<DatabasePerformanceMonitor> _logger;
        private readonly List<DatabaseOperation> _operations;
        private readonly object _lockObject = new object();
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public DatabasePerformanceMonitor(ILogger<DatabasePerformanceMonitor> logger)
        {
            _logger = logger;
            _operations = new List<DatabaseOperation>();
        }
        
        /// <summary>
        /// 记录数据库操作
        /// </summary>
        public void RecordOperation(string operationName, long elapsedMilliseconds, bool success)
        {
            var operation = new DatabaseOperation
            {
                OperationName = operationName,
                ElapsedMilliseconds = elapsedMilliseconds,
                Timestamp = DateTime.UtcNow,
                Success = success
            };
            
            lock (_lockObject)
            {
                _operations.Add(operation);
                
                // 保持最近1000条记录
                if (_operations.Count > 1000)
                {
                    _operations.RemoveAt(0);
                }
            }
            
            // 记录性能日志
            if (elapsedMilliseconds > 1000)
            {
                _logger.LogWarning("慢查询检测: {OperationName} 耗时 {ElapsedMs}ms", 
                    operationName, elapsedMilliseconds);
            }
        }
        
        /// <summary>
        /// 获取性能统计
        /// </summary>
        public DatabasePerformanceStats GetPerformanceStats(TimeSpan timeRange)
        {
            var cutoffTime = DateTime.UtcNow - timeRange;
            
            lock (_lockObject)
            {
                var recentOperations = _operations
                    .Where(op => op.Timestamp >= cutoffTime)
                    .ToList();
                
                if (recentOperations.Count == 0)
                {
                    return new DatabasePerformanceStats
                    {
                        TotalOperations = 0,
                        AverageResponseTime = TimeSpan.Zero,
                        ErrorRate = 0.0,
                        SlowOperations = 0
                    };
                }
                
                var totalOperations = recentOperations.Count;
                var successfulOperations = recentOperations.Count(op => op.Success);
                var slowOperations = recentOperations.Count(op => op.ElapsedMilliseconds > 1000);
                var averageResponseTime = TimeSpan.FromMilliseconds(
                    recentOperations.Average(op => op.ElapsedMilliseconds));
                var errorRate = (double)(totalOperations - successfulOperations) / totalOperations;
                
                return new DatabasePerformanceStats
                {
                    TotalOperations = totalOperations,
                    AverageResponseTime = averageResponseTime,
                    ErrorRate = errorRate,
                    SlowOperations = slowOperations
                };
            }
        }
        
        /// <summary>
        /// 获取慢查询列表
        /// </summary>
        public List<DatabaseOperation> GetSlowQueries(TimeSpan timeRange, long thresholdMs = 1000)
        {
            var cutoffTime = DateTime.UtcNow - timeRange;
            
            lock (_lockObject)
            {
                return _operations
                    .Where(op => op.Timestamp >= cutoffTime && op.ElapsedMilliseconds > thresholdMs)
                    .OrderByDescending(op => op.ElapsedMilliseconds)
                    .ToList();
            }
        }
        
        /// <summary>
        /// 清除历史记录
        /// </summary>
        public void ClearHistory()
        {
            lock (_lockObject)
            {
                _operations.Clear();
            }
        }
    }
    
    /// <summary>
    /// 数据库操作记录
    /// </summary>
    public class DatabaseOperation
    {
        public string OperationName { get; set; } = string.Empty;
        public long ElapsedMilliseconds { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
    }
    
    /// <summary>
    /// 数据库性能统计
    /// </summary>
    public class DatabasePerformanceStats
    {
        public int TotalOperations { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public double ErrorRate { get; set; }
        public int SlowOperations { get; set; }
    }
}