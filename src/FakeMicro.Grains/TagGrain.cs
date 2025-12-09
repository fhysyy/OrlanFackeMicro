
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Entities;
using FakeMicro.Interfaces;
using Microsoft.Extensions.Logging;

namespace FakeMicro.Grains
{
    /// <summary>
    /// 标签Grain实现
    /// </summary>
    public class TagGrain : Grain, ITagGrain
    {
        private readonly ILogger<TagGrain> _logger;
        private readonly ITagRepository _repository; 

        public TagGrain(
            ILogger<TagGrain> logger,
            ITagRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        /// <summary>
        /// 获取标签
        /// </summary>
        public async Task<Tag?> GetAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var idString = this.GetPrimaryKeyString();
                if (string.IsNullOrEmpty(idString) || !Guid.TryParse(idString, out var grainId))
                {
                    _logger.LogWarning("无效的Grain ID: {GrainId}", idString);
                    return null;
                }

                return await _repository.GetByIdAsync(grainId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取标签时发生错误: {Id}", this.GetPrimaryKeyString());
                throw;
            }
        }

        /// <summary>
        /// 创建标签
        /// </summary>
        public async Task<Tag?> CreateAsync(Tag tag, CancellationToken cancellationToken = default)
        {
            try
            {
                var idString = this.GetPrimaryKeyString();
                if (string.IsNullOrEmpty(idString) || !Guid.TryParse(idString, out var grainId))
                {
                    _logger.LogWarning("无效的Grain ID: {GrainId}", idString);
                    return null;
                }

                // 设置默认值
                tag.Id = grainId;
                tag.created_at = DateTime.UtcNow;
                tag.updated_at = DateTime.UtcNow;

                await _repository.AddAsync(tag, cancellationToken);
                _logger.LogInformation("标签创建成功: {Id}", tag.Id);

                return tag;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建标签时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 更新标签
        /// </summary>
        public async Task<Tag?> UpdateAsync(Tag tag, CancellationToken cancellationToken = default)
        {
            try
            {
                var idString = this.GetPrimaryKeyString();
                if (string.IsNullOrEmpty(idString) || !Guid.TryParse(idString, out var grainId))
                {
                    _logger.LogWarning("无效的Grain ID: {GrainId}", idString);
                    return null;
                }

                var existingTag = await _repository.GetByIdAsync(grainId, cancellationToken);
                if (existingTag == null)
                {
                    _logger.LogWarning("标签不存在: {Id}", grainId);
                    return null;
                }

                // 更新字段
                tag.Id = grainId;
                tag.created_at = existingTag.created_at;
                tag.updated_at = DateTime.UtcNow;

                await _repository.UpdateAsync(tag, cancellationToken);
                _logger.LogInformation("标签更新成功: {Id}", tag.Id);

                return tag;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新标签时发生错误: {Id}", this.GetPrimaryKeyString());
                throw;
            }
        }

        /// <summary>
        /// 删除标签
        /// </summary>
        public async Task<bool> DeleteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var idString = this.GetPrimaryKeyString();
                if (string.IsNullOrEmpty(idString) || !Guid.TryParse(idString, out var grainId))
                {
                    _logger.LogWarning("无效的Grain ID: {GrainId}", idString);
                    return false;
                }

                await _repository.DeleteByIdAsync(grainId, cancellationToken);
                _logger.LogInformation("标签删除成功: {Id}", grainId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除标签时发生错误: {Id}", this.GetPrimaryKeyString());
                throw;
            }
        }

        /// <summary>
        /// 根据用户ID获取标签列表
        /// </summary>
        public async Task<List<Tag>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var tags = await _repository.GetByUserIdAsync(userId, cancellationToken);
                return tags.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据用户ID获取标签列表时发生错误: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 根据用户ID和标签名称获取标签
        /// </summary>
        public async Task<Tag?> GetByUserIdAndNameAsync(Guid userId, string name, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _repository.GetByUserIdAndNameAsync(userId, name, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据用户ID和标签名称获取标签时发生错误: {UserId}, {Name}", userId, name);
                throw;
            }
        }

        /// <summary>
        /// 检查标签名称是否存在
        /// </summary>
        public async Task<bool> NameExistsAsync(Guid userId, string name, Guid excludeId = default, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _repository.NameExistsAsync(userId, name, excludeId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查标签名称是否存在时发生错误: {UserId}, {Name}, {ExcludeId}", userId, name, excludeId);
                throw;
            }
        }
    }
}