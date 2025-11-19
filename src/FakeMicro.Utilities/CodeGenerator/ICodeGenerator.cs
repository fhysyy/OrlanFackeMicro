using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeMicro.Utilities.CodeGenerator.Entities;

namespace FakeMicro.Utilities.CodeGenerator
{
    /// <summary>
    /// 代码生成器接口定义
    /// 遵循Orleans框架最佳实践，集成PostgreSQL数据库
    /// </summary>
    public interface ICodeGenerator
    {
        /// <summary>
        /// 生成代码的主要方法
        /// </summary>
        /// <param name="entities">实体列表</param>
        /// <param name="generationType">生成类型</param>
        /// <param name="overwriteStrategy">覆盖策略</param>
        /// <returns>生成结果</returns>
        Task<CodeGenerationResult> GenerateCodeAsync(
            List<EntityMetadata> entities, 
            GenerationType generationType = GenerationType.All,
            OverwriteStrategy? overwriteStrategy = null);

        /// <summary>
        /// 获取所有可用的实体类型
        /// </summary>
        /// <returns>实体类型列表</returns>
        Task<List<string>> GetAvailableEntityTypesAsync();

        /// <summary>
        /// 创建实体元数据
        /// </summary>
        /// <param name="entityName">实体名称</param>
        /// <param name="properties">属性列表</param>
        /// <returns>实体元数据</returns>
        EntityMetadata CreateEntityMetadata(string entityName, List<PropertyMetadata> properties);

        /// <summary>
        /// 预览代码生成结果
        /// </summary>
        /// <param name="entity">实体元数据</param>
        /// <param name="generationType">生成类型</param>
        /// <returns>生成的代码内容</returns>
        Task<Dictionary<GenerationType, string>> PreviewCodeAsync(EntityMetadata entity, GenerationType generationType);
    }
}