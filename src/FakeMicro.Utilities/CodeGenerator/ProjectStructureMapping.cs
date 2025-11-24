using System;
using System.Collections.Generic;
using System.IO;

namespace FakeMicro.Utilities.CodeGenerator
{
    /// <summary>
    /// 项目结构映射类
    /// 负责管理不同生成类型的命名空间和文件路径映射
    /// 遵循FakeMicro项目的DDD架构规范
    /// </summary>
    public static class ProjectStructureMapping
    {
        /// <summary>
        /// 命名空间映射字典
        /// Key: GenerationType, Value: 命名空间模板
        /// </summary>
        private static readonly Dictionary<GenerationType, string> _namespaceMappings = new()
        {
            { GenerationType.Entity, "FakeMicro.Entities" },
            { GenerationType.Interface, "FakeMicro.Interfaces" },
            { GenerationType.Result, "FakeMicro.Interfaces.Models" },
            { GenerationType.Request, "FakeMicro.Interfaces.Models" },
            { GenerationType.Grain, "FakeMicro.Grains" },
            { GenerationType.Dto, "FakeMicro.Interfaces.Models" },
            { GenerationType.Controller, "FakeMicro.Api.Controllers" },
            { GenerationType.Repository, "FakeMicro.DatabaseAccess.Interfaces" },
            { GenerationType.RepositoryImplementation, "FakeMicro.DatabaseAccess.Repositories" }
        };

        /// <summary>
        /// 文件路径映射字典
        /// Key: GenerationType, Value: 相对路径模板
        /// </summary>
        private static readonly Dictionary<GenerationType, string> _pathMappings = new()
        {
            { GenerationType.Entity, "FakeMicro.Entities" },
            { GenerationType.Interface, "FakeMicro.Interfaces" },
            { GenerationType.Result, "FakeMicro.Interfaces\\Models" },
            { GenerationType.Request, "FakeMicro.Interfaces\\Models" },
            { GenerationType.Grain, "FakeMicro.Grains" },
            { GenerationType.Dto, "FakeMicro.Interfaces\\Models" },
            { GenerationType.Controller, "FakeMicro.Api\\Controllers" },
            { GenerationType.Repository, "FakeMicro.DatabaseAccess\\Interfaces" },
            { GenerationType.RepositoryImplementation, "FakeMicro.DatabaseAccess\\Repositories" }
        };

        /// <summary>
        /// 文件名后缀映射字典
        /// Key: GenerationType, Value: 文件名后缀
        /// </summary>
        private static readonly Dictionary<GenerationType, string> _fileSuffixMappings = new()
        {
            { GenerationType.Entity, ".cs" },
            { GenerationType.Interface, "Grain.cs" },
            { GenerationType.Result, "Result.cs" },
            { GenerationType.Request, "Request.cs" },
            { GenerationType.Grain, "Grain.cs" },
            { GenerationType.Dto, "Dto.cs" },
            { GenerationType.Controller, "Controller.cs" },
            { GenerationType.Repository, "Repository.cs" },
            { GenerationType.RepositoryImplementation, "Repository.cs" }
        };

        /// <summary>
        /// 获取指定生成类型的命名空间
        /// </summary>
        /// <param name="type">生成类型</param>
        /// <param name="entityName">实体名称</param>
        /// <returns>命名空间字符串</returns>
        public static string GetNamespace(GenerationType type, string entityName)
        {
            if (_namespaceMappings.TryGetValue(type, out var ns))
            {
                return ns;
            }
            
            throw new ArgumentException($"不支持的生成类型: {type}", nameof(type));
        }

        /// <summary>
        /// 获取指定生成类型的文件路径
        /// </summary>
        /// <param name="type">生成类型</param>
        /// <param name="entityName">实体名称</param>
        /// <param name="basePath">基础路径</param>
        /// <returns>完整文件路径</returns>
        public static string GetFilePath(GenerationType type, string entityName, string basePath)
        {
            if (!_pathMappings.TryGetValue(type, out var pathTemplate))
            {
                throw new ArgumentException($"不支持的生成类型: {type}", nameof(type));
            }

            var fileName = GetFileName(type, entityName);
            var relativePath = Path.Combine(pathTemplate, entityName,fileName);
            return Path.Combine(basePath, relativePath);
        }

        /// <summary>
        /// 获取文件名
        /// </summary>
        /// <param name="type">生成类型</param>
        /// <param name="entityName">实体名称</param>
        /// <returns>文件名</returns>
        public static string GetFileName(GenerationType type, string entityName)
        {
            if (!_fileSuffixMappings.TryGetValue(type, out var suffix))
            {
                suffix = ".cs";
            }
            return GetFileNameWithSuffix(type, entityName, suffix);
        }

        /// <summary>
        /// 获取文件名（带自定义后缀）
        /// </summary>
        /// <param name="type">生成类型</param>
        /// <param name="entityName">实体名称</param>
        /// <param name="suffix">文件后缀</param>
        /// <returns>文件名</returns>
        private static string GetFileNameWithSuffix(GenerationType type, string entityName, string suffix)
        {
            return type switch
            {
                GenerationType.Interface => $"I{entityName}{suffix}",
                GenerationType.Result => $"{entityName}{suffix}",
                GenerationType.Request => $"{entityName}{suffix}",
                GenerationType.Grain => $"{entityName}{suffix}",
                GenerationType.Dto => $"{entityName}{suffix}",
                GenerationType.Controller => $"{entityName}{suffix}",
                GenerationType.Repository => $"I{entityName}{suffix}",
                GenerationType.RepositoryImplementation => $"{entityName}{suffix}",
                _ => $"{entityName}{suffix}"
            };
        }

        /// <summary>
        /// 获取所有需要创建的目录列表
        /// </summary>
        /// <param name="basePath">基础路径</param>
        /// <returns>目录路径列表</returns>
        public static List<string> GetAllDirectories(string basePath)
        {
            var directories = new List<string>();
            
            foreach (var pathTemplate in _pathMappings.Values)
            {
                var fullPath = Path.Combine(basePath, pathTemplate);
                if (!directories.Contains(fullPath))
                {
                    directories.Add(fullPath);
                }
            }
            
            return directories;
        }

        /// <summary>
        /// 验证生成类型是否支持
        /// </summary>
        /// <param name="type">生成类型</param>
        /// <returns>是否支持</returns>
        public static bool IsSupportedType(GenerationType type)
        {
            return _namespaceMappings.ContainsKey(type) && 
                   _pathMappings.ContainsKey(type) && 
                   _fileSuffixMappings.ContainsKey(type);
        }

        /// <summary>
        /// 获取所有支持的生成类型
        /// </summary>
        /// <returns>支持的生成类型列表</returns>
        public static List<GenerationType> GetSupportedTypes()
        {
            return new List<GenerationType>(_namespaceMappings.Keys);
        }

        /// <summary>
        /// 根据实体名称生成符合命名规范的类名
        /// </summary>
        /// <param name="type">生成类型</param>
        /// <param name="entityName">实体名称</param>
        /// <returns>格式化的类名</returns>
        public static string GetClassName(GenerationType type, string entityName)
        {
            return type switch
            {
                GenerationType.Interface => $"I{entityName}",
                GenerationType.Repository => $"I{entityName}Repository",
                GenerationType.RepositoryImplementation => $"{entityName}Repository",
                GenerationType.Controller => $"{entityName}Controller",
                GenerationType.Grain => $"{entityName}Grain",
                _ => entityName
            };
        }
    }
}