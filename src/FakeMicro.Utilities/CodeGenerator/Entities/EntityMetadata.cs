using System.Collections.Generic;

namespace FakeMicro.Utilities.CodeGenerator.Entities
{
    /// <summary>
    /// 实体元数据类，用于代码生成
    /// </summary>
    public class EntityMetadata
    {
        /// <summary>
        /// 实体名称
        /// </summary>
        public string EntityName { get; set; } = string.Empty;

        /// <summary>
        /// 实体描述
        /// </summary>
        public string EntityDescription { get; set; } = string.Empty;

        /// <summary>
        /// 命名空间
        /// </summary>
        public string Namespace { get; set; } = string.Empty;

        /// <summary>
        /// 主键属性名称
        /// </summary>
        public string PrimaryKeyProperty { get; set; } = "Id";

        /// <summary>
        /// 主键类型
        /// </summary>
        public string PrimaryKeyType { get; set; } = "int";

        /// <summary>
        /// 主键属性元数据
        /// </summary>
        public PropertyMetadata? PrimaryKeyPropertyMetadata { get; set; }

        /// <summary>
        /// 属性列表
        /// </summary>
        public List<PropertyMetadata>? Properties { get; set; }

        /// <summary>
        /// 表名
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// 是否可审计
        /// </summary>
        public bool IsAuditable { get; set; } = false;

        /// <summary>
        /// 是否支持软删除
        /// </summary>
        public bool IsSoftDeletable { get; set; } = false;

        /// <summary>
        /// 是否支持多租户
        /// </summary>
        public bool SupportMultiTenant { get; set; } = false;
    }
}