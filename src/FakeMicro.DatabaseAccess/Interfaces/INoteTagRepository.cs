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
    /// 标签仓储接口
    /// 遵循DDD原则，集成MongoDB和Orleans架构
    /// </summary>
    public interface INoteTagRepository: IMongoRepository<NoteTag, Guid>
    {
        #region Orleans Grain特定操作

        /// <summary>
        /// 通过Orleans Grain ID获取标签实体
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>实体对象，如果不存在则返回null</returns>
        new Task<NoteTag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// 创建标签实体并返回创建后的实体
        /// </summary>
        /// <param name="entity">标签实体</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>创建后的实体对象</returns>
        Task<NoteTag?> CreateAndReturnAsync(NoteTag entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// 更新标签实体并返回更新后的实体
        /// </summary>
        /// <param name="entity">标签实体</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新后的实体对象</returns>
        Task<NoteTag?> UpdateAndReturnAsync(NoteTag entity, CancellationToken cancellationToken = default);

        #endregion

        #region 业务操作

        /// <summary>
        /// 根据用户ID获取标签列表
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>标签列表</returns>
        Task<IEnumerable<NoteTag>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据用户ID和标签名称获取标签
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="name">标签名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>标签对象，如果不存在则返回null</returns>
        Task<NoteTag?> GetByUserIdAndNameAsync(Guid userId, string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查标签名称是否存在
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="name">标签名称</param>
        /// <param name="excludeId">排除的ID（用于更新时验证）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否存在</returns>
        Task<bool> NameExistsAsync(Guid userId, string name, Guid excludeId = default, CancellationToken cancellationToken = default);

        #endregion

        #region Orleans分页查询扩展

        /// <summary>
        /// 获取标签分页列表
        /// </summary>
        /// <param name="pageNumber">页码（从1开始）</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="orderBy">排序表达式</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>分页结果</returns>
        Task<PagedResult<NoteTag>> GetPagedAsync(int pageNumber, int pageSize, 
            Expression<Func<NoteTag, object>>? orderBy = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据条件获取标签分页列表
        /// </summary>
        /// <param name="predicate">查询条件</param>
        /// <param name="pageNumber">页码（从1开始）</param>
        /// <param name="pageSize">每页大小</param>
        /// <param name="orderBy">排序表达式</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>分页结果</returns>
        Task<PagedResult<NoteTag>> GetPagedByConditionAsync(
            Expression<Func<NoteTag, bool>> predicate, int pageNumber, int pageSize,
            Expression<Func<NoteTag, object>>? orderBy = null, CancellationToken cancellationToken = default);

        #endregion
    }
}