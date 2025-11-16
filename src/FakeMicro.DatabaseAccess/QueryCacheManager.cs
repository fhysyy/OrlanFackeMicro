using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess
{
    /// <summary>
    /// 查询缓存管理器
    /// 提供高效的数据库查询结果缓存功能
    /// </summary>
    public class QueryCacheManager : IQueryCacheManager
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<QueryCacheManager> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly int _defaultCacheMinutes = 5;
        private readonly int _maxCacheMinutes = 60;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="memoryCache">内存缓存</param>
        /// <param name="logger">日志记录器</param>
        public QueryCacheManager(IMemoryCache memoryCache, ILogger<QueryCacheManager> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
            
            // 配置JSON序列化选项
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
            };
            
            _logger.LogInformation("查询缓存管理器初始化完成");
        }

        /// <summary>
        /// 获取缓存数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="cacheKey">缓存键</param>
        /// <returns>缓存的数据，如果不存在返回null</returns>
        public T? Get<T>(string cacheKey)
        {
            if (string.IsNullOrEmpty(cacheKey))
                throw new ArgumentNullException(nameof(cacheKey));

            try
            {
                if (_memoryCache.TryGetValue(cacheKey, out T? value))
                {
                    _logger.LogDebug("缓存命中: {CacheKey}, 类型: {Type}", cacheKey, typeof(T).Name);
                    return value;
                }
                
                _logger.LogDebug("缓存未命中: {CacheKey}", cacheKey);
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取缓存失败: {CacheKey}", cacheKey);
                return default;
            }
        }

        /// <summary>
        /// 异步获取缓存数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="cacheKey">缓存键</param>
        /// <returns>缓存的数据，如果不存在返回null</returns>
        public Task<T?> GetAsync<T>(string cacheKey)
        {
            return Task.FromResult(Get<T>(cacheKey));
        }

        /// <summary>
        /// 设置缓存数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="cacheKey">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="cacheMinutes">缓存时间（分钟）</param>
        public void Set<T>(string cacheKey, T value, int cacheMinutes = 0)
        {
            if (string.IsNullOrEmpty(cacheKey))
                throw new ArgumentNullException(nameof(cacheKey));
            
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            try
            {
                // 验证和调整缓存时间
                cacheMinutes = Math.Max(0, Math.Min(cacheMinutes > 0 ? cacheMinutes : _defaultCacheMinutes, _maxCacheMinutes));
                
                // 创建缓存项配置
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(cacheMinutes))
                    .SetSlidingExpiration(TimeSpan.FromMinutes(Math.Min(cacheMinutes / 2, 10))) // 滑动过期时间为绝对过期时间的一半，最多10分钟
                    .RegisterPostEvictionCallback(OnCacheEvicted);
                
                // 设置缓存
                _memoryCache.Set(cacheKey, value, cacheEntryOptions);
                
                _logger.LogDebug("缓存设置成功: {CacheKey}, 类型: {Type}, 缓存时间: {Minutes}分钟", 
                    cacheKey, typeof(T).Name, cacheMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置缓存失败: {CacheKey}", cacheKey);
                // 缓存失败不应影响主流程，记录错误后继续
            }
        }

        /// <summary>
        /// 异步设置缓存数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="cacheKey">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="cacheMinutes">缓存时间（分钟）</param>
        public Task SetAsync<T>(string cacheKey, T value, int cacheMinutes = 0)
        {
            Set(cacheKey, value, cacheMinutes);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 获取或创建缓存数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="cacheKey">缓存键</param>
        /// <param name="factory">缓存值工厂函数</param>
        /// <param name="cacheMinutes">缓存时间（分钟）</param>
        /// <returns>缓存或新创建的数据</returns>
        public T GetOrCreate<T>(string cacheKey, Func<T> factory, int cacheMinutes = 0)
        {
            if (string.IsNullOrEmpty(cacheKey))
                throw new ArgumentNullException(nameof(cacheKey));
            
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            // 先尝试获取缓存
            var cachedValue = Get<T>(cacheKey);
            if (cachedValue != null)
            {
                return cachedValue;
            }
            
            // 缓存未命中，执行工厂函数获取数据
            var value = factory();
            if (value != null)
            {
                // 设置缓存
                Set(cacheKey, value, cacheMinutes);
            }
            
            return value;
        }

        /// <summary>
        /// 异步获取或创建缓存数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="cacheKey">缓存键</param>
        /// <param name="factory">缓存值工厂函数</param>
        /// <param name="cacheMinutes">缓存时间（分钟）</param>
        /// <returns>缓存或新创建的数据</returns>
        public async Task<T> GetOrCreateAsync<T>(string cacheKey, Func<Task<T>> factory, int cacheMinutes = 0)
        {
            if (string.IsNullOrEmpty(cacheKey))
                throw new ArgumentNullException(nameof(cacheKey));
            
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            // 先尝试获取缓存
            var cachedValue = await GetAsync<T>(cacheKey);
            if (cachedValue != null)
            {
                return cachedValue;
            }
            
            // 缓存未命中，执行异步工厂函数获取数据
            var value = await factory();
            if (value != null)
            {
                // 设置缓存
                await SetAsync(cacheKey, value, cacheMinutes);
            }
            
            return value;
        }

        /// <summary>
        /// 移除缓存
        /// </summary>
        /// <param name="cacheKey">缓存键</param>
        public void Remove(string cacheKey)
        {
            if (string.IsNullOrEmpty(cacheKey))
                throw new ArgumentNullException(nameof(cacheKey));

            try
            {
                _memoryCache.Remove(cacheKey);
                _logger.LogDebug("缓存移除成功: {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除缓存失败: {CacheKey}", cacheKey);
            }
        }

        /// <summary>
        /// 异步移除缓存
        /// </summary>
        /// <param name="cacheKey">缓存键</param>
        public Task RemoveAsync(string cacheKey)
        {
            Remove(cacheKey);
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// 移除实体相关的所有缓存
        /// </summary>
        /// <param name="entityType">实体类型</param>
        public Task RemoveEntityCacheAsync(Type entityType)
        {
            if (entityType == null)
                throw new ArgumentNullException(nameof(entityType));
                
            // 由于MemoryCache没有公开所有缓存键的方法
            // 这里实现一个简化版本，实际项目中可能需要使用自定义的缓存键跟踪机制
            string entityTypeName = entityType.Name;
            
            _logger.LogInformation("尝试移除实体类型 {EntityTypeName} 的相关缓存", entityTypeName);
            
            // 在真实项目中，这里应该使用一个更复杂的机制来跟踪和删除特定实体类型的缓存
            // 例如，可以使用分布式缓存或自定义的缓存键管理
            return Task.CompletedTask;
        }

        /// <summary>
        /// 生成查询缓存键
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="operationName">操作名称</param>
        /// <param name="parameters">参数</param>
        /// <returns>缓存键</returns>
        public string GenerateCacheKey<T>(string operationName, params object[] parameters)
        {
            var typeName = typeof(T).FullName ?? typeof(T).Name;
            var parameterValues = JsonSerializer.Serialize(parameters, _jsonOptions);
            
            return $"Query:{typeName}:{operationName}:{parameterValues}";
        }

        /// <summary>
        /// 为条件查询生成缓存键
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="predicate">查询条件</param>
        /// <param name="operationName">操作名称</param>
        /// <returns>缓存键</returns>
        public string GenerateConditionCacheKey<T>(Expression<Func<T, bool>> predicate, string operationName = "ByCondition")
        {
            var typeName = typeof(T).FullName ?? typeof(T).Name;
            var predicateString = predicate.ToString();
            
            // 提取表达式中的常量值以确保缓存键的唯一性
            var constants = ExtractConstants(predicate);
            var constantsString = JsonSerializer.Serialize(constants, _jsonOptions);
            
            return $"Query:{typeName}:{operationName}:{predicateString}:{constantsString}";
        }

        /// <summary>
        /// 为分页查询生成缓存键
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="orderBy">排序字段</param>
        /// <param name="isDescending">是否降序</param>
        /// <param name="predicate">查询条件（可选）</param>
        /// <returns>缓存键</returns>
        public string GeneratePagedCacheKey<T>(int pageNumber, int pageSize, 
            Expression<Func<T, object>>? orderBy = null, bool isDescending = false, 
            Expression<Func<T, bool>>? predicate = null)
        {
            var baseKey = GenerateCacheKey<T>("Paged", pageNumber, pageSize, isDescending);
            
            if (orderBy != null)
            {
                baseKey += $":Order:{orderBy.ToString()}";
            }
            
            if (predicate != null)
            {
                baseKey += $":{GenerateConditionCacheKey(predicate, "")}";
            }
            
            return baseKey;
        }

        /// <summary>
        /// 清除指定类型的所有缓存
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        public void ClearTypeCache<T>()
        {
            // 注意：MemoryCache不支持直接按模式清除
            // 在实际应用中，可能需要维护一个缓存键的集合，或者使用分布式缓存
            _logger.LogInformation("清除类型缓存: {Type}", typeof(T).Name);
        }

        /// <summary>
        /// 缓存项移除回调
        /// </summary>
        private void OnCacheEvicted(object key, object? value, EvictionReason reason, object? state)
        {
            if (key is string cacheKey)
            {
                _logger.LogDebug("缓存项被移除: {CacheKey}, 原因: {Reason}", cacheKey, reason);
            }
        }

        /// <summary>
        /// 从表达式中提取常量值
        /// </summary>
        private List<object> ExtractConstants(Expression expression)
        {
            var constants = new List<object>();
            ExtractConstantsRecursive(expression, constants);
            return constants;
        }

        /// <summary>
        /// 递归提取表达式中的常量值
        /// </summary>
        private void ExtractConstantsRecursive(Expression expression, List<object> constants)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Constant:
                    var constantExpr = (ConstantExpression)expression;
                    if (constantExpr.Value != null)
                    {
                        // 排除表达式类型的常量
                        if (constantExpr.Value.GetType() != typeof(Expression))
                        {
                            constants.Add(constantExpr.Value);
                        }
                    }
                    break;
                case ExpressionType.MemberAccess:
                    var memberAccessExpr = (MemberExpression)expression;
                    // 检查是否是字段或属性访问
                    if (memberAccessExpr.Expression is ConstantExpression objExpr && objExpr.Value != null)
                    {
                        try
                        {
                            // 检查成员是否是字段或属性，并分别处理
                            object? value = null;
                            if (memberAccessExpr.Member is FieldInfo fieldInfo)
                            {
                                value = fieldInfo.GetValue(objExpr.Value);
                            }
                            else if (memberAccessExpr.Member is PropertyInfo propertyInfo)
                            {
                                value = propertyInfo.GetValue(objExpr.Value);
                            }
                            
                            if (value != null)
                            {
                                constants.Add(value);
                            }
                        }
                        catch {}
                    }
                    // 递归处理表达式
                    if (memberAccessExpr.Expression != null)
                    {
                        ExtractConstantsRecursive(memberAccessExpr.Expression, constants);
                    }
                    break;
                default:
                    // 递归处理子表达式
                    foreach (var child in GetChildren(expression))
                    {
                        if (child != null)
                        {
                            ExtractConstantsRecursive(child, constants);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// 获取表达式的子表达式
        /// </summary>
        private IEnumerable<Expression> GetChildren(Expression expression)
        {
            if (expression is BinaryExpression binaryExpr)
            {
                yield return binaryExpr.Left;
                yield return binaryExpr.Right;
            }
            else if (expression is UnaryExpression unaryExpr)
            {
                yield return unaryExpr.Operand;
            }
            else if (expression is MethodCallExpression methodCallExpr)
            {
                foreach (var arg in methodCallExpr.Arguments)
                {
                    yield return arg;
                }
                if (methodCallExpr.Object != null)
                {
                    yield return methodCallExpr.Object;
                }
            }
            else if (expression is LambdaExpression lambdaExpr)
            {
                yield return lambdaExpr.Body;
            }
            else if (expression is MemberExpression memberExpr)
            {
                if (memberExpr.Expression != null)
                {
                    yield return memberExpr.Expression;
                }
            }
        }
    }

    /// <summary>
    /// 查询缓存管理器接口
    /// </summary>
    public interface IQueryCacheManager
    {
        /// <summary>
        /// 获取缓存数据
        /// </summary>
        T? Get<T>(string cacheKey);
        
        /// <summary>
        /// 异步获取缓存数据
        /// </summary>
        Task<T?> GetAsync<T>(string cacheKey);
        
        /// <summary>
        /// 设置缓存数据
        /// </summary>
        void Set<T>(string cacheKey, T value, int cacheMinutes = 0);
        
        /// <summary>
        /// 异步设置缓存数据
        /// </summary>
        Task SetAsync<T>(string cacheKey, T value, int cacheMinutes = 0);
        
        /// <summary>
        /// 获取或创建缓存数据
        /// </summary>
        T GetOrCreate<T>(string cacheKey, Func<T> factory, int cacheMinutes = 0);
        
        /// <summary>
        /// 异步获取或创建缓存数据
        /// </summary>
        Task<T> GetOrCreateAsync<T>(string cacheKey, Func<Task<T>> factory, int cacheMinutes = 0);
        
        /// <summary>
        /// 移除缓存
        /// </summary>
        void Remove(string cacheKey);
        
        /// <summary>
        /// 异步移除缓存
        /// </summary>
        Task RemoveAsync(string cacheKey);
        
        /// <summary>
        /// 生成查询缓存键
        /// </summary>
        string GenerateCacheKey<T>(string operationName, params object[] parameters);
        
        /// <summary>
        /// 为条件查询生成缓存键
        /// </summary>
        string GenerateConditionCacheKey<T>(Expression<Func<T, bool>> predicate, string operationName = "ByCondition");
        
        /// <summary>
        /// 为分页查询生成缓存键
        /// </summary>
        string GeneratePagedCacheKey<T>(int pageNumber, int pageSize, 
            Expression<Func<T, object>>? orderBy = null, bool isDescending = false, 
            Expression<Func<T, bool>>? predicate = null);
        
        /// <summary>
        /// 移除实体相关的所有缓存
        /// </summary>
        Task RemoveEntityCacheAsync(Type entityType);
        
        /// <summary>
        /// 清除指定类型的所有缓存
        /// </summary>
        void ClearTypeCache<T>();
    }
}
