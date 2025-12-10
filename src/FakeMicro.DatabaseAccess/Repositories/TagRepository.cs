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
    /// 标签仓储实现类
    /// 遵循DDD原则，集成MongoDB和Orleans架构
    /// </summary>
    public class TagRepository : MongoRepository<NoteTag, Guid>, ITagRepository
    {
        private readonly ILogger<TagRepository> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="mongoClient">MongoDB客户端</param>
        /// <param name="logger">日志记录器</param>
        public TagRepository(
            MongoClient mongoClient,
            ILogger<TagRepository> logger,
            string? defaultDatabaseName = null
        ) : base(mongoClient, logger, defaultDatabaseName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 通过Orleans Grain ID获取标签实体
        /// </summary>
        public new async Task<NoteTag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await base.GetByIdAsync(id, cancellationToken);
        }

        /// <summary>
        /// 创建标签实体并返回创建后的实体
        /// </summary>
        public async Task<NoteTag?> CreateAndReturnAsync(NoteTag entity, CancellationToken cancellationToken = default)
        {
            await base.AddAsync(entity, cancellationToken);
            return entity;
        }

        /// <summary>
        /// 更新标签实体并返回更新后的实体
        /// </summary>
        public async Task<NoteTag?> UpdateAndReturnAsync(NoteTag entity, CancellationToken cancellationToken = default)
        {
            await base.UpdateAsync(entity, cancellationToken);
            return entity;
        }

        /// <summary>
        /// 根据用户ID获取标签列表
        /// </summary>
        public async Task<IEnumerable<NoteTag>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await base.GetByConditionAsync(x => x.UserId == userId, cancellationToken);
        }

        /// <summary>
        /// 根据用户ID和标签名称获取标签
        /// </summary>
        public async Task<NoteTag?> GetByUserIdAndNameAsync(Guid userId, string name, CancellationToken cancellationToken = default)
        {
            var tags = await base.GetByConditionAsync(x => x.UserId == userId && x.Name == name, cancellationToken);
            return tags.FirstOrDefault();
        }

        /// <summary>
        /// 检查标签名称是否存在
        /// </summary>
        public async Task<bool> NameExistsAsync(Guid userId, string name, Guid excludeId = default, CancellationToken cancellationToken = default)
        {
            Expression<Func<NoteTag, bool>> predicate;
            if (excludeId != default)
            {
                predicate = x => x.UserId == userId && x.Name == name && x.NoteId != excludeId;
            }
            else
            {
                predicate = x => x.UserId == userId && x.Name == name;
            }
            return await base.ExistsAsync(predicate, cancellationToken);
        }

        /// <summary>
        /// 获取标签分页列表
        /// </summary>
        public async Task<PagedResult<NoteTag>> GetPagedAsync(int pageNumber, int pageSize,
            Expression<Func<NoteTag, object>>? orderBy = null, CancellationToken cancellationToken = default)
        {
            var orderByExpression = orderBy ?? (x => x.Name);
            return await base.GetPagedAsync(pageNumber, pageSize, orderByExpression, false, cancellationToken);
        }

        /// <summary>
        /// 根据条件获取标签分页列表
        /// </summary>
        public async Task<PagedResult<NoteTag>> GetPagedByConditionAsync(
            Expression<Func<NoteTag, bool>> predicate, int pageNumber, int pageSize,
            Expression<Func<NoteTag, object>>? orderBy = null, CancellationToken cancellationToken = default)
        {
            var orderByExpression = orderBy ?? (x => x.Name);
            return await base.GetPagedByConditionAsync(predicate, pageNumber, pageSize, orderByExpression, false, cancellationToken);
        }
    }
}