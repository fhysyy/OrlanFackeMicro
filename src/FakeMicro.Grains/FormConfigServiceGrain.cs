using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using AutoMapper;
using FakeMicro.Entities;
using FakeMicro.Entities.Enums;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
using FakeMicro.DatabaseAccess.Interfaces;

namespace FakeMicro.Grains
{
    /// <summary>
    /// 表单配置管理服务Grain实现
    /// 用于处理查询操作
    /// </summary>
    public class FormConfigServiceGrain : OrleansGrainBase, IFormConfigService
    {
        private readonly IRepository<FormConfig, string> _repository;
        private readonly IMapper _mapper;
        private new readonly ILogger<FormConfigServiceGrain> _logger;

        public FormConfigServiceGrain(
            IRepository<FormConfig, string> repository,
            IMapper mapper,
            ILogger<FormConfigServiceGrain> logger,
            IGrainContext? grainContext = null)
            : base(logger, grainContext)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 获取表单配置列表（分页）
        /// </summary>
        public async Task<PaginatedResult<FormConfigDto>> GetFormConfigsAsync(FormConfigQueryDto query, CancellationToken cancellationToken = default)
        {
            return await TrackPerformanceAsync("GetFormConfigs", async () =>
            {
                try
                {
                    // 验证查询参数
                    query ??= new FormConfigQueryDto();
                    int page = Math.Max(1, query.PageIndex);
                    int pageSize = Math.Max(1, Math.Min(100, query.PageSize));

                    // 构建查询条件
                    Expression<Func<FormConfig, bool>> predicate = x => !x.is_deleted;

                    // 应用筛选条件
                    if (!string.IsNullOrEmpty(query.Code))
                    {
                        predicate = x => !x.is_deleted && x.code.Contains(query.Code, StringComparison.OrdinalIgnoreCase);
                    }

                    if (!string.IsNullOrEmpty(query.Name))
                    {
                        predicate = x => !x.is_deleted && x.name.Contains(query.Name, StringComparison.OrdinalIgnoreCase);
                    }

                    if (query.Status.HasValue)
                    {
                        predicate = x => !x.is_deleted && x.status == query.Status.Value;
                    }

                    if (query.IsEnabled.HasValue)
                    {
                        predicate = x => !x.is_deleted && x.is_enabled == query.IsEnabled.Value;
                    }

                    // 执行分页查询
                    var pagedResult = await _repository.GetPagedByConditionAsync(
                        predicate,
                        page,
                        pageSize,
                        x => x.name, // 按名称排序
                        false, // 升序
                        cancellationToken
                    );

                    // 映射到DTO
                    var dtos = _mapper.Map<List<FormConfigDto>>(pagedResult.Items);

                    _logger.LogInformation("获取表单配置列表成功，页码: {Page}, 每页大小: {PageSize}, 总条数: {TotalCount}",
                        page, pageSize, pagedResult.TotalCount);

                    return new PaginatedResult<FormConfigDto>(dtos, page, pageSize, pagedResult.TotalCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "获取表单配置列表失败");
                    throw;
                }
            });
        }

        /// <summary>
        /// 获取所有表单配置列表（用于下拉选择等场景）
        /// </summary>
        public async Task<List<FormConfigDto>> GetAllFormConfigsAsync(FormConfigStatus? status = null, CancellationToken cancellationToken = default)
        {
            return await TrackPerformanceAsync("GetAllFormConfigs", async () =>
            {
                try
                {
                    // 构建查询条件
                    Expression<Func<FormConfig, bool>> predicate = x => !x.is_deleted;
                    if (status.HasValue)
                    {
                        predicate = x => !x.is_deleted && x.status == status.Value;
                    }

                    // 获取所有符合条件的实体
                    var items = await _repository.GetByConditionAsync(predicate, cancellationToken);

                    // 按名称排序
                    var sortedItems = items.OrderBy(x => x.name).ToList();

                    _logger.LogInformation("获取所有表单配置列表成功，状态筛选: {Status}, 条数: {Count}",
                        status?.ToString() ?? "全部", sortedItems.Count);

                    return _mapper.Map<List<FormConfigDto>>(sortedItems);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "获取所有表单配置列表失败，状态筛选: {Status}", status?.ToString() ?? "全部");
                    throw;
                }
            });
        }

        /// <summary>
        /// 根据编码获取表单配置
        /// </summary>
        public async Task<FormConfigDto?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            return await TrackPerformanceAsync("GetFormConfigByCode", async () =>
            {
                try
                {
                    if (string.IsNullOrEmpty(code))
                    {
                        _logger.LogWarning("表单编码不能为空");
                        return null;
                    }

                    // 使用查询条件获取实体
                    var items = await _repository.GetByConditionAsync(
                        x => x.code == code && !x.is_deleted,
                        cancellationToken
                    );

                    var entity = items.FirstOrDefault();

                    if (entity == null)
                    {
                        _logger.LogWarning("未找到指定编码的表单配置: {Code}", code);
                        return null;
                    }

                    _logger.LogInformation("根据编码获取表单配置成功: {Code}, {Id}", code, entity.id);
                    return _mapper.Map<FormConfigDto>(entity);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "根据编码获取表单配置失败: {Code}", code);
                    throw;
                }
            });
        }
    }
}