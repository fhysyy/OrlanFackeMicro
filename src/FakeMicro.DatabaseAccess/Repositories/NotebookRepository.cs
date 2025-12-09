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
    /// 笔记本仓储实现类
    /// </summary>
    public class NotebookRepository : MongoRepository<Notebook, Guid>, INotebookRepository
    {
        private readonly ILogger<NotebookRepository> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="mongoClient">MongoDB客户端</param>
        /// <param name="logger">日志记录器</param>
        public NotebookRepository(MongoClient mongoClient, ILogger<NotebookRepository> logger) : base(mongoClient, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 通过ID获取笔记本
        /// </summary>
        public new async Task<Notebook?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await base.GetByIdAsync(id, cancellationToken);
        }

        /// <summary>
        /// 创建笔记本并返回
        /// </summary>
        public async Task<Notebook?> CreateAndReturnAsync(Notebook entity, CancellationToken cancellationToken = default)
        {
            await base.AddAsync(entity, cancellationToken);
            return entity;
        }

        /// <summary>
        /// 更新笔记本并返回
        /// </summary>
        public async Task<Notebook?> UpdateAndReturnAsync(Notebook entity, CancellationToken cancellationToken = default)
        {
            await base.UpdateAsync(entity, cancellationToken);
            return entity;
        }

        /// <summary>
        /// 根据用户ID获取笔记本列表
        /// </summary>
        public async Task<IEnumerable<Notebook>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await base.GetByConditionAsync(x => x.UserId == userId, cancellationToken);
        }

        /// <summary>
        /// 根据用户ID和父笔记本ID获取笔记本列表
        /// </summary>
        public async Task<IEnumerable<Notebook>> GetByUserIdAndParentIdAsync(Guid userId, Guid? parentId, CancellationToken cancellationToken = default)
        {
            if (parentId.HasValue)
            {
                return await base.GetByConditionAsync(x => x.UserId == userId && x.ParentId == parentId, cancellationToken);
            }
            return await base.GetByConditionAsync(x => x.UserId == userId && x.ParentId == null, cancellationToken);
        }

        /// <summary>
        /// 获取笔记本分页列表
        /// </summary>
        public async Task<PagedResult<Notebook>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<Notebook, object>>? orderBy = null, CancellationToken cancellationToken = default)
        {
            var orderByExpression = orderBy ?? (x => x.SortOrder);
            return await base.GetPagedAsync(pageNumber, pageSize, orderByExpression, false, cancellationToken);
        }

        /// <summary>
        /// 根据条件获取笔记本分页列表
        /// </summary>
        public async Task<PagedResult<Notebook>> GetPagedByConditionAsync(Expression<Func<Notebook, bool>> predicate, int pageNumber, int pageSize, Expression<Func<Notebook, object>>? orderBy = null, CancellationToken cancellationToken = default)
        {
            var orderByExpression = orderBy ?? (x => x.SortOrder);
            return await base.GetPagedByConditionAsync(predicate, pageNumber, pageSize, orderByExpression, false, cancellationToken);
        }
    }
}