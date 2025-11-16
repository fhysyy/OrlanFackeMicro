using System.Collections.Generic;
using System.Threading.Tasks;
using FakeMicro.Entities;
using FakeMicro.Interfaces.Attributes;

namespace FakeMicro.Interfaces
{
    /// <summary>
    /// 字典项管理Grain接口
    /// </summary>
    [Version(1, 1)]
    public interface IDictionaryItemGrain : Orleans.IGrainWithStringKey
    {
        /// <summary>
        /// 获取字典项详情
        /// </summary>
        /// <returns>字典项实体</returns>
        Task<DictionaryItem> GetDictionaryItemAsync();

        /// <summary>
        /// 创建字典项
        /// </summary>
        /// <param name="dictionaryItem">字典项实体</param>
        /// <returns>创建后的字典项实体</returns>
        Task<DictionaryItem> CreateDictionaryItemAsync(DictionaryItem dictionaryItem);

        /// <summary>
        /// 更新字典项
        /// </summary>
        /// <param name="dictionaryItem">字典项实体</param>
        /// <returns>更新后的字典项实体</returns>
        Task<DictionaryItem> UpdateDictionaryItemAsync(DictionaryItem dictionaryItem);

        /// <summary>
        /// 删除字典项
        /// </summary>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteDictionaryItemAsync();

        /// <summary>
        /// 检查值是否已存在
        /// </summary>
        /// <param name="dictionaryTypeId">字典类型ID</param>
        /// <param name="value">字典项值</param>
        /// <param name="excludeId">排除的ID（用于更新场景）</param>
        /// <returns>是否存在</returns>
        Task<bool> ValueExistsAsync(long dictionaryTypeId, string value, long excludeId = 0);
    }

    /// <summary>
    /// 字典项管理服务接口（用于查询操作）
    /// </summary>
    public interface IDictionaryItemService : Orleans.IGrainWithGuidKey
    {
        /// <summary>
        /// 根据字典类型ID获取字典项列表
        /// </summary>
        /// <param name="typeId">字典类型ID</param>
        /// <returns>字典项列表</returns>
        Task<List<DictionaryItem>> GetByDictionaryTypeIdAsync(long typeId);

        /// <summary>
        /// 根据字典类型编码获取字典项列表
        /// </summary>
        /// <param name="code">字典类型编码</param>
        /// <returns>字典项列表</returns>
        Task<List<DictionaryItem>> GetByDictionaryTypeCodeAsync(string code);
    }
}