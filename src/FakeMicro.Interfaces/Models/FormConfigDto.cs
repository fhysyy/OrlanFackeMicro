// 表单配置数据传输对象

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Orleans.Serialization;
using Orleans;
using FakeMicro.Entities.Enums;

namespace FakeMicro.Interfaces.Models
{
    /// <summary>
    /// 表单配置数据传输对象
    /// </summary>
    [GenerateSerializer]
    public class FormConfigDto
    {
        /// <summary>
        /// 表单配置ID
        /// </summary>
        [Id(0)]
        public long Id { get; set; }

        /// <summary>
        /// 表单编码
        /// </summary>
        [Id(1)]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 表单名称
        /// </summary>
        [Id(2)]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 表单描述
        /// </summary>
        [Id(3)]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 表单布局配置
        /// </summary>
        [Id(4)]
        public string LayoutConfig { get; set; } = string.Empty;

        /// <summary>
        /// 表单状态
        /// </summary>
        [Id(5)]
        public FormConfigStatus Status { get; set; } = FormConfigStatus.Draft;

        /// <summary>
        /// 版本号
        /// </summary>
        [Id(6)]
        public int Version { get; set; } = 1;

        /// <summary>
        /// 是否为最新版本
        /// </summary>
        [Id(7)]
        public bool IsLatest { get; set; } = true;

        /// <summary>
        /// 是否启用
        /// </summary>
        [Id(8)]
        public bool IsEnabled { get; set; } = false;

        /// <summary>
        /// 创建时间
        /// </summary>
        [Id(9)]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        [Id(10)]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        [Id(11)]
        public string CreatedBy { get; set; }

        /// <summary>
        /// 更新人
        /// </summary>
        [Id(12)]
        public string UpdatedBy { get; set; }
    }

    /// <summary>
    /// 创建表单配置DTO
    /// </summary>
    [GenerateSerializer]
    public class FormConfigCreateDto
    {
        /// <summary>
        /// 表单编码
        /// </summary>
        [Id(0)]
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 表单名称
        /// </summary>
        [Id(1)]
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 表单描述
        /// </summary>
        [Id(2)]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 表单布局配置
        /// </summary>
        [Id(3)]
        public string LayoutConfig { get; set; } = string.Empty;

        /// <summary>
        /// 表单状态
        /// </summary>
        [Id(4)]
        public FormConfigStatus Status { get; set; } = FormConfigStatus.Draft;

        /// <summary>
        /// 验证数据
        /// </summary>
        /// <returns>验证结果和错误信息</returns>
        public (bool IsValid, List<string> Errors) Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Code))
                errors.Add("表单编码不能为空");
            if (Code?.Length > 50)
                errors.Add("表单编码长度不能超过50个字符");

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("表单名称不能为空");
            if (Name?.Length > 200)
                errors.Add("表单名称长度不能超过200个字符");

            if (Description?.Length > 500)
                errors.Add("表单描述长度不能超过500个字符");

            return (errors.Count == 0, errors);
        }
    }

    /// <summary>
    /// 更新表单配置DTO
    /// </summary>
    [GenerateSerializer]
    public class FormConfigUpdateDto
    {
        /// <summary>
        /// 表单名称
        /// </summary>
        [Id(0)]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 表单描述
        /// </summary>
        [Id(1)]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 表单布局配置
        /// </summary>
        [Id(2)]
        public string LayoutConfig { get; set; } = string.Empty;

        /// <summary>
        /// 表单状态
        /// </summary>
        [Id(3)]
        public FormConfigStatus Status { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [Id(4)]
        public bool IsEnabled { get; set; }

        [Id(5)]
        public string Code { get; set; }

        /// <summary>
        /// 验证数据
        /// </summary>
        /// <returns>验证结果和错误信息</returns>
        public (bool IsValid, List<string> Errors) Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("表单名称不能为空");
            if (Name?.Length > 200)
                errors.Add("表单名称长度不能超过200个字符");

            if (Description?.Length > 500)
                errors.Add("表单描述长度不能超过500个字符");

            return (errors.Count == 0, errors);
        }
    }

    /// <summary>
    /// 表单配置分页查询参数
    /// </summary>
    [GenerateSerializer]
    public class FormConfigQueryDto
    {
        /// <summary>
        /// 表单编码
        /// </summary>
        [Id(0)]
        public string? Code { get; set; }

        /// <summary>
        /// 表单名称
        /// </summary>
        [Id(1)]
        public string? Name { get; set; }

        /// <summary>
        /// 表单状态
        /// </summary>
        [Id(2)]
        public FormConfigStatus? Status { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [Id(3)]
        public bool? IsEnabled { get; set; }

        /// <summary>
        /// 页码
        /// </summary>
        [Id(4)]
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 每页大小
        /// </summary>
        [Id(5)]
        public int PageSize { get; set; } = 10;
    }
}