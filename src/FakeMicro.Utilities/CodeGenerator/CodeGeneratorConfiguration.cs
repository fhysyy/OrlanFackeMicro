using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace FakeMicro.Utilities.CodeGenerator
{
    /// <summary>
    /// 代码生成器配置文件格式
    /// </summary>
    public class CodeGeneratorConfiguration
    {
        /// <summary>
        /// 基础配置
        /// </summary>
        public BaseConfiguration Base { get; set; } = new();

        /// <summary>
        /// 模板配置
        /// </summary>
        public TemplateConfiguration Templates { get; set; } = new();

        /// <summary>
        /// 输出路径配置
        /// </summary>
        public OutputConfiguration Output { get; set; } = new();

        /// <summary>
        /// 代码风格配置
        /// </summary>
        public CodeStyleConfiguration CodeStyle { get; set; } = new();

        /// <summary>
        /// 自定义模板配置
        /// </summary>
        public Dictionary<string, CustomTemplateConfiguration> CustomTemplates { get; set; } = new();
    }

    /// <summary>
    /// 基础配置
    /// </summary>
    public class BaseConfiguration
    {
        /// <summary>
        /// 作者信息
        /// </summary>
        public string Author { get; set; } = "CodeGenerator";

        /// <summary>
        /// 公司名称
        /// </summary>
        public string Company { get; set; } = "FakeMicro";

        /// <summary>
        /// 项目根目录
        /// </summary>
        public string ProjectRoot { get; set; } = "f:/Orleans/OrlanFackeMicro/src";

        /// <summary>
        /// 默认命名空间前缀
        /// </summary>
        public string NamespacePrefix { get; set; } = "FakeMicro";

        /// <summary>
        /// 默认命名空间
        /// </summary>
        public string DefaultNamespace { get; set; } = "FakeMicro.Entities";

        /// <summary>
        /// 是否覆盖现有文件
        /// </summary>
        public bool OverwriteExisting { get; set; } = true;

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

        public string OutputPath { get; set; } = "f:/Orleans/OrlanFackeMicro/src";
    }

    /// <summary>
    /// 模板配置
    /// </summary>
    public class TemplateConfiguration
    {
        /// <summary>
        /// 默认生成的模板列表
        /// </summary>
        public List<string> DefaultTemplates { get; set; } = new()
        {
            "Interface",
            "Result",
            "Request",
            "Grain",
            "ServiceInterface", 
            "ServiceGrain",
            "Dto"
        };

        /// <summary>
        /// 可用模板列表
        /// </summary>
        public List<string> AvailableTemplates { get; set; } = new()
        {
            "Interface",
            "Result",
            "Request",
            "Grain",
            "ServiceInterface",
            "ServiceGrain", 
            "Dto",
            "Controller",
            "UnitTest"
        };

        /// <summary>
        /// 模板自定义配置
        /// </summary>
        public Dictionary<string, TemplateSettings> TemplateSettings { get; set; } = new()
        {
            ["Interface"] = new TemplateSettings
            {
                Enabled = true,
                FileNamePattern = "I{EntityName}Grain.cs",
                OutputDirectory = "FakeMicro.Interfaces"
            },
            ["Result"] = new TemplateSettings
            {
                Enabled = true,
                FileNamePattern = "{EntityName}Results.cs",
                OutputDirectory = "FakeMicro.Interfaces/Models/Results"
            },
            ["Request"] = new TemplateSettings
            {
                Enabled = true,
                FileNamePattern = "{EntityName}Requests.cs",
                OutputDirectory = "FakeMicro.Interfaces/Models/Requests"
            },
            ["Grain"] = new TemplateSettings
            {
                Enabled = true,
                FileNamePattern = "{EntityName}Grain.cs",
                OutputDirectory = "FakeMicro.Grains"
            },
            ["Dto"] = new TemplateSettings
            {
                Enabled = true,
                FileNamePattern = "{EntityName}Dto.cs",
                OutputDirectory = "FakeMicro.Interfaces/Models"
            },
            ["Controller"] = new TemplateSettings
            {
                Enabled = false,
                FileNamePattern = "{EntityName}Controller.cs",
                OutputDirectory = "FakeMicro.Api/Controllers"
            }
        };
    }

    /// <summary>
    /// 单个模板设置
    /// </summary>
    public class TemplateSettings
    {
        /// <summary>
        /// 是否启用该模板
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 输出文件名模式
        /// </summary>
        public string FileNamePattern { get; set; } = string.Empty;

        /// <summary>
        /// 输出目录
        /// </summary>
        public string OutputDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 自定义模板路径
        /// </summary>
        public string? CustomTemplatePath { get; set; }

        /// <summary>
        /// 模板参数
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// 输出配置
    /// </summary>
    public class OutputConfiguration
    {
        /// <summary>
        /// 接口文件输出目录
        /// </summary>
        public string InterfacesDirectory { get; set; } = "FakeMicro.Interfaces";

        /// <summary>
        /// Grain实现文件输出目录
        /// </summary>
        public string GrainsDirectory { get; set; } = "FakeMicro.Grains";

        /// <summary>
        /// API控制器输出目录
        /// </summary>
        public string ApiDirectory { get; set; } = "FakeMicro.Api";

        /// <summary>
        /// DTO文件输出目录
        /// </summary>
        public string DtoDirectory { get; set; } = "FakeMicro.Interfaces/Models";

        /// <summary>
        /// 单元测试输出目录
        /// </summary>
        public string TestsDirectory { get; set; } = "FakeMicro.Tests";

        /// <summary>
        /// 文档输出目录
        /// </summary>
        public string DocumentationDirectory { get; set; } = "docs/generated";
    }

    /// <summary>
    /// 代码风格配置
    /// </summary>
    public class CodeStyleConfiguration
    {
        /// <summary>
        /// 命名约定
        /// </summary>
        public NamingConvention Naming { get; set; } = new();

        /// <summary>
        /// 格式化配置
        /// </summary>
        public FormattingConfiguration Formatting { get; set; } = new();

        /// <summary>
        /// 注释配置
        /// </summary>
        public CommentConfiguration Comments { get; set; } = new();
    }

    /// <summary>
    /// 命名约定
    /// </summary>
    public class NamingConvention
    {
        /// <summary>
        /// 使用匈牙利命名法
        /// </summary>
        public bool UseHungarianNotation { get; set; } = false;

        /// <summary>
        /// 私有字段前缀
        /// </summary>
        public string PrivateFieldPrefix { get; set; } = "_";

        /// <summary>
        /// 只读字段前缀
        /// </summary>
        public string ReadOnlyFieldPrefix { get; set; } = "_";

        /// <summary>
        /// 常量命名风格
        /// </summary>
        public string ConstantStyle { get; set; } = "PascalCase"; // PascalCase, UPPER_CASE

        /// <summary>
        /// 接口前缀
        /// </summary>
        public string InterfacePrefix { get; set; } = "I";

        /// <summary>
        /// 实体后缀
        /// </summary>
        public string EntitySuffix { get; set; } = "";

        /// <summary>
        /// DTO后缀
        /// </summary>
        public string DtoSuffix { get; set; } = "Dto";

        /// <summary>
        /// Grain后缀
        /// </summary>
        public string GrainSuffix { get; set; } = "Grain";
    }

    /// <summary>
    /// 格式化配置
    /// </summary>
    public class FormattingConfiguration
    {
        /// <summary>
        /// 缩进风格
        /// </summary>
        public string IndentStyle { get; set; } = "Spaces"; // Spaces, Tabs

        /// <summary>
        /// 缩进大小
        /// </summary>
        public int IndentSize { get; set; } = 4;

        /// <summary>
        /// 行尾风格
        /// </summary>
        public string LineEnding { get; set; } = "CRLF"; // CRLF, LF

        /// <summary>
        /// 最大行长度
        /// </summary>
        public int MaxLineLength { get; set; } = 120;

        /// <summary>
        /// 是否整理using语句
        /// </summary>
        public bool SortUsingStatements { get; set; } = true;

        /// <summary>
        /// 是否移除未使用的using语句
        /// </summary>
        public bool RemoveUnusedUsings { get; set; } = true;

        /// <summary>
        /// 大括号样式
        /// </summary>
        public string BraceStyle { get; set; } = "Allman"; // Allman, K&R, 1TBS
    }

    /// <summary>
    /// 注释配置
    /// </summary>
    public class CommentConfiguration
    {
        /// <summary>
        /// 是否生成XML文档注释
        /// </summary>
        public bool GenerateXmlDocumentation { get; set; } = true;

        /// <summary>
        /// 是否生成方法参数注释
        /// </summary>
        public bool GenerateParameterComments { get; set; } = true;

        /// <summary>
        /// 是否生成返回值注释
        /// </summary>
        public bool GenerateReturnComments { get; set; } = true;

        /// <summary>
        /// 是否生成异常注释
        /// </summary>
        public bool GenerateExceptionComments { get; set; } = false;

        /// <summary>
        /// 是否生成验证逻辑
        /// </summary>
        public bool GenerateValidationLogic { get; set; } = true;

        /// <summary>
        /// 是否生成错误处理逻辑
        /// </summary>
        public bool GenerateErrorHandling { get; set; } = true;

        /// <summary>
        /// 是否生成增强日志记录
        /// </summary>
        public bool GenerateEnhancedLogging { get; set; } = true;

        /// <summary>
        /// 注释语言
        /// </summary>
        public string CommentLanguage { get; set; } = "zh-CN"; // zh-CN, en-US

        /// <summary>
        /// 自定义注释模板
        /// </summary>
        public Dictionary<string, string> CommentTemplates { get; set; } = new();
    }

    /// <summary>
    /// 自定义模板配置
    /// </summary>
    public class CustomTemplateConfiguration
    {
        /// <summary>
        /// 模板名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 模板描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 模板内容
        /// </summary>
        public string Template { get; set; } = string.Empty;

        /// <summary>
        /// 模板类型（文件/内容）
        /// </summary>
        public string TemplateType { get; set; } = "Content"; // File, Content

        /// <summary>
        /// 输出文件名模式
        /// </summary>
        public string FileNamePattern { get; set; } = string.Empty;

        /// <summary>
        /// 输出目录
        /// </summary>
        public string OutputDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 模板参数
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// 配置管理器
    /// </summary>
    public static class ConfigurationManager
    {
        /// <summary>
        /// 从配置文件加载配置
        /// </summary>
        /// <param name="configuration">配置对象</param>
        /// <param name="sectionName">配置节名称</param>
        /// <returns>代码生成器配置</returns>
        public static CodeGeneratorConfiguration LoadFromConfiguration(
            IConfiguration configuration, 
            string sectionName = "CodeGenerator")
        {
            var config = new CodeGeneratorConfiguration();
            configuration.GetSection(sectionName).Bind(config);
            return config;
        }

        /// <summary>
        /// 从JSON文件加载配置
        /// </summary>
        /// <param name="filePath">配置文件路径</param>
        /// <returns>代码生成器配置</returns>
        public static CodeGeneratorConfiguration LoadFromFile(string filePath)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(filePath, optional: false, reloadOnChange: true)
                .Build();

            return LoadFromConfiguration(configuration);
        }

        /// <summary>
        /// 获取默认配置
        /// </summary>
        /// <returns>默认配置</returns>
        public static CodeGeneratorConfiguration GetDefaultConfiguration()
        {
            return new CodeGeneratorConfiguration();
        }

        /// <summary>
        /// 将配置转换为代码生成器选项
        /// </summary>
        /// <param name="configuration">配置</param>
        /// <returns>代码生成器选项</returns>
        public static CodeGeneratorOptions ToOptions(this CodeGeneratorConfiguration configuration)
        {
            return new CodeGeneratorOptions
            {
                OutputPath = configuration.Base.ProjectRoot,
                TemplatesToGenerate = configuration.Templates.DefaultTemplates,
                OverwriteExisting = configuration.Base.OverwriteExisting,
                Author = configuration.Base.Author,
                Company = configuration.Base.Company,
                NamespacePrefix = configuration.Base.NamespacePrefix,
                AddFileHeader = configuration.Base.AddFileHeader,
                GenerateAsyncMethods = true,
                GenerateCacheSupport = true,
                GenerateTransactionSupport = true,
                GenerateAuthorization = true,
                GenerateLogging = true,
                GenerateUnitTests = configuration.Base.GenerateUnitTests,
                GenerateXmlDocumentation = configuration.CodeStyle.Comments.GenerateXmlDocumentation
            };
        }
    }
}