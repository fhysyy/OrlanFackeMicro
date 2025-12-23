using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeMicro.Interfaces.Models.Common;

namespace FakeMicro.Interfaces.Services
{
    /// <summary>
    /// AI知识库服务接口
    /// </summary>
    public interface IAiKnowledgeService
    {
        /// <summary>
        /// 向AI提问
        /// </summary>
        /// <param name="question">问题文本</param>
        /// <param name="options">AI请求选项</param>
        /// <returns>AI回答</returns>
        Task<AiResponse> AskQuestionAsync(string question, AiRequestOptions options = null);

        /// <summary>
        /// 批量向AI提问
        /// </summary>
        /// <param name="questions">问题列表</param>
        /// <param name="options">AI请求选项</param>
        /// <returns>AI回答列表</returns>
        Task<BatchResult<AiResponse>> BatchAskQuestionsAsync(IEnumerable<string> questions, AiRequestOptions options = null);

        /// <summary>
        /// 总结文本内容
        /// </summary>
        /// <param name="content">待总结的文本内容</param>
        /// <param name="options">总结选项</param>
        /// <returns>总结结果</returns>
        Task<string> SummarizeContentAsync(string content, AiSummarizeOptions options = null);

        /// <summary>
        /// 提取关键词
        /// </summary>
        /// <param name="content">文本内容</param>
        /// <param name="options">提取选项</param>
        /// <returns>关键词列表</returns>
        Task<string[]> ExtractKeywordsAsync(string content, AiExtractOptions options = null);

        /// <summary>
        /// 生成文本嵌入向量
        /// </summary>
        /// <param name="text">文本内容</param>
        /// <returns>嵌入向量</returns>
        Task<float[]> GenerateEmbeddingAsync(string text);

        /// <summary>
        /// 批量生成文本嵌入向量
        /// </summary>
        /// <param name="texts">文本列表</param>
        /// <returns>嵌入向量列表</returns>
        Task<float[][]> BatchGenerateEmbeddingsAsync(IEnumerable<string> texts);

        /// <summary>
        /// 查找语义相似的知识库条目
        /// </summary>
        /// <param name="queryText">查询文本</param>
        /// <param name="topK">返回数量</param>
        /// <returns>相似条目列表</returns>
        Task<List<SimilarItem>> FindSimilarItemsAsync(string queryText, int topK = 5);
    }

    /// <summary>
    /// AI响应模型
    /// </summary>
    public class AiResponse
    {
        /// <summary>
        /// 响应内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 响应时间
        /// </summary>
        public DateTime ResponseTime { get; set; }

        /// <summary>
        /// 响应状态
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 置信度
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 使用的知识库来源
        /// </summary>
        public List<string> Sources { get; set; }
    }

    /// <summary>
    /// AI请求选项
    /// </summary>
    public class AiRequestOptions
    {
        /// <summary>
        /// 模型名称
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// 温度参数
        /// </summary>
        public float Temperature { get; set; } = 0.7f;

        /// <summary>
        /// 最大令牌数
        /// </summary>
        public int MaxTokens { get; set; } = 512;

        /// <summary>
        /// 是否使用知识库
        /// </summary>
        public bool UseKnowledgeBase { get; set; } = true;

        /// <summary>
        /// 知识库过滤条件
        /// </summary>
        public Dictionary<string, object> KnowledgeBaseFilters { get; set; }
    }

    /// <summary>
    /// AI总结选项
    /// </summary>
    public class AiSummarizeOptions
    {
        /// <summary>
        /// 摘要类型
        /// </summary>
        public string SummaryType { get; set; } = "concise";

        /// <summary>
        /// 最大长度
        /// </summary>
        public int MaxLength { get; set; } = 200;

        /// <summary>
        /// 最小长度
        /// </summary>
        public int MinLength { get; set; } = 50;
    }

    /// <summary>
    /// AI提取选项
    /// </summary>
    public class AiExtractOptions
    {
        /// <summary>
        /// 最大关键词数量
        /// </summary>
        public int MaxKeywords { get; set; } = 10;

        /// <summary>
        /// 关键词类型
        /// </summary>
        public string KeywordType { get; set; } = "noun";
    }

    /// <summary>
    /// 相似条目
    /// </summary>
    public class SimilarItem
    {
        /// <summary>
        /// 知识库条目ID
        /// </summary>
        public long KnowledgeItemId { get; set; }

        /// <summary>
        /// 相似度分数
        /// </summary>
        public double SimilarityScore { get; set; }

        /// <summary>
        /// 条目标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 条目摘要
        /// </summary>
        public string Summary { get; set; }
    }
}
