using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Orleans;
using FakeMicro.Entities;

namespace FakeMicro.Interfaces
{
    /// <summary>
    /// 笔记本Grain接口
    /// </summary>
     public interface INotebookGrain : IGrainWithStringKey
    {
        /// <summary>
        /// 获取笔记本
        /// </summary>
        Task<Notebook?> GetAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 创建笔记本
        /// </summary>
        Task<Notebook?> CreateAsync(Notebook notebook, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新笔记本
        /// </summary>
        Task<Notebook?> UpdateAsync(Notebook notebook, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除笔记本
        /// </summary>
        Task<bool> DeleteAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据用户ID获取笔记本列表
        /// </summary>
        Task<List<Notebook>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据用户ID和父笔记本ID获取笔记本列表
        /// </summary>
        Task<List<Notebook>> GetByUserIdAndParentIdAsync(Guid userId, Guid? parentId, CancellationToken cancellationToken = default);
    }
}