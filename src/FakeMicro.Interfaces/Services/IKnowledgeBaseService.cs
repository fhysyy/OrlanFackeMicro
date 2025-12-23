using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeMicro.Entities.KnowledgeBase;
using FakeMicro.Interfaces.Models;
using FakeMicro.Interfaces.Models.Common;

namespace FakeMicro.Interfaces.Services
{
    /// <summary>
    /// 知识库服务接口
    /// </summary>
    public interface IKnowledgeBaseService
    {
        /// <summary>
        /// 分析实体并添加到知识库
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="entity">实体对象</param>
        /// <param name="entityType">实体类型标识</param>
        /// <returns>知识库条目ID</returns>
        Task<long> AnalyzeAndAddEntityAsync<TEntity>(TEntity entity, string entityType);

        /// <summary>
        /// 批量分析实体并添加到知识库
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="entities">实体列表</param>
        /// <param name="entityType">实体类型标识</param>
        /// <returns>添加结果</returns>
        Task<BatchResult<long>> BatchAnalyzeAndAddEntitiesAsync<TEntity>(IEnumerable<TEntity> entities, string entityType);

        /// <summary>
        /// 生成系统数据摘要
        /// </summary>
        /// <param name="summaryType">摘要类型</param>
        /// <param name="scope">分析范围</param>
        /// <returns>摘要ID</returns>
        Task<long> GenerateSystemSummaryAsync(string summaryType, Dictionary<string, object> scope);

        /// <summary>
        /// 获取知识库条目
        /// </summary>
        /// <param name="id">条目ID</param>
        /// <returns>知识库条目</returns>
        Task<KnowledgeBaseItem> GetKnowledgeItemAsync(long id);

        /// <summary>
        /// 根据条件查询知识库条目
        /// </summary>
        /// <param name="query">查询条件</param>
        /// <returns>查询结果</returns>
        Task<PagedResult<KnowledgeBaseItem>> QueryKnowledgeItemsAsync(KnowledgeBaseQuery query);

        /// <summary>
        /// 获取系统摘要
        /// </summary>
        /// <param name="id">摘要ID</param>
        /// <returns>系统摘要</returns>
        Task<KnowledgeBaseSummary> GetSystemSummaryAsync(long id);

        /// <summary>
        /// 根据类型获取系统摘要列表
        /// </summary>
        /// <param name="summaryType">摘要类型</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <returns>摘要列表</returns>
        Task<PagedResult<KnowledgeBaseSummary>> GetSystemSummariesByTypeAsync(string summaryType, int pageIndex, int pageSize);

        /// <summary>
        /// 更新知识库条目分析
        /// </summary>
        /// <param name="id">条目ID</param>
        /// <returns>更新结果</returns>
        Task<bool> UpdateKnowledgeItemAnalysisAsync(long id);
    }

    /// <summary>
    /// 知识库查询条件
    /// </summary>
    public class KnowledgeBaseQuery
    {
        /// <summary>
        /// 条目类型
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 关键词
        /// </summary>
        public string Keywords { get; set; }

        /// <summary>
        /// 来源ID
        /// </summary>
        public string SourceId { get; set; }

        /// <summary>
        /// 分析状态
        /// </summary>
        public string AnalysisStatus { get; set; }

        /// <summary>
        /// 创建时间起始
        /// </summary>
        public DateTime? CreatedAtStart { get; set; }

        /// <summary>
        /// 创建时间结束
        /// </summary>
        public DateTime? CreatedAtEnd { get; set; }

        /// <summary>
        /// 页码
        /// </summary>
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 每页大小
        /// </summary>
        public int PageSize { get; set; } = 20;
    }
}