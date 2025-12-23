using System;
using System.Threading.Tasks;
using Orleans;
using FakeMicro.Entities.KnowledgeBase;
using FakeMicro.Interfaces.Models.Common;

namespace FakeMicro.Interfaces
{
    /// <summary>
    /// 知识库条目Grain接口
    /// </summary>
    public interface IKnowledgeBaseItemGrain : IGrainWithIntegerKey
    {
        /// <summary>
        /// 获取知识库条目
        /// </summary>
        Task<KnowledgeBaseItem> GetItemAsync();
        
        /// <summary>
        /// 更新知识库条目
        /// </summary>
        Task<bool> UpdateItemAsync(KnowledgeBaseItem item);
        
        /// <summary>
        /// 分析知识库条目内容
        /// </summary>
        Task<bool> AnalyzeItemAsync();
        
        /// <summary>
        /// 生成条目向量表示
        /// </summary>
        Task<bool> GenerateVectorAsync();
        
        /// <summary>
        /// 删除知识库条目
        /// </summary>
        Task<bool> DeleteItemAsync();
    }
}