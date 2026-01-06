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
    /// 笔记Grain实现
    /// </summary>
    public class NoteGrain : OrleansGrainBase, INoteGrain
    {
        private readonly INoteRepository _repository;

        public NoteGrain(
            ILogger<NoteGrain> logger,
            INoteRepository repository) : base(logger)
        {
            _repository = repository;
        }

        /// <summary>
        /// 获取笔记
        /// </summary>
        public async Task<Note?> GetAsync(CancellationToken cancellationToken = default)
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
                _logger.LogError(ex, "获取笔记时发生错误: {Id}", this.GetPrimaryKeyString());
                throw;
            }
        }

        /// <summary>
        /// 创建笔记
        /// </summary>
        public async Task<Note?> CreateAsync(Note note, CancellationToken cancellationToken = default)
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
                note.Id = grainId;
                note.CreatedAt = DateTime.UtcNow;
                note.UpdatedAt = DateTime.UtcNow;
                note.Version = 0;
                note.IsDeleted = false;

                await _repository.AddAsync(note, cancellationToken);
                _logger.LogInformation("笔记创建成功: {Id}", note.Id);

                return note;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建笔记时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 更新笔记
        /// </summary>
        public async Task<Note?> UpdateAsync(Note note, CancellationToken cancellationToken = default)
        {
            try
            {
                var idString = this.GetPrimaryKeyString();
                if (string.IsNullOrEmpty(idString) || !Guid.TryParse(idString, out var grainId))
                {
                    _logger.LogWarning("无效的Grain ID: {GrainId}", idString);
                    return null;
                }

                var existingNote = await _repository.GetByIdAsync(grainId, cancellationToken);
                if (existingNote == null)
                {
                    _logger.LogWarning("笔记不存在: {Id}", grainId);
                    return null;
                }

                // 更新字段
                note.Id = grainId;
                note.CreatedAt = existingNote.CreatedAt;
                note.UpdatedAt = DateTime.UtcNow;
                note.Version = existingNote.Version + 1;

                await _repository.UpdateAsync(note, cancellationToken);
                _logger.LogInformation("笔记更新成功: {Id}, 版本: {Version}", note.Id, note.Version);

                return note;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新笔记时发生错误: {Id}", this.GetPrimaryKeyString());
                throw;
            }
        }

        /// <summary>
        /// 删除笔记
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
                _logger.LogInformation("笔记删除成功: {Id}", grainId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除笔记时发生错误: {Id}", this.GetPrimaryKeyString());
                throw;
            }
        }

        /// <summary>
        /// 软删除笔记
        /// </summary>
        public async Task<bool> SoftDeleteAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var idString = this.GetPrimaryKeyString();
                if (string.IsNullOrEmpty(idString) || !Guid.TryParse(idString, out var grainId))
                {
                    _logger.LogWarning("无效的Grain ID: {GrainId}", idString);
                    return false;
                }

                var result = await _repository.SoftDeleteAsync(grainId, cancellationToken);
                if (result)
                {
                    _logger.LogInformation("笔记软删除成功: {Id}", grainId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "软删除笔记时发生错误: {Id}", this.GetPrimaryKeyString());
                throw;
            }
        }

        /// <summary>
        /// 根据用户ID获取笔记列表
        /// </summary>
        public async Task<List<Note>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var notes = await _repository.GetByUserIdAsync(userId, cancellationToken);
                return notes.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据用户ID获取笔记列表时发生错误: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 根据笔记本ID获取笔记列表
        /// </summary>
        public async Task<List<Note>> GetByNotebookIdAsync(Guid notebookId, CancellationToken cancellationToken = default)
        {
            try
            {
                var notes = await _repository.GetByNotebookIdAsync(notebookId, cancellationToken);
                return notes.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据笔记本ID获取笔记列表时发生错误: {NotebookId}", notebookId);
                throw;
            }
        }

        /// <summary>
        /// 根据用户ID和笔记本ID获取笔记列表
        /// </summary>
        public async Task<List<Note>> GetByUserIdAndNotebookIdAsync(Guid userId, Guid notebookId, CancellationToken cancellationToken = default)
        {
            try
            {
                var notes = await _repository.GetByUserIdAndNotebookIdAsync(userId, notebookId, cancellationToken);
                return notes.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据用户ID和笔记本ID获取笔记列表时发生错误: {UserId}, {NotebookId}", userId, notebookId);
                throw;
            }
        }

        /// <summary>
        /// 根据标签ID获取笔记列表
        /// </summary>
        public async Task<List<Note>> GetByTagIdAsync(Guid tagId, CancellationToken cancellationToken = default)
        {
            try
            {
                var notes = await _repository.GetByTagIdAsync(tagId, cancellationToken);
                return notes.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据标签ID获取笔记列表时发生错误: {TagId}", tagId);
                throw;
            }
        }
    }
}