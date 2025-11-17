using System.Collections.Generic;

namespace FakeMicro.Utilities.CodeGenerator
{
    /// <summary>
    /// 实体元数据
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
        public string Namespace { get; set; } = "FakeMicro.Entities";

        /// <summary>
        /// 表名
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// 主键属性名
        /// </summary>
        public string PrimaryKeyProperty { get; set; } = "id";

        /// <summary>
        /// 主键类型
        /// </summary>
        public string PrimaryKeyType { get; set; } = "long";

        /// <summary>
        /// 属性列表
        /// </summary>
        public List<PropertyMetadata> Properties { get; set; } = new();

        /// <summary>
        /// 是否实现了审计接口
        /// </summary>
        public bool IsAuditable { get; set; }

        /// <summary>
        /// 是否实现了软删除接口
        /// </summary>
        public bool IsSoftDeletable { get; set; }

        /// <summary>
        /// 获取主键属性的元数据
        /// </summary>
        public PropertyMetadata? PrimaryKeyPropertyMetadata => 
            Properties.Find(p => p.IsPrimaryKey);

        /// <summary>
        /// 获取非主键属性
        /// </summary>
        public List<PropertyMetadata> NonPrimaryKeyProperties => 
            Properties.FindAll(p => !p.IsPrimaryKey);
    }

    /// <summary>
    /// 属性元数据
    /// </summary>
    public class PropertyMetadata
    {
        /// <summary>
        /// 属性名
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 属性类型
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 属性描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 是否必需
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 是否为主键
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// 最大长度
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// 获取驼峰命名的属性名
        /// </summary>
        public string CamelCaseName
        {
            get
            {
                if (string.IsNullOrEmpty(Name)) return Name;
                
                return char.ToLower(Name[0]) + Name.Substring(1);
            }
        }

        /// <summary>
        /// 获取Pascal命名的属性名
        /// </summary>
        public string PascalCaseName
        {
            get
            {
                if (string.IsNullOrEmpty(Name)) return Name;
                
                return char.ToUpper(Name[0]) + Name.Substring(1);
            }
        }
    }

    /// <summary>
    /// 代码生成器选项
    /// </summary>
    public class CodeGeneratorOptions
    {
        /// <summary>
        /// 输出路径（项目根目录）
        /// </summary>
        public string OutputPath { get; set; } = "f:/Orleans/OrlanFackeMicro/src";

        /// <summary>
        /// 要生成的模板类型
        /// </summary>
        public List<string> TemplatesToGenerate { get; set; } = new()
        {
            "Interface",
            "Grain", 
            "ServiceInterface",
            "ServiceGrain",
            "Dto"
        };

        /// <summary>
        /// 是否覆盖现有文件
        /// </summary>
        public bool OverwriteExisting { get; set; } = true;

        /// <summary>
        /// 作者信息
        /// </summary>
        public string Author { get; set; } = "CodeGenerator";

        /// <summary>
        /// 公司名称
        /// </summary>
        public string Company { get; set; } = "FakeMicro";

        /// <summary>
        /// 是否添加文件头注释
        /// </summary>
        public bool AddFileHeader { get; set; } = true;

        /// <summary>
        /// 命名空间前缀
        /// </summary>
        public string NamespacePrefix { get; set; } = "FakeMicro";

        /// <summary>
        /// 是否生成异步方法
        /// </summary>
        public bool GenerateAsyncMethods { get; set; } = true;

        /// <summary>
        /// 是否生成缓存支持
        /// </summary>
        public bool GenerateCacheSupport { get; set; } = true;

        /// <summary>
        /// 是否生成事务支持
        /// </summary>
        public bool GenerateTransactionSupport { get; set; } = true;

        /// <summary>
        /// 是否生成权限验证
        /// </summary>
        public bool GenerateAuthorization { get; set; } = true;

        /// <summary>
        /// 是否生成日志记录
        /// </summary>
        public bool GenerateLogging { get; set; } = true;

        /// <summary>
        /// 是否生成单元测试
        /// </summary>
        public bool GenerateUnitTests { get; set; } = false;

        /// <summary>
        /// 是否生成API文档注释
        /// </summary>
        public bool GenerateXmlDocumentation { get; set; } = true;
    }
}