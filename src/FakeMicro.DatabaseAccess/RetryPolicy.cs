using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Core;
using Npgsql;
using SqlSugar;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess;

/// <summary>
/// 重试策略类
/// 实现指数退避重试机制，用于数据库操作等可能临时失败的场景
/// </summary>
public class RetryPolicy
{
    private readonly ILogger _logger;
    private readonly int _maxRetries;
    private readonly int _initialDelayMs;
    private readonly int _maxDelayMs;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="maxRetries">最大重试次数</param>
    /// <param name="initialDelayMs">初始延迟时间（毫秒）</param>
    /// <param name="maxDelayMs">最大延迟时间（毫秒）</param>
    public RetryPolicy(ILogger logger, int maxRetries = 3, int initialDelayMs = 100, int maxDelayMs = 2000)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxRetries = maxRetries;
        _initialDelayMs = initialDelayMs;
        _maxDelayMs = maxDelayMs;
    }
    
    /// <summary>
    /// 执行带重试的异步操作并返回结果
    /// </summary>
    public async Task<TResult> ExecuteWithRetryAsync<TResult>(Func<Task<TResult>> operation, string operationName, CancellationToken cancellationToken = default)
    {
        int retries = 0;
        while (true)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await operation();
            }
            catch (Exception ex) when (retries < _maxRetries && IsRetryableException(ex))
            {
                retries++;
                var delay = Math.Min(_initialDelayMs * Math.Pow(2, retries - 1), _maxDelayMs);
                
                _logger.LogWarning(ex, "操作 '{OperationName}' 失败，正在进行第 {RetryCount}/{MaxRetries} 次重试，延迟 {Delay}ms", 
                    operationName, retries, _maxRetries, delay);
                
                await Task.Delay(TimeSpan.FromMilliseconds(delay), cancellationToken);
            }
        }
    }
    
    /// <summary>
    /// 执行带重试的异步操作
    /// </summary>
    public async Task ExecuteWithRetryAsync(Func<Task> operation, string operationName, CancellationToken cancellationToken = default)
    {
        int retries = 0;
        while (true)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await operation();
                return;
            }
            catch (Exception ex) when (retries < _maxRetries && IsRetryableException(ex))
            {
                retries++;
                var delay = Math.Min(_initialDelayMs * Math.Pow(2, retries - 1), _maxDelayMs);
                
                _logger.LogWarning(ex, "操作 '{OperationName}' 失败，正在进行第 {RetryCount}/{MaxRetries} 次重试，延迟 {Delay}ms", 
                    operationName, retries, _maxRetries, delay);
                
                await Task.Delay(TimeSpan.FromMilliseconds(delay), cancellationToken);
            }
        }
    }
    
    /// <summary>
    /// 判断异常是否可重试
    /// </summary>
    public bool IsRetryableException(Exception ex)
    {
        if (ex == null) return false;
        
        // 递归检查内部异常
        if (ex.InnerException != null && IsRetryableException(ex.InnerException))
        {
            return true;
        }
        
        // MongoDB异常处理
        if (ex is MongoException mongoEx)
        {
            // 网络异常、超时异常、连接异常等可重试
            if (mongoEx is MongoConnectionException || 
                mongoEx is TimeoutException || 
                mongoEx is OperationCanceledException ||
                mongoEx is MongoWriteConcernException)
            {
                return true;
            }

            // 检查MongoDB异常消息
            if (ContainsRetryableKeywords(mongoEx.Message))
            {
                return true;
            }
        }
        
        // 连接异常可重试
        if (ex is SqlSugarException sqlEx)
        {
            // 检查常见的连接、超时、死锁关键词
            if (ContainsRetryableKeywords(sqlEx.Message))
            {
                return true;
            }
        }
        
        // 检查PostgreSQL特定的异常类型和错误代码
        if (ex is NpgsqlException npgsqlEx)
        {
            // 检查PostgreSQL SQL状态代码
            var sqlState = npgsqlEx.SqlState;
            if (!string.IsNullOrEmpty(sqlState))
            {
                // 事务相关错误
                if (sqlState.StartsWith("40")) return true;
                // 资源相关错误
                if (sqlState.StartsWith("53")) return true;
                // 死锁错误
                if (sqlState == "40P01") return true;
            }
        }
        
        // 超时异常
        if (ex is TimeoutException) return true;
        
        // 检查消息中的关键词
        return ContainsRetryableKeywords(ex.Message);
    }
    
    /// <summary>
    /// 检查消息中是否包含可重试的关键词
    /// 支持中英文关键词
    /// </summary>
    public bool ContainsRetryableKeywords(string message)
    {
        if (string.IsNullOrEmpty(message)) return false;
        
        // 转换为小写进行匹配
        var lowerMessage = message.ToLower();
        
        // 连接相关关键词
        if (lowerMessage.Contains("connection") || 
            lowerMessage.Contains("connect") || 
            lowerMessage.Contains("连接") ||
            lowerMessage.Contains("无法连接"))
        {
            return true;
        }
        
        // 超时相关关键词
        if (lowerMessage.Contains("timeout") || 
            lowerMessage.Contains("timed out") || 
            lowerMessage.Contains("超时") ||
            lowerMessage.Contains("超时过期"))
        {
             return true;
         }
         
        // 死锁相关关键词
        if (lowerMessage.Contains("deadlock") || 
            lowerMessage.Contains("dead lock") || 
            lowerMessage.Contains("死锁"))
        {
            return true;
        }
        
        // 资源相关关键词
        if (lowerMessage.Contains("resource") || 
            lowerMessage.Contains("resources") || 
            lowerMessage.Contains("资源"))
        {
            return true;
        }
        
        return false;
    }
}