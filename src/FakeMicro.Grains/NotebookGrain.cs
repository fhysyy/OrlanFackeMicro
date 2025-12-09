using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Orleans;
using FakeMicro.Interfaces;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Entities;

namespace FakeMicro.Grains
{
    /// <summary>
    /// 笔记本Grain实现
    /// </summary>
    public class NotebookGrain : Grain,INotebookGrain
    {
        private readonly ILogger<NotebookGrain> _logger;
        private readonly INotebookRepository _repository;

        public NotebookGrain(
            ILogger<NotebookGrain> logger,
            INotebookRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        /// <summary>
        /// 获取笔记本
        /// </summary>
        public async Task<Notebook?> GetAsync(CancellationToken cancellationToken = default)
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
                _logger.LogError(ex, "获取笔记本时发生错误: {Id}", this.GetPrimaryKeyString());
                throw;
            }
        }

        /// <summary>
        /// 创建笔记本
        /// </summary>
        public async Task<Notebook?> CreateAsync(Notebook notebook, CancellationToken cancellationToken = default)
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
                notebook.Id = grainId;
                notebook.created_at = DateTime.UtcNow;
                notebook.updated_at = DateTime.UtcNow;

                await _repository.AddAsync(notebook, cancellationToken);
                _logger.LogInformation("笔记本创建成功: {Id}", notebook.Id);

                return notebook;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建笔记本时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 更新笔记本
        /// </summary>
        public async Task<Notebook?> UpdateAsync(Notebook notebook, CancellationToken cancellationToken = default)
        {
            try
            {
                var idString = this.GetPrimaryKeyString();
                if (string.IsNullOrEmpty(idString) || !Guid.TryParse(idString, out var grainId))
                {
                    _logger.LogWarning("无效的Grain ID: {GrainId}", idString);
                    return null;
                }

                var existingNotebook = await _repository.GetByIdAsync(grainId, cancellationToken);
                if (existingNotebook == null)
                {
                    _logger.LogWarning("笔记本不存在: {Id}", grainId);
                    return null;
                }

                // 更新字段
                notebook.Id = grainId;
                notebook.created_at = existingNotebook.created_at;
                notebook.updated_at = DateTime.UtcNow;

                await _repository.UpdateAsync(notebook, cancellationToken);
                _logger.LogInformation("笔记本更新成功: {Id}", notebook.Id);

                return notebook;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新笔记本时发生错误: {Id}", this.GetPrimaryKeyString());
                throw;
            }
        }

        /// <summary>
        /// 删除笔记本
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
                _logger.LogInformation("笔记本删除成功: {Id}", grainId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除笔记本时发生错误: {Id}", this.GetPrimaryKeyString());
                throw;
            }
        }

        /// <summary>
        /// 根据用户ID获取笔记本列表
        /// </summary>
        public async Task<List<Notebook>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var notebooks = await _repository.GetByUserIdAsync(userId, cancellationToken);
                return notebooks.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据用户ID获取笔记本列表时发生错误: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 根据用户ID和父笔记本ID获取笔记本列表
        /// </summary>
        public async Task<List<Notebook>> GetByUserIdAndParentIdAsync(Guid userId, Guid? parentId, CancellationToken cancellationToken = default)
        {
            try
            {
                var notebooks = await _repository.GetByUserIdAndParentIdAsync(userId, parentId, cancellationToken);
                return notebooks.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据用户ID和父笔记本ID获取笔记本列表时发生错误: {UserId}, {ParentId}", userId, parentId);
                throw;
            }
        }
    }
}