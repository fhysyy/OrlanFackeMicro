using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq.Expressions;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Entities;
using FakeMicro.Interfaces.Models;

namespace FakeMicro.DatabaseAccess.Interfaces
{
    /// <summary>
    /// 笔记本仓储接口
    /// 遵循DDD原则，集成MongoDB和Orleans架构
    /// </summary>
    public interface INotebookRepository
        : IMongoRepository<Notebook, Guid>
    {
        #region Orleans Grain特定操作

        /// <summary>
        /// 通过Orleans Grain ID获取笔记本实体
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>实体对象，如果不存在则返回null</returns>
        new Task<Notebook?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 创建笔记本实体并返回创建后的实体
        /// </summary>
        /// <param name="entity">笔记本实体</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>创建后的实体对象</returns>
        Task<Notebook?> CreateAndReturnAsync(Notebook entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新笔记本实体并返回更新后的实体
        /// </summary>
        /// <param name="entity">笔记本实体</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新后的实体对象</returns>
        Task<Notebook?> UpdateAndReturnAsync(Notebook entity, CancellationToken cancellationToken = default);

        #endregion

        #region 业务操作

        /// <summary>
        /// 根据用户ID获取笔记本列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>笔记本列表</returns>
        Task<IEnumerable<Notebook>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据用户ID和父笔记本ID获取笔记本列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="parentId">父笔记本ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>笔记本列表</returns>
        Task<IEnumerable<Notebook>> GetByUserIdAndParentIdAsync(Guid userId, Guid? parentId, CancellationToken cancellationToken = default);

        #endregion

        #region Orleans分页查询扩展

        /// <summary>
        /// 获取笔记本分页列表
        /// </summary>
        /// <param name="pageNumber">页码（从1开始）</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="orderBy">排序表达式</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>分页结果</returns>
        Task<PagedResult<Notebook>> GetPagedAsync(int pageNumber, int pageSize, 
            Expression<Func<Notebook, object>>? orderBy = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据条件获取笔记本分页列表
        /// </summary>
        /// <param name="predicate">查询条件</param>
        /// <param name="pageNumber">页码（从1开始）</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="orderBy">排序表达式</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>分页结果</returns>
        Task<PagedResult<Notebook>> GetPagedByConditionAsync(
            Expression<Func<Notebook, bool>> predicate, int pageNumber, int pageSize,
            Expression<Func<Notebook, object>>? orderBy = null, CancellationToken cancellationToken = default);

        #endregion
    }
}