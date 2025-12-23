using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Entities.KnowledgeBase;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
using FakeMicro.Interfaces.Models.Common;
using FakeMicro.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Orleans;

namespace FakeMicro.Grains.Services
{
    public class KnowledgeBaseService : IKnowledgeBaseService
    {
        private readonly IKnowledgeBaseRepository _repository;
        private readonly ILogger<KnowledgeBaseService> _logger;
        private readonly IGrainFactory _grainFactory;
        private readonly IAiKnowledgeService _aiKnowledgeService;

        public KnowledgeBaseService(
            IKnowledgeBaseRepository repository,
            ILogger<KnowledgeBaseService> logger,
            IGrainFactory grainFactory,
            IAiKnowledgeService aiKnowledgeService)
        {
            _repository = repository;
            _logger = logger;
            _grainFactory = grainFactory;
            _aiKnowledgeService = aiKnowledgeService;
        }

        public async Task<long> AnalyzeAndAddEntityAsync<TEntity>(TEntity entity, string entityType)
        {
            try
            {
                if (entity == null)
                {
                    _logger.LogWarning("尝试分析和添加空实体");
                    throw new ArgumentNullException(nameof(entity));
                }

                var item = await CreateKnowledgeItemFromEntity(entity, entityType);
                if (item == null)
                {
                    throw new InvalidOperationException("创建知识库条目失败");
                }

                // 保存到数据库
                await _repository.AddAsync(item);
                if (item.Id <= 0)
                {
                    _logger.LogError("保存知识库条目失败: {Type}", entityType);
                    throw new InvalidOperationException("保存知识库条目失败");
                }

                // 使用Grain进行异步分析
                var itemGrain = _grainFactory.GetGrain<IKnowledgeBaseItemGrain>(item.Id);
                await itemGrain.AnalyzeItemAsync();
                await itemGrain.GenerateVectorAsync();

                _logger.LogInformation("成功分析和添加实体到知识库: {Type}, {Id}", entityType, item.Id);
                return item.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析和添加实体到知识库失败: {Type}", entityType);
                throw;
            }
        }

        public async Task<BatchResult<long>> BatchAnalyzeAndAddEntitiesAsync<TEntity>(IEnumerable<TEntity> entities, string entityType)
        {
            var successIds = new List<long>();
            var errors = new List<string>();

            foreach (var entity in entities)
            {
                try
                {
                    long id = await AnalyzeAndAddEntityAsync(entity, entityType);
                    successIds.Add(id);
                }
                catch (Exception ex)
                {
                    errors.Add($"添加实体异常: {entityType}, 错误: {ex.Message}");
                }
            }

            return new BatchResult<long>
            {
                SuccessItems = successIds,
                ErrorMessages = errors,
                TotalItems = entities.Count(),
                SuccessCount = successIds.Count,
                FailedCount = errors.Count,
                IsAllSuccess = errors.Count == 0
            };
        }

        public async Task<long> GenerateSystemSummaryAsync(string summaryType, Dictionary<string, object> scope)
        {
            try
            {
                _logger.LogInformation("开始生成系统摘要: {SummaryType}, 范围: {Scope}", summaryType, scope);

                // 这里应该实现生成系统摘要的逻辑
                // 目前先简单创建一个摘要对象
                var summary = new KnowledgeBaseSummary
                {
                    SummaryType = summaryType,
                    Content = $"系统摘要内容: {summaryType}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // 保存到数据库
                var savedSummaryId = await _repository.AddSummaryAsync(summary);
                if (savedSummaryId <= 0)
                {
                    _logger.LogError("保存系统摘要失败: {SummaryType}", summaryType);
                    throw new InvalidOperationException("保存系统摘要失败");
                }

                _logger.LogInformation("成功生成系统摘要: {SummaryType}, {Id}", summaryType, savedSummaryId);
                return savedSummaryId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成系统摘要失败: {SummaryType}, {Scope}", summaryType, scope);
                throw;
            }
        }

        public async Task<KnowledgeBaseItem> GetKnowledgeItemAsync(long id)
        {
            try
            {
                return await _repository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取知识库条目失败: {Id}", id);
                throw;
            }
        }

        public async Task<PagedResult<KnowledgeBaseItem>> QueryKnowledgeItemsAsync(KnowledgeBaseQuery query)
        {
            try
            {
                return await _repository.QueryItemsAsync(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询知识库条目失败: {Query}", query);
                throw;
            }
        }

        public async Task<KnowledgeBaseSummary> GetSystemSummaryAsync(long id)
        {
            try
            {
                return await _repository.GetSummaryAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取系统摘要失败: {Id}", id);
                throw;
            }
        }

        public async Task<PagedResult<KnowledgeBaseSummary>> GetSystemSummariesByTypeAsync(string summaryType, int pageIndex, int pageSize)
        {
            try
            {
                return await _repository.GetSummariesByTypeAsync(summaryType, pageIndex, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据类型获取系统摘要失败: {SummaryType}", summaryType);
                throw;
            }
        }

        public async Task<bool> UpdateKnowledgeItemAnalysisAsync(long id)
        {
            try
            {
                // 使用Grain进行分析更新
                var itemGrain = _grainFactory.GetGrain<IKnowledgeBaseItemGrain>(id);
                return await itemGrain.AnalyzeItemAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新知识库条目分析失败: {Id}", id);
                return false;
            }
        }

        #region 辅助方法

        private async Task<KnowledgeBaseItem> CreateKnowledgeItemFromEntity(object entity, string type, string sourceId = null)
        {
            try
            {
                if (entity == null)
                {
                    return null;
                }

                // 如果没有提供sourceId，尝试从实体的Id属性获取
                if (string.IsNullOrEmpty(sourceId))
                {
                    var idProperty = entity.GetType().GetProperty("Id") ?? entity.GetType().GetProperty("ID");
                    if (idProperty != null)
                    {
                        sourceId = idProperty.GetValue(entity)?.ToString();
                    }
                }

                // 提取实体的属性值
                var properties = entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var propertyValues = new List<string>();

                foreach (var property in properties)
                {
                    var value = property.GetValue(entity);
                    if (value != null && property.PropertyType.IsPrimitive || property.PropertyType == typeof(string) || property.PropertyType == typeof(DateTime))
                    {
                        propertyValues.Add($"{property.Name}: {value}");
                    }
                }

                var content = string.Join("\n", propertyValues);
                var title = entity.GetType().Name;

                return new KnowledgeBaseItem
                {
                    SourceId = sourceId,
                    Type = type,
                    Title = title,
                    Content = content,
                    Keywords = string.Empty,
                    AnalysisStatus = "pending",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从实体创建知识库条目失败: {Type}", type);
                return null;
            }
        }

        #endregion
    }
}