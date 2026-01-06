using System.Collections.Generic;
using System.Threading.Tasks;
using FakeMicro.Entities;
using FakeMicro.Interfaces.Models;
using FakeMicro.Utilities;
using Orleans;
using Orleans.Concurrency;
namespace FakeMicro.Interfaces
{
    /// <summary>
    /// 字典类型管理Grain接口
    /// </summary>
    public interface IDictionaryTypeGrain :IGrainWithStringKey
    {
        /// <summary>
        /// 获取字典类型详情
        /// </summary>
        /// <returns>字典类型实体</returns>
        [ReadOnly]
        [AlwaysInterleave]
        Task<DictionaryType> GetDictionaryTypeAsync();

        /// <summary>
        /// 创建字典类型
        /// </summary>
        /// <param name="dictionaryType">字典类型实体</param>
        /// <returns>创建后的字典类型实体</returns>
        Task<DictionaryType> CreateDictionaryTypeAsync(DictionaryType dictionaryType);

        /// <summary>
        /// 更新字典类型
        /// </summary>
        /// <param name="dictionaryType">字典类型实体</param>
        /// <returns>更新后的字典类型实体</returns>
        Task<DictionaryType> UpdateDictionaryTypeAsync(DictionaryType dictionaryType);

        /// <summary>
        /// 删除字典类型
        /// </summary>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteDictionaryTypeAsync();

        /// <summary>
        /// 检查编码是否已存在
        /// </summary>
        /// <param name="code">字典类型编码</param>
        /// <param name="excludeId">排除的ID（用于更新场景）</param>
        /// <returns>是否存在</returns>
        [ReadOnly]
        [AlwaysInterleave]
        Task<bool> CodeExistsAsync(string code, long excludeId = 0);
    }

    /// <summary>
    /// 字典类型管理服务接口（用于查询操作）
    /// </summary>
    public interface IDictionaryTypeService :IGrainWithGuidKey
    {
        /// <summary>
        /// 获取字典类型列表（分页）
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="keyword">关键字</param>
        /// <param name="isEnabled">是否启用</param>
        /// <returns>分页结果</returns>
        [ReadOnly]
        [AlwaysInterleave]
        Task<IPaginatedResult<DictionaryType>> GetDictionaryTypesAsync(int page, int pageSize, string? keyword = null, bool? isEnabled = null);
    }
}