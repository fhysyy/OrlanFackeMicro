using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FakeMicro.Utilities.CodeGenerator.Entities;

namespace FakeMicro.Utilities.CodeGenerator
{
    /// <summary>
    /// Orleans代码生成器，支持生成接口、Grain、DTO和控制器代码
    /// 遵循Orleans框架最佳实践，集成PostgreSQL数据库
    /// </summary>
    public class CodeGenerator : ICodeGenerator
    {
        private readonly CodeGeneratorConfiguration _configuration;
        private readonly string _outputPath;
        private readonly OverwriteStrategy _overwriteStrategy;

        public CodeGenerator(
            CodeGeneratorConfiguration? configuration = null,
            string? outputPath = null,
            OverwriteStrategy overwriteStrategy = OverwriteStrategy.Backup)
        {
            _configuration = configuration ?? new CodeGeneratorConfiguration();
            // 如果没有指定输出路径，使用当前目录作为解决方案根目录
            _outputPath = outputPath ?? Directory.GetCurrentDirectory();
            _overwriteStrategy = overwriteStrategy;
            
            EnsureOutputDirectoriesExist();
        }

        private void EnsureOutputDirectoriesExist()
        {
            var directories = ProjectStructureMapping.GetAllDirectories(_outputPath);
            foreach (var directory in directories)
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
        }

        /// <summary>
        /// 生成代码的主要方法
        /// </summary>
        /// <param name="entities">实体列表</param>
        /// <param name="generationType">生成类型</param>
        /// <param name="overwriteStrategy">覆盖策略</param>
        /// <param name="outputPath">输出路径，如果为null则使用默认路径</param>
        /// <returns>生成结果</returns>
        public async Task<CodeGenerationResult> GenerateCodeAsync(
            List<EntityMetadata> entities, 
            GenerationType generationType = GenerationType.All,
            OverwriteStrategy? overwriteStrategy = null,
            string? outputPath = null)
        {
            var result = new CodeGenerationResult
            {
                IsSuccess = true,
                StartTime = DateTime.UtcNow,
                OutputPath = outputPath
            };

            var strategy = overwriteStrategy ?? _overwriteStrategy;
            // 使用传入的outputPath，如果为null则使用默认路径
            var targetOutputPath = outputPath ?? _outputPath;

            try
            {
                foreach (var entity in entities)
                {
                    var entityResult = await GenerateEntityCodeAsync(entity, generationType, strategy, targetOutputPath);
                    if (!entityResult.IsSuccess)
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = entityResult.ErrorMessage;
                        result.ErrorType = entityResult.ErrorType;
                        break;
                    }

                    result.GeneratedFiles.AddRange(entityResult.GeneratedFiles);
                    result.Warnings.AddRange(entityResult.Warnings);
                }

                result.EndTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"生成代码时发生错误: {ex.Message}";
                result.ErrorType = GeneratorErrorType.Unknown;
                result.EndTime = DateTime.UtcNow;
            }

            return result;
        }

        private async Task<CodeGenerationResult> GenerateEntityCodeAsync(
            EntityMetadata entity, 
            GenerationType generationType, 
            OverwriteStrategy overwriteStrategy,
            string outputPath)
        {
            var result = new CodeGenerationResult { IsSuccess = true };

            // 生成接口文件
            if (generationType.HasFlag(GenerationType.Entity))
            {
                await GenerateEntityAsync(entity, overwriteStrategy, outputPath);
            }

            if (generationType.HasFlag(GenerationType.Interface))
            {
                await GenerateInterfaceAsync(entity, overwriteStrategy, outputPath);
            }

            // 生成结果类文件
            if (generationType.HasFlag(GenerationType.Result))
            {
                await GenerateResultAsync(entity, overwriteStrategy, outputPath);
            }

            // 生成请求类文件
            if (generationType.HasFlag(GenerationType.Request))
            {
                await GenerateRequestAsync(entity, overwriteStrategy, outputPath);
            }

            if (generationType.HasFlag(GenerationType.Grain))
            {
                await GenerateGrainAsync(entity, overwriteStrategy, outputPath);
            }

            if (generationType.HasFlag(GenerationType.Dto))
            {
                await GenerateDtoAsync(entity, overwriteStrategy, outputPath);
            }

            if (generationType.HasFlag(GenerationType.Controller))
            {
                await GenerateControllerAsync(entity, overwriteStrategy, outputPath);
            }

            if (generationType.HasFlag(GenerationType.Repository))
            {
                await GenerateRepositoryAsync(entity, overwriteStrategy, outputPath);
            }

            if (generationType.HasFlag(GenerationType.RepositoryImplementation))
            {
                await GenerateRepositoryImplementationAsync(entity, overwriteStrategy, outputPath);
            }

            return result;
        }

