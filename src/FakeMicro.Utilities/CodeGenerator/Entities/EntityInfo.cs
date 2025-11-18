using System.Collections.Generic;

namespace FakeMicro.Utilities.CodeGenerator.Entities
{
    /// <summary>
    /// 实体信息类，用于代码生成
    /// </summary>
    public class EntityInfo
    {
        /// <summary>
        /// 实体名称
        /// </summary>
        public string EntityName { get; set; } = string.Empty;

        /// <summary>
        /// 实体命名空间
        /// </summary>
        public string Namespace { get; set; } = string.Empty;

        /// <summary>
        /// 主键属性名称
        /// </summary>
        public string PrimaryKeyName { get; set; } = "Id";

        /// <summary>
        /// 主键类型
        /// </summary>
        public string PrimaryKeyType { get; set; } = "int";

        /// <summary>
        /// 实体属性列表
        /// </summary>
        public List<PropertyInfo> Properties { get; set; } = new List<PropertyInfo>();

        /// <summary>
        /// 实体描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 是否支持软删除
        /// </summary>
        public bool SupportSoftDelete { get; set; } = true;

        /// <summary>
        /// 是否支持多租户
        /// </summary>
        public bool SupportMultiTenant { get; set; } = false;
    }

    /// <summary>
    /// 属性信息类
    /// </summary>
    public class PropertyInfo
    {
        /// <summary>
        /// 属性名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 属性类型
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 是否可为空
        /// </summary>
        public bool IsNullable { get; set; } = false;

        /// <summary>
        /// 是否为主键
        /// </summary>
        public bool IsPrimaryKey { get; set; } = false;

        /// <summary>
        /// 是否为外键
        /// </summary>
        public bool IsForeignKey { get; set; } = false;

        /// <summary>
        /// 外键实体名称
        /// </summary>
        public string? ForeignEntityName { get; set; }

        /// <summary>
        /// 外键属性名称
        /// </summary>
        public string? ForeignKeyName { get; set; }

        /// <summary>
        /// 属性描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 最大长度（用于字符串类型）
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// 默认值
        /// </summary>
        public string? DefaultValue { get; set; }
    }
}