using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeMicro.Entities.KnowledgeBase;
using FakeMicro.Interfaces.Models;
using FakeMicro.Interfaces.Models.Common;
using FakeMicro.Interfaces.Services;

namespace FakeMicro.DatabaseAccess.Interfaces
{
    /// <summary>
    /// 知识库仓储接口
    /// </summary>
    public interface IKnowledgeBaseRepository : IBaseRepository<KnowledgeBaseItem, string>
    {
        /// <summary>
        /// 根据条件查询知识库条目
        /// </summary>
        /// <param name="query">查询条件</param>
        /// <returns>分页结果</returns>
        Task<PagedResult<KnowledgeBaseItem>> QueryItemsAsync(FakeMicro.Interfaces.Services.KnowledgeBaseQuery query);

        /// <summary>
        /// 根据来源ID获取知识库条目
        /// </summary>
        /// <param name="sourceId">来源ID</param>
        /// <returns>知识库条目</returns>
        Task<KnowledgeBaseItem> GetItemBySourceIdAsync(string sourceId);

        /// <summary>
        /// 根据类型获取知识库条目
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <returns>分页结果</returns>
        Task<PagedResult<KnowledgeBaseItem>> GetItemsByTypeAsync(string type, int pageIndex, int pageSize);

        /// <summary>
        /// 添加知识库摘要
        /// </summary>
        /// <param name="summary">摘要对象</param>
        /// <returns>摘要ID</returns>
        Task<string> AddSummaryAsync(KnowledgeBaseSummary summary);

        /// <summary>
        /// 更新知识库摘要
        /// </summary>
        /// <param name="summary">摘要对象</param>
        /// <returns>更新结果</returns>
        Task<bool> UpdateSummaryAsync(KnowledgeBaseSummary summary);

        /// <summary>
        /// 获取知识库摘要
        /// </summary>
        /// <param name="id">摘要ID</param>
        /// <returns>摘要对象</returns>
        Task<KnowledgeBaseSummary> GetSummaryAsync(string id);

        /// <summary>
        /// 根据类型获取知识库摘要
        /// </summary>
        /// <param name="summaryType">摘要类型</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <returns>分页结果</returns>
        Task<PagedResult<KnowledgeBaseSummary>> GetSummariesByTypeAsync(string summaryType, int pageIndex, int pageSize);

        /// <summary>
        /// 删除知识库条目
        /// </summary>
        /// <param name="sourceId">来源ID</param>
        /// <returns>删除结果</returns>
        Task<bool> DeleteItemBySourceIdAsync(string sourceId);
    }
}