        private async Task GenerateEntityAsync(EntityMetadata entity, OverwriteStrategy overwriteStrategy, string outputPath)
        {
            var content = Templates.EntityTemplate.Generate(entity);
            var filePath = ProjectStructureMapping.GetFilePath(GenerationType.Entity, entity.EntityName, outputPath);
            await WriteFileWithStrategyAsync(filePath, content, overwriteStrategy);
        }

        private async Task GenerateResultAsync(EntityMetadata entity, OverwriteStrategy overwriteStrategy, string outputPath)
        {
            var content = Templates.ResultTemplate.Generate(entity);
            var filePath = ProjectStructureMapping.GetFilePath(GenerationType.Result, entity.EntityName, outputPath);

            await WriteFileWithStrategyAsync(filePath, content, overwriteStrategy);
        }
        private async Task GenerateInterfaceAsync(EntityMetadata entity, OverwriteStrategy overwriteStrategy, string outputPath)
        {
            var content = Templates.InterfaceTemplate.Generate(entity);
            var filePath = ProjectStructureMapping.GetFilePath(GenerationType.Interface, entity.EntityName, outputPath);

            await WriteFileWithStrategyAsync(filePath, content, overwriteStrategy);
        }

  

        private async Task GenerateRequestAsync(EntityMetadata entity, OverwriteStrategy overwriteStrategy, string outputPath)
        {
            var content = Templates.RequestTemplate.Generate(entity);
            var filePath = ProjectStructureMapping.GetFilePath(GenerationType.Request, entity.EntityName, outputPath);

            await WriteFileWithStrategyAsync(filePath, content, overwriteStrategy);
        }

        private async Task GenerateGrainAsync(EntityMetadata entity, OverwriteStrategy overwriteStrategy, string outputPath)
        {
            var content = Templates.GrainTemplate.Generate(entity);
            var filePath = ProjectStructureMapping.GetFilePath(GenerationType.Grain, entity.EntityName, outputPath);

            await WriteFileWithStrategyAsync(filePath, content, overwriteStrategy);
        }

        private async Task GenerateDtoAsync(EntityMetadata entity, OverwriteStrategy overwriteStrategy, string outputPath)
        {
            var content = Templates.DtoTemplate.Generate(entity);
            var filePath = ProjectStructureMapping.GetFilePath(GenerationType.Dto, entity.EntityName, outputPath);

            await WriteFileWithStrategyAsync(filePath, content, overwriteStrategy);
        }

        private async Task GenerateControllerAsync(EntityMetadata entity, OverwriteStrategy overwriteStrategy, string outputPath)
        {
            var content = Templates.ControllerTemplate.Generate(entity);
            var filePath = ProjectStructureMapping.GetFilePath(GenerationType.Controller, entity.EntityName, outputPath);

            await WriteFileWithStrategyAsync(filePath, content, overwriteStrategy);
        }

        private async Task GenerateRepositoryAsync(EntityMetadata entity, OverwriteStrategy overwriteStrategy, string outputPath)
        {
            var content = Templates.RepositoryInterfaceTemplate.Generate(entity);
            var filePath = ProjectStructureMapping.GetFilePath(GenerationType.Repository, entity.EntityName, outputPath);

            await WriteFileWithStrategyAsync(filePath, content, overwriteStrategy);
        }

        private async Task GenerateRepositoryImplementationAsync(EntityMetadata entity, OverwriteStrategy overwriteStrategy, string outputPath)
        {
            var content = Templates.RepositoryImplementationTemplate.Generate(entity);
            var filePath = ProjectStructureMapping.GetFilePath(GenerationType.RepositoryImplementation, entity.EntityName, outputPath);

            await WriteFileWithStrategyAsync(filePath, content, overwriteStrategy);
        }

