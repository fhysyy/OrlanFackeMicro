using System.Text;

namespace FakeMicro.Utilities.CodeGenerator.Templates
{
    /// <summary>
    /// 控制器模板
    /// </summary>
    public class ControllerTemplate : ITemplate
    {
        public string Generate(EntityMetadata metadata)
        {
            var sb = new StringBuilder();
            
            // Using语句
            sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
            sb.AppendLine("using Microsoft.AspNetCore.Authorization;");
            sb.AppendLine("using System.Threading;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine($"using FakeMicro.Interfaces;");
            sb.AppendLine($"using FakeMicro.Entities;");
            sb.AppendLine($"using Microsoft.Extensions.Logging;");
            sb.AppendLine($"using Orleans;");
            sb.AppendLine();
            
            // 命名空间
            sb.AppendLine("namespace FakeMicro.Api.Controllers");
            sb.AppendLine("{");
            
            // 类定义
            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// {metadata.EntityDescription}控制器");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    [ApiController]");
            sb.AppendLine($"    [Route(\"api/[controller]\")]");
            sb.AppendLine($"    public class {metadata.EntityName}Controller : ControllerBase");
            sb.AppendLine("    {");
            
            // 字段和构造函数
            sb.AppendLine($"        private readonly IClusterClient clusterClient;");
            sb.AppendLine($"  private readonly ILogger<{metadata.EntityName}Controller> logger;");
            sb.AppendLine($"        public {metadata.EntityName}Controller(IClusterClient _clusterClient, ILogger<{metadata.EntityName}Controller> _logger)");
            sb.AppendLine("        {");
            sb.AppendLine($"  logger=  _logger ;      ");
            sb.AppendLine(" _clusterClient= _clusterClient;       ");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            // 获取方法
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// 获取{metadata.EntityDescription}");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        [HttpGet(\"{{id}}\")]");
            sb.AppendLine($"        public async Task<ActionResult<{metadata.EntityName}Dto>> Get{metadata.EntityName}([FromRoute] int id, CancellationToken cancellationToken = default)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var _{metadata.EntityName.ToLower()}Grain = clusterClient.GetGrain<I{metadata.EntityName}Grain>(\"{metadata.EntityName}\");");
            sb.AppendLine($"            var result = await _{metadata.EntityName.ToLower()}Grain.GetAsync();");
            sb.AppendLine("            if (result == null)");
            sb.AppendLine("                return NotFound();");
            sb.AppendLine("            return Ok(result);");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            // 创建方法
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// 创建{metadata.EntityDescription}");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        [HttpPost]");
            sb.AppendLine($"        public async Task<ActionResult<{metadata.EntityName}Dto>> Create{metadata.EntityName}([FromBody] Create{metadata.EntityName}Dto dto, CancellationToken cancellationToken = default)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var _{metadata.EntityName.ToLower()}Grain = clusterClient.GetGrain<I{metadata.EntityName}Grain>(\"{metadata.EntityName}\");");
            sb.AppendLine($"            var result = await _{metadata.EntityName.ToLower()}Grain.CreateAsync(dto);");
            sb.AppendLine($"            return CreatedAtAction(nameof(Get{metadata.EntityName}), new {{ id = result.Id }}, result);");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            // 更新方法
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// 更新{metadata.EntityDescription}");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        [HttpPut(\"{{id}}\")]");
            sb.AppendLine($"        public async Task<ActionResult<{metadata.EntityName}Dto>> Update{metadata.EntityName}([FromRoute] int id, [FromBody] Update{metadata.EntityName}Dto dto, CancellationToken cancellationToken = default)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var _{metadata.EntityName.ToLower()}Grain = clusterClient.GetGrain<I{metadata.EntityName}Grain>(\"{metadata.EntityName}\");");
            sb.AppendLine($"            var result = await _{metadata.EntityName.ToLower()}Grain.UpdateAsync(dto);");
            sb.AppendLine($"            return Ok(result);");
            sb.AppendLine("        }");
            sb.AppendLine();
            
            // 删除方法
            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// 删除{metadata.EntityDescription}");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine($"        [HttpDelete(\"{{id}}\")]");
            sb.AppendLine($"        public async Task<ActionResult> Delete{metadata.EntityName}([FromRoute] int id, CancellationToken cancellationToken = default)");
            sb.AppendLine("        {");
            sb.AppendLine($"            var _{metadata.EntityName.ToLower()}Grain = clusterClient.GetGrain<I{metadata.EntityName}Grain>(\"{metadata.EntityName}\");");
            sb.AppendLine($"            var result = await _{metadata.EntityName.ToLower()}Grain.DeleteAsync();");
            sb.AppendLine($"            if (!result.Success)");
            sb.AppendLine("                return NotFound();");
            sb.AppendLine("            return NoContent();");
            sb.AppendLine("        }");
            
            sb.AppendLine("    }");
            sb.AppendLine("}");
            
            return sb.ToString();
        }
    }
}