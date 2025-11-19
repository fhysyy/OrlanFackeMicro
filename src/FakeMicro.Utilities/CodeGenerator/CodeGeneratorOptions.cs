using System.Collections.Generic;

namespace FakeMicro.Utilities.CodeGenerator
{
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
        /// 默认命名空间前缀
        /// </summary>
        public string NamespacePrefix { get; set; } = "FakeMicro";

        /// <summary>
        /// 默认命名空间
        /// </summary>
        public string DefaultNamespace { get; set; } = "FakeMicro.Entities";

        /// <summary>
        /// 是否添加文件头注释
        /// </summary>
        public bool AddFileHeader { get; set; } = true;

        /// <summary>
        /// 是否生成单元测试
        /// </summary>
        public bool GenerateUnitTests { get; set; } = false;

        /// <summary>
        /// 是否生成API文档
        /// </summary>
        public bool GenerateApiDocumentation { get; set; } = true;

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
        /// 是否生成授权功能
        /// </summary>
        public bool GenerateAuthorization { get; set; } = true;

        /// <summary>
        /// 是否生成日志记录
        /// </summary>
        public bool GenerateLogging { get; set; } = true;

        /// <summary>
        /// 是否生成XML文档
        /// </summary>
        public bool GenerateXmlDocumentation { get; set; } = true;
    }
}