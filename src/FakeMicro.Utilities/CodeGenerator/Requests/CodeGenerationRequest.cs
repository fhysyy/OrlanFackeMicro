using System.ComponentModel.DataAnnotations;

namespace FakeMicro.Utilities.CodeGenerator.Requests
{
    /// <summary>
    /// 代码生成请求
    /// </summary>
    public class CodeGenerationRequest
    {
        /// <summary>
        /// 实体名称
        /// </summary>
        [Required]
        public string EntityName { get; set; } = string.Empty;

        /// <summary>
        /// 实体描述
        /// </summary>
        public string? EntityDescription { get; set; }

        /// <summary>
        /// 生成类型
        /// </summary>
        [Required]
        public GenerationType GenerationType { get; set; }

        /// <summary>
        /// 输出路径
        /// </summary>
        public string? OutputPath { get; set; }

        /// <summary>
        /// 覆盖策略
        /// </summary>
        public OverwriteStrategy OverwriteStrategy { get; set; } = OverwriteStrategy.Overwrite;

        /// <summary>
        /// 属性列表
        /// </summary>
        public List<PropertyRequest>? Properties { get; set; }
    }

    /// <summary>
    /// 属性请求
    /// </summary>
    public class PropertyRequest
    {
        /// <summary>
        /// 属性名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 属性类型
        /// </summary>
        public string Type { get; set; } = "string";

        /// <summary>
        /// 是否可为空
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// 是否为主键
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// 最大长度
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// 是否必需
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// 批量代码生成请求
    /// </summary>
    public class BatchCodeGenerationRequest
    {
        /// <summary>
        /// 实体生成请求列表
        /// </summary>
        [Required]
        public List<CodeGenerationRequest> Entities { get; set; } = new List<CodeGenerationRequest>();
    }
}