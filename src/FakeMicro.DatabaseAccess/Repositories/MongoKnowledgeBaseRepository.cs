using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Entities.KnowledgeBase;
using FakeMicro.Interfaces.Models;
using FakeMicro.Interfaces.Models.Common;
using FakeMicro.Interfaces.Services;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FakeMicro.DatabaseAccess.Repositories
{
    /// <summary>
    /// 基于MongoDB的知识库仓储实现
    /// </summary>
    public class MongoKnowledgeBaseRepository : MongoRepository<KnowledgeBaseItem, string>, IKnowledgeBaseRepository
    {
        private readonly ILogger<MongoKnowledgeBaseRepository> _logger;
        private const string KNOWLEDGE_BASE_ITEMS_COLLECTION = "knowledge_base_items";
        private const string KNOWLEDGE_BASE_SUMMARIES_COLLECTION = "knowledge_base_summaries";

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="mongoClient">MongoDB客户端</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="defaultDatabaseName">默认数据库名称</param>
        public MongoKnowledgeBaseRepository(MongoClient mongoClient, ILogger<MongoKnowledgeBaseRepository> logger, string? defaultDatabaseName = null)
            : base(mongoClient, logger, defaultDatabaseName)
        {
            _logger = logger;
        }

        /// <summary>
        /// 根据条件查询知识库条目
        /// </summary>
        public async Task<PagedResult<KnowledgeBaseItem>> QueryItemsAsync(FakeMicro.Interfaces.Services.KnowledgeBaseQuery query)
        {
            try
            {
                var collection = GetCollection(collectionName: KNOWLEDGE_BASE_ITEMS_COLLECTION);
                var filterBuilder = Builders<KnowledgeBaseItem>.Filter;
                var filter = filterBuilder.Empty;

                // 应用过滤条件
                if (!string.IsNullOrEmpty(query.Type))
                {
                    filter &= filterBuilder.Eq(item => item.Type, query.Type);
                }

                if (!string.IsNullOrEmpty(query.Keywords))
                {
                    filter &= filterBuilder.Or(
                        filterBuilder.Regex(item => item.Title, new BsonRegularExpression(query.Keywords)),
                        filterBuilder.Regex(item => item.Summary, new BsonRegularExpression(query.Keywords)),
                        filterBuilder.Regex(item => item.Content, new BsonRegularExpression(query.Keywords)),
                        filterBuilder.Regex(item => item.Keywords, new BsonRegularExpression(query.Keywords))
                    );
                }

                if (!string.IsNullOrEmpty(query.SourceId))
                {
                    filter &= filterBuilder.Eq(item => item.SourceId, query.SourceId);
                }

                if (!string.IsNullOrEmpty(query.AnalysisStatus))
                {
                    filter &= filterBuilder.Eq(item => item.AnalysisStatus, query.AnalysisStatus);
                }

                if (query.CreatedAtStart.HasValue)
                {
                    filter &= filterBuilder.Gte(item => item.CreatedAt, query.CreatedAtStart.Value);
                }

                if (query.CreatedAtEnd.HasValue)
                {
                    filter &= filterBuilder.Lte(item => item.CreatedAt, query.CreatedAtEnd.Value);
                }

                // 计算总数
                var totalCount = await collection.CountDocumentsAsync(filter);

                // 执行分页查询
                var items = await collection
                    .Find(filter)
                    .SortByDescending(item => item.CreatedAt)
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Limit(query.PageSize)
                    .ToListAsync();

                return PagedResult<KnowledgeBaseItem>.SuccessResult(items, (int)totalCount, query.PageIndex, query.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询知识库条目失败: {Query}", query);
                throw;
            }
        }

        /// <summary>
        /// 根据来源ID获取知识库条目
        /// </summary>
        public async Task<KnowledgeBaseItem> GetItemBySourceIdAsync(string sourceId)
        {
            if (string.IsNullOrEmpty(sourceId))
                throw new ArgumentNullException(nameof(sourceId));

            try
            {
                var collection = GetCollection(collectionName: KNOWLEDGE_BASE_ITEMS_COLLECTION);
                var filter = Builders<KnowledgeBaseItem>.Filter.Eq(item => item.SourceId, sourceId);
                return await collection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据来源ID获取知识库条目失败: {SourceId}", sourceId);
                throw;
            }
        }

        /// <summary>
        /// 根据类型获取知识库条目
        /// </summary>
        public async Task<PagedResult<KnowledgeBaseItem>> GetItemsByTypeAsync(string type, int pageIndex, int pageSize)
        {
            if (string.IsNullOrEmpty(type))
                throw new ArgumentNullException(nameof(type));

            try
            {
                var collection = GetCollection(collectionName: KNOWLEDGE_BASE_ITEMS_COLLECTION);
                var filter = Builders<KnowledgeBaseItem>.Filter.Eq(item => item.Type, type);

                // 计算总数
                var totalCount = await collection.CountDocumentsAsync(filter);

                // 执行分页查询
                var items = await collection
                    .Find(filter)
                    .SortByDescending(item => item.CreatedAt)
                    .Skip((pageIndex - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();

                return PagedResult<KnowledgeBaseItem>.SuccessResult(items, (int)totalCount, pageIndex, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据类型获取知识库条目失败: {Type}", type);
                throw;
            }
        }

        /// <summary>
        /// 添加知识库摘要
        /// </summary>
        public async Task<string> AddSummaryAsync(KnowledgeBaseSummary summary)
        {
            if (summary == null)
                throw new ArgumentNullException(nameof(summary));

            try
            {
                var collection = GetCollection<KnowledgeBaseSummary>(collectionName: KNOWLEDGE_BASE_SUMMARIES_COLLECTION);
                await collection.InsertOneAsync(summary);
                return summary.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加知识库摘要失败: {Summary}", summary);
                throw;
            }
        }

        /// <summary>
        /// 更新知识库摘要
        /// </summary>
        public async Task<bool> UpdateSummaryAsync(KnowledgeBaseSummary summary)
        {
            if (summary == null)
                throw new ArgumentNullException(nameof(summary));

            try
            {
                var collection = GetCollection<KnowledgeBaseSummary>(collectionName: KNOWLEDGE_BASE_SUMMARIES_COLLECTION);
                var filter = Builders<KnowledgeBaseSummary>.Filter.Eq(s => s.Id, summary.Id);
                var updateResult = await collection.ReplaceOneAsync(filter, summary, new ReplaceOptions { IsUpsert = false });
                return updateResult.IsAcknowledged && updateResult.MatchedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新知识库摘要失败: {Summary}", summary);
                throw;
            }
        }

        /// <summary>
        /// 获取知识库摘要
        /// </summary>
        public async Task<KnowledgeBaseSummary> GetSummaryAsync(string id)
        {
            try
            {
                var collection = GetCollection<KnowledgeBaseSummary>(collectionName: KNOWLEDGE_BASE_SUMMARIES_COLLECTION);
                var filter = Builders<KnowledgeBaseSummary>.Filter.Eq(s => s.Id, id);
                return await collection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取知识库摘要失败: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 根据类型获取知识库摘要
        /// </summary>
        public async Task<PagedResult<KnowledgeBaseSummary>> GetSummariesByTypeAsync(string summaryType, int pageIndex, int pageSize)
        {
            if (string.IsNullOrEmpty(summaryType))
                throw new ArgumentNullException(nameof(summaryType));

            try
            {
                var collection = GetCollection<KnowledgeBaseSummary>(collectionName: KNOWLEDGE_BASE_SUMMARIES_COLLECTION);
                var filter = Builders<KnowledgeBaseSummary>.Filter.Eq(s => s.SummaryType, summaryType);

                // 计算总数
                var totalCount = await collection.CountDocumentsAsync(filter);

                // 执行分页查询
                var summaries = await collection
                    .Find(filter)
                    .SortByDescending(s => s.CreatedAt)
                    .Skip((pageIndex - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();

                return PagedResult<KnowledgeBaseSummary>.SuccessResult(summaries, (int)totalCount, pageIndex, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据类型获取知识库摘要失败: {SummaryType}", summaryType);
                throw;
            }
        }

        /// <summary>
        /// 删除知识库条目
        /// </summary>
        public async Task<bool> DeleteItemBySourceIdAsync(string sourceId)
        {
            if (string.IsNullOrEmpty(sourceId))
                throw new ArgumentNullException(nameof(sourceId));

            try
            {
                var collection = GetCollection(collectionName: KNOWLEDGE_BASE_ITEMS_COLLECTION);
                var filter = Builders<KnowledgeBaseItem>.Filter.Eq(item => item.SourceId, sourceId);
                var deleteResult = await collection.DeleteOneAsync(filter);
                return deleteResult.IsAcknowledged && deleteResult.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据来源ID删除知识库条目失败: {SourceId}", sourceId);
                throw;
            }
        }
    }
}