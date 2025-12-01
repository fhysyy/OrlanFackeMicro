using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using AutoMapper;
using FakeMicro.Entities;
using FakeMicro.Entities.Enums;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
using FakeMicro.Interfaces.Models.Results;
using FakeMicro.DatabaseAccess.Interfaces;

namespace FakeMicro.Grains
{
    /// <summary>
    /// 表单配置管理Grain实现
    /// </summary>
    public class FormConfigGrain : OrleansGrainBase, IFormConfigGrain
    {
        private readonly IRepository<FormConfig, string> _repository;
        private readonly IMapper _mapper;
        private new readonly ILogger<FormConfigGrain> _logger;

        public FormConfigGrain(
            IRepository<FormConfig, string> repository,
            IMapper mapper,
            ILogger<FormConfigGrain> logger,
            IGrainContext? grainContext = null)
            : base(logger, grainContext)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 获取表单配置详情
        /// </summary>
        public async Task<FormConfigDto?> GetAsync(CancellationToken cancellationToken = default)
        {
            return await TrackPerformanceAsync("GetFormConfig", async () =>
            {
                try
                {
                    var id = this.GetPrimaryKeyString();
                    var entity = await _repository.GetByIdAsync(id, cancellationToken);
                    
                    if (entity == null || entity.is_deleted)
                    {
                        _logger.LogWarning("表单配置不存在或已删除: {Id}", id);
                        return null;
                    }
                    
                    return _mapper.Map<FormConfigDto>(entity);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "获取表单配置详情失败: {Id}", this.GetPrimaryKeyString());
                    throw;
                }
            });
        }

        /// <summary>
        /// 创建表单配置
        /// </summary>
        public async Task<FormConfigDto> CreateAsync(FormConfigCreateDto request, CancellationToken cancellationToken = default)
        {
            return await TrackPerformanceAsync("CreateFormConfig", async () =>
            {
                try
                {
                    // 验证请求
                    if (request == null)
                    {
                        throw new ArgumentNullException(nameof(request), "请求参数不能为空");
                    }

                    // 检查编码是否已存在
                    if (await CodeExistsAsync(request.Code, cancellationToken: cancellationToken))
                    {
                        throw new ArgumentException($"表单编码 '{request.Code}' 已存在");
                    }

                    // 映射到实体
                    var entity = _mapper.Map<FormConfig>(request);
                    entity.created_at = DateTime.UtcNow;
                    entity.updated_at = DateTime.UtcNow;
                    entity.is_deleted = false;

                    // 保存到数据库
                    await _repository.AddAsync(entity, cancellationToken);
                    await _repository.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("创建表单配置成功: {Id}, {Code}", entity.id, entity.code);
                    return _mapper.Map<FormConfigDto>(entity);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "创建表单配置失败: {Code}", request?.Code);
                    throw;
                }
            });
        }

        /// <summary>
        /// 更新表单配置
        /// </summary>
        public async Task<FormConfigDto> UpdateAsync(FormConfigUpdateDto request, CancellationToken cancellationToken = default)
        {
            return await TrackPerformanceAsync("UpdateFormConfig", async () =>
            {
                try
                {
                    var id = this.GetPrimaryKeyString();
                    
                    // 获取现有实体
                    var entity = await _repository.GetByIdAsync(id, cancellationToken);
                    if (entity == null || entity.is_deleted)
                    {
                        throw new ArgumentException("表单配置不存在或已删除");
                    }

                    // 检查编码是否重复（排除当前记录）
                    if (!string.IsNullOrEmpty(request.Code) && request.Code != entity.code)
                    {
                        if (await CodeExistsAsync(request.Code, id, cancellationToken))
                        {
                            throw new ArgumentException($"表单编码 '{request.Code}' 已存在");
                        }
                    }

                    // 更新字段
                    _mapper.Map(request, entity);
                    entity.updated_at = DateTime.UtcNow;

                    // 保存更新
                    await _repository.UpdateAsync(entity, cancellationToken);
                    await _repository.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("更新表单配置成功: {Id}, {Code}", entity.id, entity.code);
                    return _mapper.Map<FormConfigDto>(entity);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "更新表单配置失败: {Id}", this.GetPrimaryKeyString());
                    throw;
                }
            });
        }

        /// <summary>
        /// 删除表单配置（软删除）
        /// </summary>
        public async Task<bool> DeleteAsync(CancellationToken cancellationToken = default)
        {
            return await TrackPerformanceAsync("DeleteFormConfig", async () =>
            {
                try
                {
                    var id = this.GetPrimaryKeyString();
                    
                    // 获取实体
                    var entity = await _repository.GetByIdAsync(id, cancellationToken);
                    if (entity == null || entity.is_deleted)
                    {
                        return true; // 已删除或不存在视为成功
                    }

                    // 软删除
                    entity.is_deleted = true;
                    entity.deleted_at = DateTime.UtcNow;
                    entity.updated_at = DateTime.UtcNow;

                    await _repository.UpdateAsync(entity, cancellationToken);
                    await _repository.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("删除表单配置成功: {Id}, {Code}", entity.id, entity.code);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "删除表单配置失败: {Id}", this.GetPrimaryKeyString());
                    throw;
                }
            });
        }

        /// <summary>
        /// 更新表单配置状态
        /// </summary>
        public async Task<FormConfigDto> UpdateStatusAsync(FormConfigStatus status, CancellationToken cancellationToken = default)
        {
            return await TrackPerformanceAsync("UpdateFormConfigStatus", async () =>
            {
                try
                {
                    var id = this.GetPrimaryKeyString();
                    
                    // 获取实体
                    var entity = await _repository.GetByIdAsync(id, cancellationToken);
                    if (entity == null || entity.is_deleted)
                    {
                        throw new ArgumentException("表单配置不存在或已删除");
                    }

                    // 更新状态
                    entity.status = status;
                    entity.updated_at = DateTime.UtcNow;

                    await _repository.UpdateAsync(entity, cancellationToken);
                    await _repository.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("更新表单配置状态成功: {Id}, 状态: {Status}", entity.id, status);
                    return _mapper.Map<FormConfigDto>(entity);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "更新表单配置状态失败: {Id}, 状态: {Status}", 
                        this.GetPrimaryKeyString(), status);
                    throw;
                }
            });
        }

        /// <summary>
        /// 检查表单编码是否已存在
        /// </summary>
        public async Task<bool> CodeExistsAsync(string code, string excludeId = "", CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(code))
            {
                return false;
            }

            try
            {
                var entities = await _repository.GetByConditionAsync(x => x.code == code && !x.is_deleted, cancellationToken);
                var entity = entities.FirstOrDefault();
                if (entity == null)
                {
                    return false;
                }

                if (string.IsNullOrEmpty(excludeId))
                {
                    return true;
                }

                // 尝试将 excludeId 转换为 long 类型
                if (long.TryParse(excludeId, out long excludeLongId))
                {
                    return entity.id != excludeLongId;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查表单编码是否存在失败: {Code}", code);
                throw;
            }
        }
    }
}