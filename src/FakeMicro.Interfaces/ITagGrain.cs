using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Orleans;
using FakeMicro.Entities;

namespace FakeMicro.Interfaces
{
    /// <summary>
    /// 标签Grain接口
    /// </summary>
    public interface ITagGrain : IGrainWithStringKey
    {
        /// <summary>
        /// 获取标签
        /// </summary>
        Task<Tag?> GetAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 创建标签
        /// </summary>
        Task<Tag?> CreateAsync(Tag tag, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新标签
        /// </summary>
        Task<Tag?> UpdateAsync(Tag tag, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除标签
        /// </summary>
        Task<bool> DeleteAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据用户ID获取标签列表
        /// </summary>
        Task<List<Tag>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据用户ID和标签名称获取标签
        /// </summary>
        Task<Tag?> GetByUserIdAndNameAsync(Guid userId, string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查标签名称是否存在
        /// </summary>
        Task<bool> NameExistsAsync(Guid userId, string name, Guid excludeId = default, CancellationToken cancellationToken = default);
    }
}