using FakeMicro.Utilities.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FakeMicro.Api.Services
{
    /// <summary>
    /// 异常行为检测服务
    /// </summary>
    public class AnomalyDetectionService
    {
        private readonly AnomalyDetectionConfig _config;
        private readonly ILogger<AnomalyDetectionService> _logger;
        
        // 存储每个IP的失败尝试记录
        private readonly ConcurrentDictionary<string, List<DateTime>> _failedAttempts = new();
        
        // 存储被阻止的IP地址和阻止时间
        private readonly ConcurrentDictionary<string, DateTime> _blockedIps = new();
        
        // 存储每个IP的请求计数
        private readonly ConcurrentDictionary<string, (int Count, DateTime ResetTime)> _requestCounts = new();

        public AnomalyDetectionService(
            IOptions<AnomalyDetectionConfig> config,
            ILogger<AnomalyDetectionService> logger)
        {
            _config = config.Value;
            _logger = logger;
        }

        /// <summary>
        /// 检查IP是否被阻止
        /// </summary>
        public bool IsIpBlocked(string ipAddress)
        {
            if (!_config.Enabled || !_config.BlockSuspiciousIps)
                return false;

            if (_blockedIps.TryGetValue(ipAddress, out var blockTime))
            {
                // 检查阻止时间是否已过期
                if (DateTime.UtcNow > blockTime)
                {
                    _blockedIps.TryRemove(ipAddress, out _);
                    _logger.LogInformation("Removed IP {IpAddress} from block list (expired)", ipAddress);
                    return false;
                }
                
                _logger.LogWarning("Blocked request from IP {IpAddress} until {BlockTime}", 
                    ipAddress, blockTime.ToString("O"));
                return true;
            }

            return false;
        }

        /// <summary>
        /// 记录失败尝试
        /// </summary>
        public void RecordFailedAttempt(string ipAddress, string reason)
        {
            if (!_config.Enabled)
                return;

            var now = DateTime.UtcNow;
            
            // 获取或创建该IP的失败尝试列表
            var attempts = _failedAttempts.GetOrAdd(ipAddress, _ => new List<DateTime>());
            
            // 添加当前失败记录
            lock (attempts)
            {
                attempts.Add(now);
                
                // 清理过期的记录
                CleanupExpiredAttempts(attempts, now);
                
                // 检查是否超过阈值
                CheckAndBlockIpIfNeeded(ipAddress, attempts, now, reason);
            }
        }

        /// <summary>
        /// 记录正常请求
        /// </summary>
        public bool RecordRequest(string ipAddress)
        {
            if (!_config.Enabled)
                return true;

            var now = DateTime.UtcNow;
            
            // 先检查IP是否被阻止
            if (IsIpBlocked(ipAddress))
                return false;

            // 更新请求计数
            var counter = _requestCounts.AddOrUpdate(
                ipAddress,
                _ => (1, now.AddMinutes(1)),
                (_, existing) =>
                {
                    // 如果重置时间已过，重置计数
                    if (now > existing.ResetTime)
                        return (1, now.AddMinutes(1));
                    
                    // 否则增加计数
                    return (existing.Count + 1, existing.ResetTime);
                });

            // 检查是否超过请求速率限制
            if (counter.Count > _config.MaxRequestsPerMinute)
            {
                BlockIp(ipAddress, "Rate limit exceeded");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查并在必要时阻止IP
        /// </summary>
        private void CheckAndBlockIpIfNeeded(string ipAddress, List<DateTime> attempts, DateTime now, string reason)
        {
            // 计算短时间窗口内的失败次数
            var shortWindowCount = attempts.Count(a => now - a <= TimeSpan.FromSeconds(_config.ShortTimeWindowSeconds));
            
            // 计算长时间窗口内的失败次数
            var longWindowCount = attempts.Count(a => now - a <= TimeSpan.FromSeconds(_config.LongTimeWindowSeconds));
            
            // 如果超过任一阈值，则阻止IP
            if (shortWindowCount >= _config.FailedAttemptsThreshold || 
                longWindowCount >= _config.LongFailedAttemptsThreshold)
            {
                BlockIp(ipAddress, reason);
            }
        }

        /// <summary>
        /// 阻止IP地址
        /// </summary>
        private void BlockIp(string ipAddress, string reason)
        {
            var blockUntil = DateTime.UtcNow.AddSeconds(_config.BlockDurationSeconds);
            _blockedIps[ipAddress] = blockUntil;
            
            if (_config.LogAnomalies)
            {
                _logger.LogWarning("Blocking IP {IpAddress} until {BlockUntil} for reason: {Reason}",
                    ipAddress, blockUntil.ToString("O"), reason);
            }
        }

        /// <summary>
        /// 清理过期的失败尝试记录
        /// </summary>
        private void CleanupExpiredAttempts(List<DateTime> attempts, DateTime now)
        {
            // 只保留长时间窗口内的记录
            var cutoffTime = now - TimeSpan.FromSeconds(_config.LongTimeWindowSeconds);
            attempts.RemoveAll(a => a < cutoffTime);
        }

        /// <summary>
        /// 记录可疑行为
        /// </summary>
        public void RecordSuspiciousActivity(string ipAddress, string activityType, string details = null)
        {
            if (!_config.Enabled || !_config.LogAnomalies)
                return;

            _logger.LogWarning("Suspicious activity detected - IP: {IpAddress}, Type: {ActivityType}, Details: {Details}",
                ipAddress, activityType, details);
        }

        /// <summary>
        /// 清理过期的请求计数和阻止记录（可以定期调用）
        /// </summary>
        public void CleanupExpiredData()
        {
            var now = DateTime.UtcNow;
            
            // 清理过期的阻止记录
            foreach (var ip in _blockedIps.Keys.ToList())
            {
                if (_blockedIps.TryGetValue(ip, out var blockTime) && now > blockTime)
                {
                    _blockedIps.TryRemove(ip, out _);
                }
            }
            
            // 清理过期的请求计数
            foreach (var ip in _requestCounts.Keys.ToList())
            {
                if (_requestCounts.TryGetValue(ip, out var counter) && now > counter.ResetTime)
                {
                    _requestCounts.TryRemove(ip, out _);
                }
            }
        }
    }
}