using System.Threading.Tasks;
using FakeMicro.DatabaseAccess;
using FakeMicro.Entities;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
using Microsoft.Extensions.Logging;
using Orleans;

namespace FakeMicro.Grains
{
    /// <summary>
    /// 字典类型管理Grain实现
    /// </summary>
    public class DictionaryTypeGrain : Grain, IDictionaryTypeGrain
    {
        private readonly IDictionaryTypeRepository _dictionaryTypeRepository;
        private readonly ILogger<DictionaryTypeGrain> _logger;

        public DictionaryTypeGrain(IDictionaryTypeRepository dictionaryTypeRepository, ILogger<DictionaryTypeGrain> logger)
        {
            _dictionaryTypeRepository = dictionaryTypeRepository;
            _logger = logger;
        }

        public async Task<DictionaryType> GetDictionaryTypeAsync()
        {
            try
            {
                var primaryKeyString = this.GetPrimaryKeyString();
                if (!long.TryParse(primaryKeyString, out long id))
                {
                    _logger.LogError("无效的字典类型ID格式: {Id}", primaryKeyString);
                    throw new ArgumentException("无效的字典类型ID格式", nameof(primaryKeyString));
                }
                return await _dictionaryTypeRepository.GetByIdAsync(id);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "获取字典类型详情失败: {Id}", this.GetPrimaryKeyString());
                throw;
            }
        }

        public async Task<DictionaryType> CreateDictionaryTypeAsync(DictionaryType dictionaryType)
        {
            try
            {
                await _dictionaryTypeRepository.AddAsync(dictionaryType);
                return dictionaryType;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "创建字典类型失败: {Code}", dictionaryType.Code);
                throw;
            }
        }

        public async Task<DictionaryType> UpdateDictionaryTypeAsync(DictionaryType dictionaryType)
        {
            try
            {
                // 使用异步UpdateAsync方法
                await _dictionaryTypeRepository.UpdateAsync(dictionaryType);
                return dictionaryType;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "更新字典类型失败: {Id}", dictionaryType.Id);
                throw;
            }
        }

        public async Task<bool> DeleteDictionaryTypeAsync()
        {
            try
            {
                var primaryKeyString = this.GetPrimaryKeyString();
                if (!long.TryParse(primaryKeyString, out long id))
                {
                    _logger.LogError("无效的字典类型ID格式: {Id}", primaryKeyString);
                    throw new ArgumentException("无效的字典类型ID格式", nameof(primaryKeyString));
                }
                await _dictionaryTypeRepository.DeleteByIdAsync(id);
                return true;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "删除字典类型失败: {Id}", this.GetPrimaryKeyString());
                throw;
            }
        }

        public async Task<bool> CodeExistsAsync(string code, long excludeId = 0)
        {
            try
            {
                return await _dictionaryTypeRepository.CodeExistsAsync(code, excludeId);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "检查字典类型编码是否存在失败: {Code}", code);
                throw;
            }
        }
    }

    /// <summary>
    /// 字典类型管理服务Grain实现
    /// </summary>
    public class DictionaryTypeService : Grain, IDictionaryTypeService
    {
        private readonly IDictionaryTypeRepository _dictionaryTypeRepository;
        private readonly ILogger<DictionaryTypeService> _logger;

        public DictionaryTypeService(IDictionaryTypeRepository dictionaryTypeRepository, ILogger<DictionaryTypeService> logger)
        {
            _dictionaryTypeRepository = dictionaryTypeRepository;
            _logger = logger;
        }

        public async Task<IPaginatedResult<DictionaryType>> GetDictionaryTypesAsync(int page, int pageSize, string? keyword = null, bool? isEnabled = null)
        {
            try
            {
                // 构建查询条件
                System.Linq.Expressions.Expression<System.Func<DictionaryType, bool>>? predicate = null;
                if (!string.IsNullOrEmpty(keyword) || isEnabled.HasValue)
                {
                    predicate = x =>
                        (string.IsNullOrEmpty(keyword) || x.Name.Contains(keyword) || x.Code.Contains(keyword)) &&
                        (!isEnabled.HasValue || x.IsEnabled == isEnabled.Value);
                }

                // 使用GetPagedByConditionAsync方法并指定排序
                var result = await _dictionaryTypeRepository.GetPagedByConditionAsync(
                    predicate ?? (x => true),
                    page,
                    pageSize,
                    orderBy: x => x.SortOrder, // 按排序号排序
                    isDescending: false);

                // 将项目的分页结果转换为自定义的分页结果
                return new FakeMicro.Interfaces.PaginatedResult<DictionaryType>(
                    result.Items.ToList() as System.Collections.Generic.IReadOnlyCollection<DictionaryType>,
                    result.PageIndex,
                    result.PageSize,
                    result.TotalCount);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "获取字典类型列表失败");
                throw;
            }
        }

       
    }


}