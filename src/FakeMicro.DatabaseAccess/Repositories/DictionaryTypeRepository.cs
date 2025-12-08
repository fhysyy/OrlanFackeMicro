using FakeMicro.Entities;
using SqlSugar;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using FakeMicro.DatabaseAccess.Interfaces;

namespace FakeMicro.DatabaseAccess.Repositories
{
    /// <summary>
    /// 字典类型仓储实现
    /// </summary>
    public class DictionaryTypeRepository : SqlSugarRepository<DictionaryType, long>, IDictionaryTypeRepository
    {
        public DictionaryTypeRepository(ISqlSugarClient db, 
            ILogger<DictionaryTypeRepository> logger) 
            : base(db, logger)
        {
        }

        /// <summary>
        /// 根据编码获取字典类型
        /// </summary>
        public async Task<DictionaryType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await GetSqlSugarClient().Queryable<DictionaryType>()
                    .Where(dt => dt.Code == code)
                    .FirstAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"根据编码获取字典类型失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 检查编码是否存在
        /// </summary>
        public async Task<bool> CodeExistsAsync(string code, long excludeId = 0, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var query = GetSqlSugarClient().Queryable<DictionaryType>()
                    .Where(dt => dt.Code == code);
                
                if (excludeId > 0)
                {
                    query = query.Where(dt => dt.Id != excludeId);
                }
                
                var count = await query.CountAsync();
                return count > 0;
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"检查字典类型编码是否存在失败: {ex.Message}", ex);
            }
        }

        // 实现IWriteRepository接口的同步方法
        public void Update(DictionaryType entity)
        {
            // 调用异步方法并同步等待
            UpdateAsync(entity).GetAwaiter().GetResult();
        }

        public void UpdateRange(IEnumerable<DictionaryType> entities)
        {
            // 调用异步方法并同步等待
            UpdateRangeAsync(entities).GetAwaiter().GetResult();
        }

        public void Delete(DictionaryType entity)
        {
            // 调用异步方法并同步等待
            DeleteAsync(entity).GetAwaiter().GetResult();
        }

        public void DeleteRange(IEnumerable<DictionaryType> entities)
        {
            // 调用异步方法并同步等待
            DeleteRangeAsync(entities).GetAwaiter().GetResult();
        }

        // 实现不同参数版本的接口方法
        public async Task<(IEnumerable<DictionaryType> Items, int TotalCount)> GetPagedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            // 转换为我们已实现的分页方法格式
            var result = await base.GetPagedAsync(pageIndex + 1, pageSize, x => x.Id, false, cancellationToken);
            return (result.Data, result.TotalCount);
        }

        public async Task<(IEnumerable<DictionaryType> Items, int TotalCount)> GetPagedByConditionAsync(
            Expression<Func<DictionaryType, bool>> predicate, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            // 转换为我们已实现的分页方法格式
            var result = await base.GetPagedByConditionAsync(predicate, pageIndex + 1, pageSize, x => x.Id, false, cancellationToken);
            return (result.Data, result.TotalCount);
        }

        public void UpdatePartial(DictionaryType entity, params Expression<Func<DictionaryType, object>>[] properties)
        {
            // 调用异步方法并同步等待
            UpdatePartialAsync(entity, properties).GetAwaiter().GetResult();
        }
    }
}