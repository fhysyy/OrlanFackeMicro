using System.Collections.Generic;
using System.Threading.Tasks;
using FakeMicro.DatabaseAccess;
using FakeMicro.Entities;
using FakeMicro.Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;

namespace FakeMicro.Grains
{
    /// <summary>
    /// 字典项管理Grain实现
    /// </summary>
    public class DictionaryItemGrain : Grain, IDictionaryItemGrain
    {
        private readonly IDictionaryItemRepository _dictionaryItemRepository;
        private readonly ILogger<DictionaryItemGrain> _logger;

        public DictionaryItemGrain(IDictionaryItemRepository dictionaryItemRepository, ILogger<DictionaryItemGrain> logger)
        {
            _dictionaryItemRepository = dictionaryItemRepository;
            _logger = logger;
        }

        public async Task<DictionaryItem> GetDictionaryItemAsync()
        {
            try
            {
                var primaryKeyString = this.GetPrimaryKeyString();
                if (!long.TryParse(primaryKeyString, out long id))
                {
                    _logger.LogError("无效的字典项ID格式: {Id}", primaryKeyString);
                    throw new ArgumentException("无效的字典项ID格式", nameof(primaryKeyString));
                }
                return await _dictionaryItemRepository.GetByIdAsync(id);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "获取字典项详情失败: {Id}", this.GetPrimaryKeyString());
                throw;
            }
        }

        public async Task<DictionaryItem> CreateDictionaryItemAsync(DictionaryItem dictionaryItem)
        {
            try
            {
                await _dictionaryItemRepository.AddAsync(dictionaryItem);
                return dictionaryItem;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "创建字典项失败: {Value}", dictionaryItem.Value);
                throw;
            }
        }

        public async Task<DictionaryItem> UpdateDictionaryItemAsync(DictionaryItem dictionaryItem)
        {
            try
            {
                await _dictionaryItemRepository.UpdateAsync(dictionaryItem);
                return dictionaryItem;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "更新字典项失败: {Id}", dictionaryItem.Id);
                throw;
            }
        }

        public async Task<bool> DeleteDictionaryItemAsync()
        {
            try
            {
                var primaryKeyString = this.GetPrimaryKeyString();
                if (!long.TryParse(primaryKeyString, out long id))
                {
                    _logger.LogError("无效的字典项ID格式: {Id}", primaryKeyString);
                    throw new ArgumentException("无效的字典项ID格式", nameof(primaryKeyString));
                }
                await _dictionaryItemRepository.DeleteByIdAsync(id);
                return true;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "删除字典项失败: {Id}", this.GetPrimaryKeyString());
                throw;
            }
        }

        public async Task<bool> ValueExistsAsync(long dictionaryTypeId, string value, long excludeId = 0)
        {
            try
            {
                return await _dictionaryItemRepository.ValueExistsAsync(dictionaryTypeId, value, excludeId);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "检查字典项值是否存在失败: {Value}", value);
                throw;
            }
        }
    }

    /// <summary>
    /// 字典项管理服务Grain实现
    /// </summary>
    public class DictionaryItemService : Grain, IDictionaryItemService
    {
        private readonly IDictionaryItemRepository _dictionaryItemRepository;
        private readonly ILogger<DictionaryItemService> _logger;

        public DictionaryItemService(IDictionaryItemRepository dictionaryItemRepository, ILogger<DictionaryItemService> logger)
        {
            _dictionaryItemRepository = dictionaryItemRepository;
            _logger = logger;
        }

        public async Task<List<DictionaryItem>> GetByDictionaryTypeIdAsync(long typeId)
        {
            try
            {
                var items = await _dictionaryItemRepository.GetByDictionaryTypeIdAsync(typeId);
                return items.ToList();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "根据字典类型ID获取字典项列表失败: {TypeId}", typeId);
                throw;
            }
        }

        public async Task<List<DictionaryItem>> GetByDictionaryTypeCodeAsync(string code)
        {
            try
            {
                var items = await _dictionaryItemRepository.GetByDictionaryTypeCodeAsync(code);
                return items.ToList();
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "根据字典类型编码获取字典项列表失败: {Code}", code);
                throw;
            }
        }
    }
}