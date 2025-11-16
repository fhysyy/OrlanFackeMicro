using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FakeMicro.Utilities
{
    /// <summary>
    /// 代码生成器
    /// </summary>
    public static class CodeGenerator
    {
        /// <summary>
        /// 代码生成模板
        /// </summary>
        public abstract class CodeTemplate
        {
            public abstract string Generate();
            public virtual string GetFileName() => $"{GetType().Name}.cs";
        }
        
        /// <summary>
        /// 实体类模板
        /// </summary>
        public class EntityTemplate : CodeTemplate
        {
            public string Namespace { get; set; } = "FakeMicro.Entities";
            public string ClassName { get; set; } = "MyEntity";
            public List<PropertyDefinition> Properties { get; set; } = new List<PropertyDefinition>();
            public bool GenerateConstructor { get; set; } = true;
            public bool GenerateToString { get; set; } = true;
            
            public override string Generate()
            {
                var sb = new StringBuilder();
                
                // Using语句
                sb.AppendLine("using System;");
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine();
                
                // 命名空间
                sb.AppendLine($"namespace {Namespace}");
                sb.AppendLine("{");
                
                // 类定义
                sb.AppendLine($"    public class {ClassName}");
                sb.AppendLine("    {");
                
                // 属性
                foreach (var prop in Properties)
                {
                    sb.AppendLine($"        public {prop.Type} {prop.Name} {{ get; set; }}");
                }
                sb.AppendLine();
                
                // 构造函数
                if (GenerateConstructor && Properties.Any())
                {
                    sb.AppendLine($"        public {ClassName}()");
                    sb.AppendLine("        {");
                    sb.AppendLine("        }");
                    sb.AppendLine();
                    
                    sb.AppendLine($"        public {ClassName}({string.Join(", ", Properties.Select(p => $"{p.Type} {p.Name.ToLower()}"))})");
                    sb.AppendLine("        {");
                    foreach (var prop in Properties)
                    {
                        sb.AppendLine($"            {prop.Name} = {prop.Name.ToLower()};");
                    }
                    sb.AppendLine("        }");
                    sb.AppendLine();
                }
                
                // ToString方法
                if (GenerateToString)
                {
                    sb.AppendLine("        public override string ToString()");
                    sb.AppendLine("        {");
                    if (Properties.Any())
                    {
                        sb.AppendLine($"            return $\"{ClassName} {{ ");
                        var props = Properties.Select(p => $"{p.Name} = {{{p.Name}}}");
                        sb.AppendLine(string.Join(", ", props));
                        sb.AppendLine("            }\";");
                    }
                    else
                    {
                        sb.AppendLine($"            return $\"{ClassName}\";");
                    }
                    sb.AppendLine("        }");
                }
                
                sb.AppendLine("    }");
                sb.AppendLine("}");
                
                return sb.ToString();
            }
            
            public override string GetFileName()
            {
                return $"{ClassName}.cs";
            }
        }
        
        /// <summary>
        /// 属性定义
        /// </summary>
        public class PropertyDefinition
        {
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = "string";
            public string? DefaultValue { get; set; }
            public bool IsRequired { get; set; }
            public int MaxLength { get; set; }
        }
        
        /// <summary>
        /// 仓储接口模板
        /// </summary>
        public class RepositoryInterfaceTemplate : CodeTemplate
        {
            public string Namespace { get; set; } = "FakeMicro.Interfaces";
            public string InterfaceName { get; set; } = "IMyRepository";
            public string EntityName { get; set; } = "MyEntity";
            public string KeyType { get; set; } = "int";
            public List<string> Methods { get; set; } = new List<string>
            {
                "GetAll", "GetById", "Add", "Update", "Delete"
            };
            
            public override string Generate()
            {
                var sb = new StringBuilder();
                
                sb.AppendLine("using System;");
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine("using System.Threading.Tasks;");
                sb.AppendLine();
                
                sb.AppendLine($"namespace {Namespace}");
                sb.AppendLine("{");
                sb.AppendLine($"    public interface {InterfaceName}");
                sb.AppendLine("    {");
                
                foreach (var method in Methods)
                {
                    switch (method.ToLower())
                    {
                        case "getall":
                            sb.AppendLine($"        Task<IEnumerable<{EntityName}>> GetAllAsync();");
                            break;
                        case "getbyid":
                            sb.AppendLine($"        Task<{EntityName}?> GetByIdAsync({KeyType} id);");
                            break;
                        case "add":
                            sb.AppendLine($"        Task<{EntityName}> AddAsync({EntityName} entity);");
                            break;
                        case "update":
                            sb.AppendLine($"        Task<{EntityName}> UpdateAsync({EntityName} entity);");
                            break;
                        case "delete":
                            sb.AppendLine($"        Task<bool> DeleteAsync({KeyType} id);");
                            break;
                        case "exists":
                            sb.AppendLine($"        Task<bool> ExistsAsync({KeyType} id);");
                            break;
                        case "count":
                            sb.AppendLine($"        Task<int> CountAsync();");
                            break;
                    }
                }
                
                sb.AppendLine("    }");
                sb.AppendLine("}");
                
                return sb.ToString();
            }
            
            public override string GetFileName()
            {
                return $"{InterfaceName}.cs";
            }
        }
        
        /// <summary>
        /// API控制器模板
        /// </summary>
        public class ApiControllerTemplate : CodeTemplate
        {
            public string Namespace { get; set; } = "FakeMicro.Api.Controllers";
            public string ControllerName { get; set; } = "MyController";
            public string EntityName { get; set; } = "MyEntity";
            public string RepositoryInterface { get; set; } = "IMyRepository";
            public List<string> Actions { get; set; } = new List<string>
            {
                "GetAll", "GetById", "Create", "Update", "Delete"
            };
            
            public override string Generate()
            {
                var sb = new StringBuilder();
                
                sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine("using System.Threading.Tasks;");
                sb.AppendLine($"using FakeMicro.Interfaces;");
                sb.AppendLine();
                
                sb.AppendLine($"namespace {Namespace}");
                sb.AppendLine("{");
                sb.AppendLine($"    [ApiController]");
                sb.AppendLine($"    [Route(\"api/[controller]\")]");
                sb.AppendLine($"    public class {ControllerName} : ControllerBase");
                sb.AppendLine("    {");
                sb.AppendLine($"        private readonly {RepositoryInterface} _repository;");
                sb.AppendLine();
                sb.AppendLine($"        public {ControllerName}({RepositoryInterface} repository)");
                sb.AppendLine("        {");
                sb.AppendLine($"            _repository = repository;");
                sb.AppendLine("        }");
                sb.AppendLine();
                
                foreach (var action in Actions)
                {
                    switch (action.ToLower())
                    {
                        case "getall":
                            sb.AppendLine("        [HttpGet]");
                            sb.AppendLine($"        public async Task<ActionResult<IEnumerable<{EntityName}>>> GetAll()");
                            sb.AppendLine("        {");
                            sb.AppendLine($"            var entities = await _repository.GetAllAsync();");
                            sb.AppendLine($"            return Ok(entities);");
                            sb.AppendLine("        }");
                            sb.AppendLine();
                            break;
                        case "getbyid":
                            sb.AppendLine("        [HttpGet(\"{id}\")]");
                            sb.AppendLine($"        public async Task<ActionResult<{EntityName}>> GetById(int id)");
                            sb.AppendLine("        {");
                            sb.AppendLine($"            var entity = await _repository.GetByIdAsync(id);");
                            sb.AppendLine($"            if (entity == null) return NotFound();");
                            sb.AppendLine($"            return Ok(entity);");
                            sb.AppendLine("        }");
                            sb.AppendLine();
                            break;
                        case "create":
                            sb.AppendLine("        [HttpPost]");
                            sb.AppendLine($"        public async Task<ActionResult<{EntityName}>> Create({EntityName} entity)");
                            sb.AppendLine("        {");
                            sb.AppendLine($"            var created = await _repository.AddAsync(entity);");
                            sb.AppendLine($"            return CreatedAtAction(nameof(GetById), new {{ id = created.Id }}, created);");
                            sb.AppendLine("        }");
                            sb.AppendLine();
                            break;
                        case "update":
                            sb.AppendLine("        [HttpPut(\"{id}\")]");
                            sb.AppendLine($"        public async Task<ActionResult<{EntityName}>> Update(int id, {EntityName} entity)");
                            sb.AppendLine("        {");
                            sb.AppendLine($"            if (id != entity.Id) return BadRequest();");
                            sb.AppendLine($"            var updated = await _repository.UpdateAsync(entity);");
                            sb.AppendLine($"            return Ok(updated);");
                            sb.AppendLine("        }");
                            sb.AppendLine();
                            break;
                        case "delete":
                            sb.AppendLine("        [HttpDelete(\"{id}\")]");
                            sb.AppendLine($"        public async Task<ActionResult> Delete(int id)");
                            sb.AppendLine("        {");
                            sb.AppendLine($"            var result = await _repository.DeleteAsync(id);");
                            sb.AppendLine($"            if (!result) return NotFound();");
                            sb.AppendLine($"            return NoContent();");
                            sb.AppendLine("        }");
                            sb.AppendLine();
                            break;
                    }
                }
                
                sb.AppendLine("    }");
                sb.AppendLine("}");
                
                return sb.ToString();
            }
            
            public override string GetFileName()
            {
                return $"{ControllerName}.cs";
            }
        }
        
        /// <summary>
        /// 生成代码文件
        /// </summary>
        public static void GenerateCodeFile(CodeTemplate template, string outputDirectory)
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
            
            var filePath = Path.Combine(outputDirectory, template.GetFileName());
            var code = template.Generate();
            
            File.WriteAllText(filePath, code, Encoding.UTF8);
        }
        
        /// <summary>
        /// 从JSON生成实体类
        /// </summary>
        public static EntityTemplate CreateEntityFromJson(string json, string className, string @namespace = "FakeMicro.Entities")
        {
            var template = new EntityTemplate
            {
                Namespace = @namespace,
                ClassName = className
            };
            
            // 简单的JSON属性解析（实际实现需要更复杂的JSON解析）
            var properties = ExtractPropertiesFromJson(json);
            template.Properties.AddRange(properties);
            
            return template;
        }
        
        /// <summary>
        /// 从JSON字符串提取属性
        /// </summary>
        private static List<PropertyDefinition> ExtractPropertiesFromJson(string json)
        {
            var properties = new List<PropertyDefinition>();
            
            // 简单的JSON属性提取（实际实现需要完整的JSON解析）
            var matches = Regex.Matches(json, @"""(\w+)"":\s*""?([^"",}]+)""?");
            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 2)
                {
                    var propertyName = match.Groups[1].Value;
                    var propertyValue = match.Groups[2].Value;
                    
                    var property = new PropertyDefinition
                    {
                        Name = propertyName,
                        Type = InferTypeFromValue(propertyValue)
                    };
                    
                    properties.Add(property);
                }
            }
            
            return properties;
        }
        
        /// <summary>
        /// 从值推断类型
        /// </summary>
        private static string InferTypeFromValue(string value)
        {
            if (int.TryParse(value, out _)) return "int";
            if (long.TryParse(value, out _)) return "long";
            if (double.TryParse(value, out _)) return "double";
            if (bool.TryParse(value, out _)) return "bool";
            if (DateTime.TryParse(value, out _)) return "DateTime";
            
            return "string";
        }
        
        /// <summary>
        /// 生成完整的CRUD代码
        /// </summary>
        public static void GenerateCrudCode(string entityName, string @namespace, string outputDirectory)
        {
            // 生成实体类
            var entityTemplate = new EntityTemplate
            {
                Namespace = $"{@namespace}.Entities",
                ClassName = entityName,
                Properties = new List<PropertyDefinition>
                {
                    new PropertyDefinition { Name = "Id", Type = "int" },
                    new PropertyDefinition { Name = "Name", Type = "string" },
                    new PropertyDefinition { Name = "CreatedAt", Type = "DateTime" }
                }
            };
            
            GenerateCodeFile(entityTemplate, Path.Combine(outputDirectory, "Entities"));
            
            // 生成仓储接口
            var repositoryTemplate = new RepositoryInterfaceTemplate
            {
                Namespace = $"{@namespace}.Interfaces",
                InterfaceName = $"I{entityName}Repository",
                EntityName = entityName
            };
            
            GenerateCodeFile(repositoryTemplate, Path.Combine(outputDirectory, "Interfaces"));
            
            // 生成API控制器
            var controllerTemplate = new ApiControllerTemplate
            {
                Namespace = $"{@namespace}.Api.Controllers",
                ControllerName = $"{entityName}Controller",
                EntityName = entityName,
                RepositoryInterface = $"I{entityName}Repository"
            };
            
            GenerateCodeFile(controllerTemplate, Path.Combine(outputDirectory, "Controllers"));
        }
        
        /// <summary>
        /// 代码生成配置
        /// </summary>
        public class CodeGenerationConfig
        {
            public string OutputDirectory { get; set; } = "GeneratedCode";
            public string Namespace { get; set; } = "FakeMicro";
            public bool GenerateTests { get; set; } = true;
            public bool GenerateDocumentation { get; set; } = true;
        }
    }
}