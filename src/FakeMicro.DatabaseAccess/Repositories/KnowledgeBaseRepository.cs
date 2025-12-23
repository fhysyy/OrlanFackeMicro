using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Entities.KnowledgeBase;
using FakeMicro.Interfaces.Models;
using FakeMicro.Interfaces.Models.Common;
using FakeMicro.Interfaces.Services;
using Microsoft.Extensions.Logging;
using SqlSugar;

namespace FakeMicro.DatabaseAccess.Repositories
{
    /// <summary>
    /// 知识库仓储实现
    /// </summary>
    public class KnowledgeBaseRepository : SqlSugarRepository<KnowledgeBaseItem, string>, IKnowledgeBaseRepository
    {
        private readonly ILogger<KnowledgeBaseRepository> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="sqlSugarClient">SqlSugar客户端</param>
        /// <param name="logger">日志记录器</param>
        public KnowledgeBaseRepository(ISqlSugarClient sqlSugarClient, ILogger<KnowledgeBaseRepository> logger)
            : base(sqlSugarClient, logger)
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
                var dbQuery = GetSqlSugarClient().Queryable<KnowledgeBaseItem>();

                // 应用过滤条件
                if (!string.IsNullOrEmpty(query.Type))
                {
                    dbQuery = dbQuery.Where(item => item.Type == query.Type);
                }

                if (!string.IsNullOrEmpty(query.Keywords))
                {
                    dbQuery = dbQuery.Where(item => 
                        item.Title.Contains(query.Keywords) || 
                        item.Summary.Contains(query.Keywords) || 
                        item.Content.Contains(query.Keywords) ||
                        item.Keywords.Contains(query.Keywords));
                }

                if (!string.IsNullOrEmpty(query.SourceId))
                {
                    dbQuery = dbQuery.Where(item => item.SourceId == query.SourceId);
                }

                if (!string.IsNullOrEmpty(query.AnalysisStatus))
                {
                    dbQuery = dbQuery.Where(item => item.AnalysisStatus == query.AnalysisStatus);
                }

                if (query.CreatedAtStart.HasValue)
                {
                    dbQuery = dbQuery.Where(item => item.CreatedAt >= query.CreatedAtStart.Value);
                }

                if (query.CreatedAtEnd.HasValue)
                {
                    dbQuery = dbQuery.Where(item => item.CreatedAt <= query.CreatedAtEnd.Value);
                }

                // 执行分页查询
                var totalCount = await dbQuery.CountAsync();
                var items = await dbQuery
                    .OrderBy(item => item.CreatedAt, OrderByType.Desc)
                    .ToPageListAsync(query.PageIndex, query.PageSize);

                return PagedResult<KnowledgeBaseItem>.SuccessResult(items, totalCount, query.PageIndex, query.PageSize);
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
                return await GetSqlSugarClient().Queryable<KnowledgeBaseItem>()
                    .Where(item => item.SourceId == sourceId)
                    .FirstAsync();
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
                var dbQuery = GetSqlSugarClient().Queryable<KnowledgeBaseItem>()
                    .Where(item => item.Type == type);

                var totalCount = await dbQuery.CountAsync();
                var items = await dbQuery
                    .OrderBy(item => item.CreatedAt, OrderByType.Desc)
                    .ToPageListAsync(pageIndex, pageSize);

                return PagedResult<KnowledgeBaseItem>.SuccessResult(items, totalCount, pageIndex, pageSize);
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
                var result = await GetSqlSugarClient().Insertable(summary).ExecuteReturnBigIdentityAsync();
                return result.ToString();
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
                return await GetSqlSugarClient().Updateable(summary).ExecuteCommandAsync() > 0;
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
                return await GetSqlSugarClient().Queryable<KnowledgeBaseSummary>()
                    .Where(summary => summary.Id == id)
                    .FirstAsync();
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
                var dbQuery = GetSqlSugarClient().Queryable<KnowledgeBaseSummary>()
                    .Where(summary => summary.SummaryType == summaryType);

                var totalCount = await dbQuery.CountAsync();
                var summaries = await dbQuery
                    .OrderBy(summary => summary.CreatedAt, OrderByType.Desc)
                    .ToPageListAsync(pageIndex, pageSize);

                return PagedResult<KnowledgeBaseSummary>.SuccessResult(summaries, totalCount, pageIndex, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据类型获取知识库摘要失败: {SummaryType}", summaryType);
                throw;
            }
        }

        /// <summary>
        /// 根据ID获取知识库条目
        /// </summary>
        public async Task<KnowledgeBaseItem?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await GetSqlSugarClient().Queryable<KnowledgeBaseItem>()
                    .Where(item => item.Id == id)
                    .FirstAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID获取知识库条目失败: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 根据ID删除知识库条目
        /// </summary>
        public async Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await GetSqlSugarClient().Deleteable<KnowledgeBaseItem>()
                    .Where(item => item.Id == id)
                    .ExecuteCommandAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID删除知识库条目失败: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 根据来源ID删除知识库条目
        /// </summary>
        public async Task<bool> DeleteItemBySourceIdAsync(string sourceId)
        {
            if (string.IsNullOrEmpty(sourceId))
                throw new ArgumentNullException(nameof(sourceId));

            try
            {
                return await GetSqlSugarClient().Deleteable<KnowledgeBaseItem>()
                    .Where(item => item.SourceId == sourceId)
                    .ExecuteCommandAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据来源ID删除知识库条目失败: {SourceId}", sourceId);
                throw;
            }
        }
    }
}