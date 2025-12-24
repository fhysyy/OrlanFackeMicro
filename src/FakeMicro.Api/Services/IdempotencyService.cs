using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using FakeMicro.DatabaseAccess.Entities;
using FakeMicro.DatabaseAccess.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System;

namespace FakeMicro.Api.Services;

/// <summary>
/// 幂等性服务
/// 用于管理API请求的幂等性
/// </summary>
public class IdempotencyService
{
    private readonly IIdempotentRequestRepository _repository;
    private readonly ILogger<IdempotencyService> _logger;
    private readonly TimeSpan _defaultExpirationTime = TimeSpan.FromHours(24);

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="repository">幂等性请求仓储</param>
    /// <param name="logger">日志记录器</param>
    public IdempotencyService(IIdempotentRequestRepository repository, ILogger<IdempotencyService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 获取或创建幂等性请求记录
    /// </summary>
    /// <param name="idempotencyKey">幂等性键</param>
    /// <param name="userId">用户ID</param>
    /// <param name="httpMethod">HTTP方法</param>
    /// <param name="requestPath">请求路径</param>
    /// <returns>幂等性请求记录，如果不存在则返回null</returns>
    public async Task<IdempotentRequest?> GetIdempotentRequestAsync(string idempotencyKey, long? userId, string httpMethod, string requestPath)
    {
        try
        {
            var existingRequest = await _repository.GetByIdempotencyKeyAsync(idempotencyKey, userId, httpMethod, requestPath);
            if (existingRequest != null)
            {
                _logger.LogInformation("找到已存在的幂等性请求记录: {IdempotencyKey}", idempotencyKey);
                return existingRequest;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取幂等性请求记录失败: {IdempotencyKey}", idempotencyKey);
            // 这里不抛出异常，而是让请求继续处理，保证服务可用性
            return null;
        }
    }

    /// <summary>
    /// 保存幂等性请求响应
    /// </summary>
    /// <param name="idempotencyKey">幂等性键</param>
    /// <param name="userId">用户ID</param>
    /// <param name="httpMethod">HTTP方法</param>
    /// <param name="requestPath">请求路径</param>
    /// <param name="statusCode">响应状态码</param>
    /// <param name="responseHeaders">响应头</param>
    /// <param name="responseBody">响应内容</param>
    /// <returns>保存结果</returns>
    public async Task<bool> SaveIdempotentResponseAsync(
        string idempotencyKey, 
        long? userId, 
        string httpMethod, 
        string requestPath, 
        int statusCode, 
        IHeaderDictionary responseHeaders, 
        string responseBody)
    {
        try
        {
            // 只缓存成功响应（状态码<500）
            if (statusCode >= 500)
            {
                return false;
            }

            // 序列化响应头
            var headersDict = new Dictionary<string, string>();
            foreach (var header in responseHeaders)
            {
                headersDict[header.Key] = header.Value;
            }
            var headersJson = JsonConvert.SerializeObject(headersDict);

            // 创建幂等性请求记录
            var request = new IdempotentRequest
            {
                IdempotencyKey = idempotencyKey,
                UserId = userId,
                HttpMethod = httpMethod,
                RequestPath = requestPath,
                StatusCode = statusCode,
                ResponseHeaders = headersJson,
                ResponseBody = responseBody,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.Add(_defaultExpirationTime)
            };

            await _repository.AddAsync(request);
            _logger.LogInformation("幂等性请求记录已保存: {IdempotencyKey}", idempotencyKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存幂等性请求记录失败: {IdempotencyKey}", idempotencyKey);
            // 这里不抛出异常，保证服务可用性
            return false;
        }
    }

    /// <summary>
    /// 清理过期的幂等性请求记录
    /// </summary>
    /// <returns>清理的记录数</returns>
    public async Task<int> CleanupExpiredRequestsAsync()
    {
        try
        {
            int deletedCount = await _repository.DeleteExpiredAsync();
            if (deletedCount > 0)
            {
                _logger.LogInformation("已清理过期幂等性请求记录: {Count}条", deletedCount);
            }
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理过期幂等性请求记录失败");
            return 0;
        }
    }
}
