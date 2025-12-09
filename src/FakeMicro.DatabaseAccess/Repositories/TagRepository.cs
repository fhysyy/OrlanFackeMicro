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
    /// </summary>
    public class TagRepository : MongoRepository<FakeMicro.Entities.Tag, Guid>, ITagRepository
    {
        private readonly ILogger<TagRepository> _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="mongoClient">MongoDB客户端</param>
        /// <param name="logger">日志记录器</param>
        public TagRepository(MongoClient mongoClient, ILogger<TagRepository> logger) : base(mongoClient, logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 通过ID获取标签
        /// </summary>
        public new async Task<FakeMicro.Entities.Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await base.GetByIdAsync(id, cancellationToken);
        }

        /// <summary>
        /// 创建标签并返回
        /// </summary>
        public async Task<FakeMicro.Entities.Tag?> CreateAndReturnAsync(FakeMicro.Entities.Tag entity, CancellationToken cancellationToken = default)
        {
            await base.AddAsync(entity, cancellationToken);
            return entity;
        }

        /// <summary>
        /// 更新标签并返回
        /// </summary>
        public async Task<FakeMicro.Entities.Tag?> UpdateAndReturnAsync(FakeMicro.Entities.Tag entity, CancellationToken cancellationToken = default)
        {
            await base.UpdateAsync(entity, cancellationToken);
            return entity;
        }

        /// <summary>
        /// 根据用户ID获取标签列表
        /// </summary>
        public async Task<IEnumerable<FakeMicro.Entities.Tag>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await base.GetByConditionAsync(x => x.UserId == userId, cancellationToken);
        }

        /// <summary>
        /// 根据用户ID和标签名称获取标签
        /// </summary>
        public async Task<FakeMicro.Entities.Tag?> GetByUserIdAndNameAsync(Guid userId, string name, CancellationToken cancellationToken = default)
        {
            return await base.GetByConditionAsync(x => x.UserId == userId && x.Name == name, cancellationToken).ContinueWith(t => t.Result.FirstOrDefault(), cancellationToken);
        }

        /// <summary>
        /// 检查标签名称是否存在
        /// </summary>
        public async Task<bool> NameExistsAsync(Guid userId, string name, Guid excludeId = default, CancellationToken cancellationToken = default)
        {
            Expression<Func<FakeMicro.Entities.Tag, bool>> predicate = x => x.UserId == userId && x.Name == name;
            if (excludeId != default(Guid))
            {
                predicate = x => x.UserId == userId && x.Name == name && x.Id != excludeId;
            }
            return await base.ExistsAsync(predicate, cancellationToken);
        }

        /// <summary>
        /// 获取标签分页列表
        /// </summary>
        public async Task<PagedResult<FakeMicro.Entities.Tag>> GetPagedAsync(int pageNumber, int pageSize, 
            Expression<Func<FakeMicro.Entities.Tag, object>>? orderBy = null, CancellationToken cancellationToken = default)
        {
            return await base.GetPagedAsync(pageNumber, pageSize, orderBy, false, cancellationToken);
        }

        /// <summary>
        /// 根据条件获取标签分页列表
        /// </summary>
        public async Task<PagedResult<FakeMicro.Entities.Tag>> GetPagedByConditionAsync(
            Expression<Func<FakeMicro.Entities.Tag, bool>> predicate, int pageNumber, int pageSize,
            Expression<Func<FakeMicro.Entities.Tag, object>>? orderBy = null, CancellationToken cancellationToken = default)
        {
            return await base.GetPagedByConditionAsync(predicate, pageNumber, pageSize, orderBy, false, cancellationToken);
        }
    }
}