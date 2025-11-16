using FakeMicro.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SqlSugar;
using FakeMicro.DatabaseAccess.Interfaces;

namespace FakeMicro.DatabaseAccess.Repositories
{
    /// <summary>
    /// 科目仓储实现
    /// </summary>
    public class SubjectRepository : SqlSugarRepository<Subject, long>, ISubjectRepository
    {
        public SubjectRepository(ISqlSugarClient db, ILogger<SubjectRepository> logger)
            : base(db, logger)
        {
        }
         
        /// <summary>
        /// 根据科目代码获取科目
        /// </summary>
        public async Task<Subject?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await GetSqlSugarClient().Queryable<Subject>()
                    .Where(s => s.code == code)
                    .FirstAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"根据科目代码获取科目失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 根据科目名称获取科目
        /// </summary>
        public async Task<Subject?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await GetSqlSugarClient().Queryable<Subject>()
                    .Where(s => s.name == name)
                    .FirstAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"根据科目名称获取科目失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 获取所有科目列表
        /// </summary>
        public async Task<List<Subject>> GetAllSubjectsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await GetSqlSugarClient().Queryable<Subject>()
                    .OrderBy(s => s.name)
                    .ToListAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"获取所有科目列表失败: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// 获取科目列表（注意：Subject实体没有grade属性，此方法返回所有科目）
        /// </summary>
        public async Task<List<Subject>> GetSubjectsByGradeAsync(string grade, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // Subject实体没有grade属性，返回所有科目并按名称排序
                return await GetSqlSugarClient().Queryable<Subject>()
                    .OrderBy(s => s.name)
                    .ToListAsync();
            }
            catch (SqlSugarException ex)
            {
                throw new Exception($"获取科目列表失败: {ex.Message}", ex);
            }
        }
    }
}