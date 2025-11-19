namespace FakeMicro.Utilities.CodeGenerator.Entities
{
    /// <summary>
    /// 属性元数据类，用于代码生成
    /// </summary>
    public class PropertyMetadata
    {
        /// <summary>
        /// 属性名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 数据库列名（如果不指定则使用属性名）
        /// </summary>
        public string? ColumnName { get; set; }

        /// <summary>
        /// 属性类型
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 属性描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 是否可为空
        /// </summary>
        public bool IsNullable { get; set; } = false;

        /// <summary>
        /// 是否为主键
        /// </summary>
        public bool IsPrimaryKey { get; set; } = false;

        /// <summary>
        /// 是否为自增标识
        /// </summary>
        public bool IsIdentity { get; set; } = false;

        /// <summary>
        /// 是否为只读属性
        /// </summary>
        public bool IsReadOnly { get; set; } = false;

        /// <summary>
        /// 是否为外键
        /// </summary>
        public bool IsForeignKey { get; set; } = false;

        /// <summary>
        /// 是否为导航属性
        /// </summary>
        public bool IsNavigationProperty { get; set; } = false;

        /// <summary>
        /// 关联实体名称（如果是导航属性）
        /// </summary>
        public string? RelatedEntityName { get; set; }

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// 默认值
        /// </summary>
        public string? DefaultValue { get; set; }

        /// <summary>
        /// 最大长度（用于字符串类型）
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// 最小长度（用于字符串类型）
        /// </summary>
        public int? MinLength { get; set; }
    }
}