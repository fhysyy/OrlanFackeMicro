using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Entities;
using System.Linq.Expressions;

namespace FakeMicro.DatabaseAccess
{
    /// <summary>
    /// 字典项仓储接口
    /// </summary>
    public interface IDictionaryItemRepository:IRepository<DictionaryItem, long>
    {
        /// <summary>
        /// 根据字典类型ID获取字典项列表
        /// </summary>
        Task<IEnumerable<DictionaryItem>> GetByDictionaryTypeIdAsync(long dictionaryTypeId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据字典类型编码获取字典项列表
        /// </summary>
        Task<IEnumerable<DictionaryItem>> GetByDictionaryTypeCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据字典类型ID和值获取字典项
        /// </summary>
        Task<DictionaryItem?> GetByTypeIdAndValueAsync(long dictionaryTypeId, string value, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查在同一字典类型下值是否存在
        /// </summary>
        Task<bool> ValueExistsAsync(long dictionaryTypeId, string value, long excludeId = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量删除指定字典类型的所有字典项
        /// </summary>
        Task<int> DeleteByDictionaryTypeIdAsync(long dictionaryTypeId, CancellationToken cancellationToken = default);
    }
}