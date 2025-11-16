using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Entities;

namespace FakeMicro.DatabaseAccess
{
    /// <summary>
    /// 字典类型仓储接口
    /// </summary>
    public interface IDictionaryTypeRepository:IRepository<DictionaryType, long>
    {
        /// <summary>
        /// 根据编码获取字典类型
        /// </summary>
        Task<DictionaryType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查编码是否存在
        /// </summary>
        Task<bool> CodeExistsAsync(string code, long excludeId = 0, CancellationToken cancellationToken = default);
    }
}