using SqlSugar;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Entities;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace FakeMicro.DatabaseAccess.Repositories
{
    /// <summary>
    /// 字典项仓储实现
    /// </summary>
    public class DictionaryItemRepository : SqlSugarRepository<DictionaryItem, long>, IDictionaryItemRepository
    {
        public DictionaryItemRepository(ISqlSugarClient db, 
            ILogger<DictionaryItemRepository> logger) 
            : base(db, logger)
        {
        }

        /// <summary>
        /// 根据字典类型ID获取字典项列表
        /// </summary>
        public async Task<IEnumerable<DictionaryItem>> GetByDictionaryTypeIdAsync(long dictionaryTypeId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await GetSqlSugarClient().Queryable<DictionaryItem>()
                    .Where(di => di.DictionaryTypeId == dictionaryTypeId)
                    .OrderBy(di => di.SortOrder)
                    .OrderBy(di => di.Id)
                    .ToListAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"根据字典类型ID获取字典项列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 根据字典类型编码获取字典项列表
        /// </summary>
        public async Task<IEnumerable<DictionaryItem>> GetByDictionaryTypeCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await GetSqlSugarClient().Queryable<DictionaryItem>()
                    .LeftJoin<DictionaryType>((dictionaryItem, dictionaryType) => dictionaryItem.DictionaryTypeId == dictionaryType.Id)
                    .Where("dictionaryType.code = @code AND dictionaryType.is_enabled = 1 AND dictionaryItem.is_enabled = 1", new { code })
                    .OrderBy("dictionaryItem.sort_order, dictionaryItem.id")
                    .Select<DictionaryItem>()
                    .ToListAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"根据字典类型编码获取字典项列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 根据字典类型ID和值获取字典项
        /// </summary>
        public async Task<DictionaryItem?> GetByTypeIdAndValueAsync(long dictionaryTypeId, string value, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await GetSqlSugarClient().Queryable<DictionaryItem>()
                    .Where(di => di.DictionaryTypeId == dictionaryTypeId && di.Value == value)
                    .FirstAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"根据字典类型ID和值获取字典项失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 批量删除指定字典类型的所有字典项
        /// </summary>
        public async Task<int> DeleteByDictionaryTypeIdAsync(long dictionaryTypeId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await GetSqlSugarClient().Deleteable<DictionaryItem>()
                    .Where(di => di.DictionaryTypeId == dictionaryTypeId)
                    .ExecuteCommandAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"批量删除字典项失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 检查在同一字典类型下值是否存在
        /// </summary>
        public async Task<bool> ValueExistsAsync(long dictionaryTypeId, string value, long excludeId = 0, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var query = GetSqlSugarClient().Queryable<DictionaryItem>()
                    .Where(di => di.DictionaryTypeId == dictionaryTypeId && di.Value == value);
                
                if (excludeId > 0)
                {
                    query = query.Where(di => di.Id != excludeId);
                }
                
                var count = await query.CountAsync();
                return count > 0;
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"检查字典项值是否存在失败: {ex.Message}", ex);
            }
        }

 
        // 实现IWriteRepository接口的同步方法
        public void Update(DictionaryItem entity)
        {
            // 调用异步方法并同步等待
            UpdateAsync(entity).GetAwaiter().GetResult();
        }

        public void UpdateRange(IEnumerable<DictionaryItem> entities)
        {
            // 调用异步方法并同步等待
            UpdateRangeAsync(entities).GetAwaiter().GetResult();
        }

        public void Delete(DictionaryItem entity)
        {
            // 调用异步方法并同步等待
            DeleteAsync(entity).GetAwaiter().GetResult();
        }

        public void DeleteRange(IEnumerable<DictionaryItem> entities)
        {
            // 调用异步方法并同步等待
            DeleteRangeAsync(entities).GetAwaiter().GetResult();
        }

        // 实现不同参数版本的接口方法
        public async Task<(IEnumerable<DictionaryItem> Items, int TotalCount)> GetPagedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            // 转换为我们已实现的分页方法格式
            var result = await base.GetPagedAsync(pageIndex + 1, pageSize, x => x.Id, false, cancellationToken);
            return (result.Data, result.TotalCount);
        }

        public async Task<(IEnumerable<DictionaryItem> Items, int TotalCount)> GetPagedByConditionAsync(
            Expression<Func<DictionaryItem, bool>> predicate, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            // 转换为我们已实现的分页方法格式
            var result = await base.GetPagedByConditionAsync(predicate, pageIndex + 1, pageSize, x => x.Id, false, cancellationToken);
            return (result.Data, result.TotalCount);
        }

        public void UpdatePartial(DictionaryItem entity, params Expression<Func<DictionaryItem, object>>[] properties)
        {
            // 调用异步方法并同步等待
            UpdatePartialAsync(entity, properties).GetAwaiter().GetResult();
        }
    }
}