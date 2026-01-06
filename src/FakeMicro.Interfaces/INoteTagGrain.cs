using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Orleans;
using Orleans.Concurrency;
using FakeMicro.Entities;

namespace FakeMicro.Interfaces
{
    /// <summary>
    /// 标签Grain接口
    /// </summary>
    public interface INoteTagGrain : IGrainWithStringKey
    {
        /// <summary>
        /// 获取标签
        /// </summary>
        [ReadOnly]
        [AlwaysInterleave]
        Task<NoteTag?> GetAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 创建标签
        /// </summary>
        Task<NoteTag?> CreateAsync(NoteTag tag, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新标签
        /// </summary>
        Task<NoteTag?> UpdateAsync(NoteTag tag, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除标签
        /// </summary>
        Task<bool> DeleteAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据用户ID获取标签列表
        /// </summary>
        [ReadOnly]
        [AlwaysInterleave]
        Task<List<NoteTag>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据用户ID和标签名称获取标签
        /// </summary>
        [ReadOnly]
        [AlwaysInterleave]
        Task<NoteTag?> GetByUserIdAndNameAsync(Guid userId, string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查标签名称是否存在
        /// </summary>
        [ReadOnly]
        [AlwaysInterleave]
        Task<bool> NameExistsAsync(Guid userId, string name, Guid excludeId = default, CancellationToken cancellationToken = default);
    }
}