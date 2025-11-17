using System.Text;

namespace FakeMicro.Utilities.CodeGenerator.Templates
{
    /// <summary>
    /// Grain模板
    /// </summary>
    public class GrainTemplate : ITemplate
    {
        public string Generate(EntityMetadata metadata)
        {
            var sb = new StringBuilder();
            
            // Using语句
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using Orleans;");
            sb.AppendLine($"using FakeMicro.Interfaces;");
            sb.AppendLine($"using FakeMicro.Entities;");
            sb.AppendLine($"using FakeMicro.DatabaseAccess;");
            sb.AppendLine("using Microsoft.Extensions.Logging;");
            sb.AppendLine();
            
            // 命名空间
            sb.AppendLine("namespace FakeMicro.Grains");
            sb.AppendLine("{");
            
            // 类定义
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {metadata.EntityDescription} Grain实现");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public class {metadata.EntityName}Grain : Grain, I{metadata.EntityName}Grain");
            sb.AppendLine("    {");
            
            // 字段和构造函数
            sb.AppendLine($"        private readonly SqlSugarRepository<{metadata.EntityName}, int> _repository;");
            sb.AppendLine("        private readonly ILogger<{metadata.EntityName}Grain> _logger;");
            sb.AppendLine();
            sb.AppendLine($"        public {metadata.EntityName}Grain(SqlSugarRepository<{metadata.EntityName}, int> repository, ILogger<{metadata.EntityName}Grain> logger)");
            sb.AppendLine("        {");
            sb.AppendLine("            _repository = repository;");
            sb.AppendLine("            _logger = logger;");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            // 创建方法
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// 创建{metadata.EntityDescription}");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<{metadata.EntityName}Dto> Create{metadata.EntityName}Async(Create{metadata.EntityName}Dto dto)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var entity = dto.Adapt<{metadata.EntityName}>();");
            sb.AppendLine("            await _repository.InsertAsync(entity);");
            sb.AppendLine($"            return entity.Adapt<{metadata.EntityName}Dto>();");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            // 获取方法
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// 获取{metadata.EntityDescription}");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<{metadata.EntityName}Dto?> Get{metadata.EntityName}Async()");
            sb.AppendLine("        {");
            sb.AppendLine($"            var entity = await _repository.GetByIdAsync(this.GetPrimaryKey());");
            sb.AppendLine($"            return entity?.Adapt<{metadata.EntityName}Dto>();");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            // 更新方法
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// 更新{metadata.EntityDescription}");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<{metadata.EntityName}Dto> Update{metadata.EntityName}Async(Update{metadata.EntityName}Dto dto)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var entity = await _repository.GetByIdAsync(this.GetPrimaryKey());");
            sb.AppendLine("            if (entity == null)");
            sb.AppendLine("                throw new KeyNotFoundException($\"{metadata.EntityName} not found\");");
            sb.AppendLine("            dto.Adapt(entity);");
            sb.AppendLine("            await _repository.UpdateAsync(entity);");
            sb.AppendLine($"            return entity.Adapt<{metadata.EntityName}Dto>();");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            // 删除方法
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// 删除{metadata.EntityDescription}");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        public async Task<bool> Delete{metadata.EntityName}Async()");
            sb.AppendLine("        {");
            sb.AppendLine($"            return await _repository.DeleteByIdAsync(this.GetPrimaryKey());");
            sb.AppendLine("        }");
            
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            return sb.ToString();
        }
    }
}