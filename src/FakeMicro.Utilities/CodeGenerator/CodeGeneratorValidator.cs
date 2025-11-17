using FakeMicro.Utilities.CodeGenerator;
using FakeMicro.Utilities.CodeGenerator.Requests;
using System.ComponentModel.DataAnnotations;

namespace FakeMicro.Utilities.CodeGenerator
{
    /// <summary>
    /// 代码生成器验证器
    /// 负责验证代码生成请求的有效性
    /// </summary>
    public class CodeGeneratorValidator : ICodeGeneratorValidator
    {
        /// <summary>
        /// 验证代码生成请求
        /// </summary>
        /// <param name="request">代码生成请求</param>
        /// <returns>验证结果</returns>
        public async Task<ValidationResult> ValidateAsync(CodeGenerationRequest request)
        {
            var errors = new List<string>();

            // 验证实体名称
            if (string.IsNullOrWhiteSpace(request.EntityName))
            {
                errors.Add("实体名称不能为空");
            }
            else if (!IsValidIdentifier(request.EntityName))
            {
                errors.Add("实体名称必须是有效的C#标识符");
            }

            // 验证生成类型
            if (request.GenerationType == GenerationType.None)
            {
                errors.Add("必须指定至少一种生成类型");
            }

            // 验证属性列表
            if (request.Properties != null && request.Properties.Any())
            {
                foreach (var property in request.Properties)
                {
                    var propertyErrors = await ValidatePropertyAsync(property);
                    errors.AddRange(propertyErrors);
                }
            }
            else
            {
                // 如果没有指定属性，创建默认属性
                request.Properties = CreateDefaultProperties(request.EntityName);
            }

            // 验证输出路径
            if (!string.IsNullOrEmpty(request.OutputPath))
            {
                try
                {
                    var path = Path.GetFullPath(request.OutputPath);
                    var directory = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        errors.Add($"输出路径的目录不存在: {directory}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"输出路径无效: {ex.Message}");
                }
            }

            return new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        /// <summary>
        /// 验证属性
        /// </summary>
        /// <param name="property">属性请求</param>
        /// <returns>验证错误列表</returns>
        private async Task<List<string>> ValidatePropertyAsync(PropertyRequest property)
        {
            var errors = new List<string>();

            // 验证属性名称
            if (string.IsNullOrWhiteSpace(property.Name))
            {
                errors.Add("属性名称不能为空");
            }
            else if (!IsValidIdentifier(property.Name))
            {
                errors.Add($"属性名称 '{property.Name}' 不是有效的C#标识符");
            }

            // 验证属性类型
            if (string.IsNullOrWhiteSpace(property.Type))
            {
                errors.Add($"属性 '{property.Name}' 的类型不能为空");
            }
            else if (!IsValidType(property.Type))
            {
                errors.Add($"属性 '{property.Name}' 的类型 '{property.Type}' 不支持");
            }

            // 验证最大长度
            if (property.MaxLength.HasValue && property.MaxLength.Value <= 0)
            {
                errors.Add($"属性 '{property.Name}' 的最大长度必须大于0");
            }

            return await Task.FromResult(errors);
        }

        /// <summary>
        /// 检查是否为有效的C#标识符
        /// </summary>
        /// <param name="identifier">标识符</param>
        /// <returns>是否为有效标识符</returns>
        private bool IsValidIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;

            // 检查是否以字母或下划线开头
            if (!char.IsLetter(identifier[0]) && identifier[0] != '_')
                return false;

            // 检查其余字符是否为字母、数字或下划线
            for (int i = 1; i < identifier.Length; i++)
            {
                if (!char.IsLetterOrDigit(identifier[i]) && identifier[i] != '_')
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 检查是否为有效的C#类型
        /// </summary>
        /// <param name="type">类型名称</param>
        /// <returns>是否为有效类型</returns>
        private bool IsValidType(string type)
        {
            var validTypes = new HashSet<string>
            {
                "string", "int", "long", "short", "byte",
                "bool", "char", "float", "double", "decimal",
                "DateTime", "DateTimeOffset", "TimeSpan",
                "Guid", "int?", "long?", "short?", "byte?",
                "bool?", "char?", "float?", "double?", "decimal?",
                "DateTime?", "DateTimeOffset?", "Guid?"
            };

            return validTypes.Contains(type) || 
                   type.EndsWith("?") && validTypes.Contains(type.Substring(0, type.Length - 1));
        }

        /// <summary>
        /// 创建默认属性
        /// </summary>
        /// <param name="entityName">实体名称</param>
        /// <returns>默认属性列表</returns>
        private List<PropertyRequest> CreateDefaultProperties(string entityName)
        {
            return new List<PropertyRequest>
            {
                new PropertyRequest
                {
                    Name = "Id",
                    Type = "int",
                    IsNullable = false,
                    IsPrimaryKey = true,
                    IsRequired = true,
                    Description = "主键ID"
                },
                new PropertyRequest
                {
                    Name = "Name",
                    Type = "string",
                    IsNullable = false,
                    IsPrimaryKey = false,
                    IsRequired = true,
                    MaxLength = 100,
                    Description = "名称"
                },
                new PropertyRequest
                {
                    Name = "CreatedAt",
                    Type = "DateTime",
                    IsNullable = false,
                    IsPrimaryKey = false,
                    IsRequired = true,
                    Description = "创建时间"
                },
                new PropertyRequest
                {
                    Name = "UpdatedAt",
                    Type = "DateTime",
                    IsNullable = true,
                    IsPrimaryKey = false,
                    IsRequired = false,
                    Description = "更新时间"
                },
                new PropertyRequest
                {
                    Name = "IsDeleted",
                    Type = "bool",
                    IsNullable = false,
                    IsPrimaryKey = false,
                    IsRequired = true,
                    Description = "是否已删除"
                }
            };
        }
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 是否验证通过
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 验证错误列表
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// 代码生成器验证器接口
    /// </summary>
    public interface ICodeGeneratorValidator
    {
        /// <summary>
        /// 验证代码生成请求
        /// </summary>
        /// <param name="request">代码生成请求</param>
        /// <returns>验证结果</returns>
        Task<ValidationResult> ValidateAsync(CodeGenerationRequest request);
    }
}