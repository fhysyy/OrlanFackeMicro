using System;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Orleans.Providers;
using FakeMicro.Entities.KnowledgeBase;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace FakeMicro.Grains
{
    /// <summary>
    /// 知识库条目Grain实现
    /// </summary>
    [StorageProvider(ProviderName = "PubSubStore")] // 使用配置好的PostgreSQL存储
    public class KnowledgeBaseItemGrain : Grain<KnowledgeBaseItem>, IKnowledgeBaseItemGrain
    {
        private readonly ILogger<KnowledgeBaseItemGrain> _logger;
        private readonly IAiKnowledgeService _aiKnowledgeService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="aiKnowledgeService">AI知识库服务</param>
        public KnowledgeBaseItemGrain(ILogger<KnowledgeBaseItemGrain> logger, IAiKnowledgeService aiKnowledgeService)
        {
            _logger = logger;
            _aiKnowledgeService = aiKnowledgeService;
        }

        /// <summary>
        /// 激活Grain时调用
        /// </summary>
        public override Task OnActivateAsync(CancellationToken cancellationToken = default)
        {
            if (State == null)
            {
                State = new KnowledgeBaseItem();
            }
            return base.OnActivateAsync(cancellationToken);
        }

        /// <summary>
        /// 获取知识库条目
        /// </summary>
        public async Task<KnowledgeBaseItem> GetItemAsync()
        {
            return State;
        }

        /// <summary>
        /// 更新知识库条目
        /// </summary>
        public async Task<bool> UpdateItemAsync(KnowledgeBaseItem item)
        {
            try
            {
                item.UpdatedAt = DateTime.Now;
                State = item;
                await WriteStateAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新知识库条目失败: {Id}", this.GetPrimaryKeyLong());
                return false;
            }
        }

        /// <summary>
        /// 分析知识库条目内容
        /// </summary>
        public async Task<bool> AnalyzeItemAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(State.Content))
                {
                    return false;
                }

                // 使用AI服务提取关键词
                var keywords = await _aiKnowledgeService.ExtractKeywordsAsync(State.Content);
                State.Keywords = string.Join(",", keywords);

                // 使用AI服务生成摘要
                var summary = await _aiKnowledgeService.SummarizeContentAsync(State.Content);
                State.Summary = summary;

                State.LastAnalyzedAt = DateTime.Now;
                State.AnalysisStatus = "Completed";

                await WriteStateAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析知识库条目失败: {Id}", this.GetPrimaryKeyLong());
                State.AnalysisStatus = "Failed";
                await WriteStateAsync();
                return false;
            }
        }

        /// <summary>
        /// 生成条目向量表示
        /// </summary>
        public async Task<bool> GenerateVectorAsync()
        {
            try
            {
                // TODO: 实现向量生成和存储逻辑
                // 这将使用AI服务生成文本嵌入向量并存储到数据库
                State.LastAnalyzedAt = DateTime.Now;
                await WriteStateAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成知识库条目向量失败: {Id}", this.GetPrimaryKeyLong());
                return false;
            }
        }

        /// <summary>
        /// 删除知识库条目
        /// </summary>
        public async Task<bool> DeleteItemAsync()
        {
            try
            {
                await ClearStateAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除知识库条目失败: {Id}", this.GetPrimaryKeyLong());
                return false;
            }
        }
    }
}