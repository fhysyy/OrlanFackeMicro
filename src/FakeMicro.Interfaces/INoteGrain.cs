using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Orleans;
using Orleans.Concurrency;
using FakeMicro.Entities;

namespace FakeMicro.Interfaces
{
    /// <summary>
    /// 笔记Grain接口
    /// </summary>
    public interface INoteGrain : IGrainWithStringKey
    {
        /// <summary>
        /// 获取笔记
        /// </summary>
        [ReadOnly]
        [AlwaysInterleave]
        Task<Note?> GetAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 创建笔记
        /// </summary>
        Task<Note?> CreateAsync(Note note, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新笔记
        /// </summary>
        Task<Note?> UpdateAsync(Note note, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除笔记
        /// </summary>
        Task<bool> DeleteAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 软删除笔记
        /// </summary>
        Task<bool> SoftDeleteAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据用户ID获取笔记列表
        /// </summary>
        [ReadOnly]
        [AlwaysInterleave]
        Task<List<Note>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据笔记本ID获取笔记列表
        /// </summary>
        [ReadOnly]
        [AlwaysInterleave]
        Task<List<Note>> GetByNotebookIdAsync(Guid notebookId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据用户ID和笔记本ID获取笔记列表
        /// </summary>
        [ReadOnly]
        [AlwaysInterleave]
        Task<List<Note>> GetByUserIdAndNotebookIdAsync(Guid userId, Guid notebookId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据标签ID获取笔记列表
        /// </summary>
        [ReadOnly]
        [AlwaysInterleave]
        Task<List<Note>> GetByTagIdAsync(Guid tagId, CancellationToken cancellationToken = default);
    }
}