        private async Task WriteFileWithStrategyAsync(string filePath, string content, OverwriteStrategy strategy)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(filePath))
            {
                switch (strategy)
                {
                    case OverwriteStrategy.Skip:
                        return;
                    case OverwriteStrategy.Backup:
                        var backupPath = GetBackupPath(filePath);
                        File.Move(filePath, backupPath);
                        break;
                    case OverwriteStrategy.Overwrite:
                        // 直接覆盖，无需额外操作
                        break;
                }
            }

            await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
        }

        private string GetBackupPath(string originalPath)
        {
            var directory = Path.GetDirectoryName(originalPath);
            var fileName = Path.GetFileNameWithoutExtension(originalPath);
            var extension = Path.GetExtension(originalPath);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
            
            // 如果目录为null，使用当前目录
            if (string.IsNullOrEmpty(directory))
            {
                directory = Directory.GetCurrentDirectory();
            }
            
            return Path.Combine(directory, $"{fileName}_backup_{timestamp}{extension}");
        }

        /// <summary>
        /// 获取所有可用的实体类型
        /// </summary>
        /// <returns>实体类型列表</returns>
        public async Task<List<string>> GetAvailableEntityTypesAsync()
        {
            // 这里可以从配置文件或数据库中获取实际的实体类型
            // 现在返回一些示例实体类型
            return await Task.FromResult(new List<string>
            {
                "User",
                "Product", 
                "Order",
                "Category",
                "Permission",
                "Role"
            });
        }

        /// <summary>
        /// 创建实体元数据
        /// </summary>
        /// <param name="entityName">实体名称</param>
        /// <param name="properties">属性列表</param>
        /// <returns>实体元数据</returns>
        public EntityMetadata CreateEntityMetadata(string entityName, List<PropertyMetadata> properties)
        {
            return new EntityMetadata
            {
                EntityName = entityName,
                EntityDescription = entityName,
                Properties = properties,
                Namespace = _configuration.Base.DefaultNamespace,
                PrimaryKeyProperty = properties.FirstOrDefault(p => p.IsPrimaryKey)?.Name ?? "Id",
                PrimaryKeyType = properties.FirstOrDefault(p => p.IsPrimaryKey)?.Type ?? "int"
            };
        }

        /// <summary>
        /// 预览生成的代码
        /// </summary>
        /// <param name="entity">实体元数据</param>
        /// <param name="generationType">生成类型</param>
        /// <returns>预览结果</returns>
        public async Task<Dictionary<GenerationType, string>> PreviewCodeAsync(EntityMetadata entity, GenerationType generationType)
        {
            var preview = new Dictionary<GenerationType, string>();

            if (generationType.HasFlag(GenerationType.Entity))
            {
                preview[GenerationType.Entity] = Templates.EntityTemplate.Generate(entity);
            }

            if (generationType.HasFlag(GenerationType.Interface))
            {
                preview[GenerationType.Interface] = Templates.InterfaceTemplate.Generate(entity);
            }

            if (generationType.HasFlag(GenerationType.Result))
            {
                preview[GenerationType.Result] = Templates.ResultTemplate.Generate(entity);
            }

            if (generationType.HasFlag(GenerationType.Request))
            {
                preview[GenerationType.Request] = Templates.RequestTemplate.Generate(entity);
            }

            if (generationType.HasFlag(GenerationType.Grain))
            {
                preview[GenerationType.Grain] = Templates.GrainTemplate.Generate(entity);
            }

            if (generationType.HasFlag(GenerationType.Dto))
            {
                preview[GenerationType.Dto] = Templates.DtoTemplate.Generate(entity);
            }

            if (generationType.HasFlag(GenerationType.Controller))
            {
                preview[GenerationType.Controller] = Templates.ControllerTemplate.Generate(entity);
            }

            if (generationType.HasFlag(GenerationType.Repository))
            {
                preview[GenerationType.Repository] = Templates.RepositoryInterfaceTemplate.Generate(entity);
            }

            if (generationType.HasFlag(GenerationType.RepositoryImplementation))
            {
                preview[GenerationType.RepositoryImplementation] = Templates.RepositoryImplementationTemplate.Generate(entity);
            }

            return preview;
        }
    }

    /// <summary>
    /// 覆盖策略枚举
    /// </summary>
    public enum OverwriteStrategy
    {
        Overwrite,    // 覆盖
        Backup,       // 备份后覆盖
        Skip          // 跳过
    }
}