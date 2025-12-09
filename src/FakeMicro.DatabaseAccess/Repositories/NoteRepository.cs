using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Entities;
using FakeMicro.Interfaces.Models;

namespace FakeMicro.DatabaseAccess.Repositories
{
    /// <summary>
    /// 笔记仓储实现类
    /// </summary>
    public class NoteRepository : MongoRepository<Note, Guid>, INoteRepository
    {
        private readonly ILogger<NoteRepository> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="mongoClient">MongoDB客户端</param>
        /// <param name="logger">日志记录器</param>
        public NoteRepository(MongoClient mongoClient, ILogger<NoteRepository> logger) : base(mongoClient, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 通过ID获取笔记
        /// </summary>
        public new async Task<Note?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await base.GetByIdAsync(id, cancellationToken);
        }

        /// <summary>
        /// 创建笔记并返回
        /// </summary>
        public async Task<Note?> CreateAndReturnAsync(Note entity, CancellationToken cancellationToken = default)
        {
            await base.AddAsync(entity, cancellationToken);
            return entity;
        }

        /// <summary>
        /// 更新笔记并返回
        /// </summary>
        public async Task<Note?> UpdateAndReturnAsync(Note entity, CancellationToken cancellationToken = default)
        {
            await base.UpdateAsync(entity, cancellationToken);
            return entity;
        }

        /// <summary>
        /// 根据用户ID获取笔记列表
        /// </summary>
        public async Task<IEnumerable<Note>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await base.GetByConditionAsync(x => x.UserId == userId, cancellationToken);
        }

        /// <summary>
        /// 根据笔记本ID获取笔记列表
        /// </summary>
        public async Task<IEnumerable<Note>> GetByNotebookIdAsync(Guid notebookId, CancellationToken cancellationToken = default)
        {
            return await base.GetByConditionAsync(x => x.NotebookId == notebookId, cancellationToken);
        }

        /// <summary>
        /// 根据用户ID和笔记本ID获取笔记列表
        /// </summary>
        public async Task<IEnumerable<Note>> GetByUserIdAndNotebookIdAsync(Guid userId, Guid notebookId, CancellationToken cancellationToken = default)
        {
            return await base.GetByConditionAsync(x => x.UserId == userId && x.NotebookId == notebookId, cancellationToken);
        }

        /// <summary>
        /// 软删除笔记
        /// </summary>
        public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var note = await base.GetByIdAsync(id, cancellationToken);
            if (note == null)
            {
                return false;
            }

            note.IsDeleted = true;
            note.DeletedAt = DateTime.UtcNow;
            await base.UpdateAsync(note, cancellationToken);
            return true;
        }

        /// <summary>
        /// 根据标签ID获取笔记列表
        /// </summary>
        public async Task<IEnumerable<Note>> GetByTagIdAsync(Guid tagId, CancellationToken cancellationToken = default)
        {
            // 这里需要通过NoteTag关联表查询，暂时返回空列表
            return new List<Note>();
        }

        /// <summary>
        /// 获取笔记分页列表
        /// </summary>
        public async Task<PagedResult<Note>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<Note, object>>? orderBy = null, CancellationToken cancellationToken = default)
        {
            var orderByExpression = orderBy ?? (x => x.UpdatedAt);
            return await base.GetPagedAsync(pageNumber, pageSize, orderByExpression, true, cancellationToken);
        }

        /// <summary>
        /// 根据条件获取笔记分页列表
        /// </summary>
        public async Task<PagedResult<Note>> GetPagedByConditionAsync(Expression<Func<Note, bool>> predicate, int pageNumber, int pageSize, Expression<Func<Note, object>>? orderBy = null, CancellationToken cancellationToken = default)
        {
            var orderByExpression = orderBy ?? (x => x.UpdatedAt);
            return await base.GetPagedByConditionAsync(predicate, pageNumber, pageSize, orderByExpression, true, cancellationToken);
        }
    }
}