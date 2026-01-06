using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Orleans;
using Orleans.Concurrency;
using FakeMicro.Entities;
using FakeMicro.Entities.Enums;
using FakeMicro.Interfaces.Models;
using FakeMicro.Interfaces.Models.Results;

namespace FakeMicro.Interfaces
{
    /// <summary>
    /// 表单配置管理Grain接口
    /// </summary>
    public interface IFormConfigGrain : IGrainWithStringKey
    {
        /// <summary>
        /// 获取表单配置详情
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表单配置实体</returns>
        [ReadOnly]
        [AlwaysInterleave]
        Task<FormConfigDto?> GetAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 创建表单配置
        /// </summary>
        /// <param name="request">创建请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>创建结果</returns>
        Task<FormConfigDto> CreateAsync(FormConfigCreateDto request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新表单配置
        /// </summary>
        /// <param name="request">更新请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新结果</returns>
        Task<FormConfigDto> UpdateAsync(FormConfigUpdateDto request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除表单配置
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>删除结果</returns>
        Task<bool> DeleteAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新表单配置状态
        /// </summary>
        /// <param name="status">新状态</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新结果</returns>
        Task<FormConfigDto> UpdateStatusAsync(FormConfigStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查表单编码是否已存在
        /// </summary>
        /// <param name="code">表单编码</param>
        /// <param name="excludeId">排除的ID（用于更新场景）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否存在</returns>
        [ReadOnly]
        [AlwaysInterleave]
        Task<bool> CodeExistsAsync(string code, string excludeId = "", CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 表单配置管理服务接口（用于查询操作）
    /// </summary>
    public interface IFormConfigService : IGrainWithGuidKey
    {
        /// <summary>
        /// 获取表单配置列表（分页）
        /// </summary>
        /// <param name="query">查询参数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>分页结果</returns>
        [ReadOnly]
        [AlwaysInterleave]
        Task<PaginatedResult<FormConfigDto>> GetFormConfigsAsync(FormConfigQueryDto query, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有表单配置列表（用于下拉选择等场景）
        /// </summary>
        /// <param name="status">状态筛选</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表单配置列表</returns>
        [ReadOnly]
        [AlwaysInterleave]
        Task<List<FormConfigDto>> GetAllFormConfigsAsync(FormConfigStatus? status = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据编码获取表单配置
        /// </summary>
        /// <param name="code">表单编码</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表单配置实体</returns>
        [ReadOnly]
        [AlwaysInterleave]
        Task<FormConfigDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    }
}