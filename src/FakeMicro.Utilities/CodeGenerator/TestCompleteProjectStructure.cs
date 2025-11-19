using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using FakeMicro.Utilities.CodeGenerator.Entities;
using FakeMicro.Utilities.CodeGenerator.Templates;

namespace FakeMicro.Utilities.CodeGenerator.Test
{
    /// <summary>
    /// 完整的项目结构测试
    /// 测试所有类型的文件生成到正确的项目位置
    /// </summary>
    public class TestCompleteProjectStructure
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== 完整项目结构测试 ===");
            
            try
            {
                // 创建Product实体元数据
                var productEntity = new EntityMetadata
                {
                    EntityName = "Product",
                    EntityDescription = "产品",
                    TableName = "products",
                    PrimaryKeyProperty = "Id",
                    PrimaryKeyType = "long",
                    Namespace = "FakeMicro.Entities",
                    IsAuditable = true,
                    IsSoftDeletable = true,
                    SupportMultiTenant = true,
                    Properties = new List<PropertyMetadata>
                    {
                        new PropertyMetadata 
                        { 
                            Name = "ProductName", 
                            Type = "string", 
                            IsRequired = true,
                            MaxLength = 200
                        },
                        new PropertyMetadata 
                        { 
                            Name = "Price", 
                            Type = "decimal", 
                            IsRequired = true
                        },
                        new PropertyMetadata 
                        { 
                            Name = "Description", 
                            Type = "string", 
                            IsRequired = false,
                            MaxLength = 1000
                        },
                        new PropertyMetadata 
                        { 
                            Name = "CategoryId", 
                            Type = "long", 
                            IsRequired = true,
                            IsForeignKey = true
                        }
                    }
                };

                var basePath = @"F:\ProjectCode\OrlanFackeMicro\src";
                
                Console.WriteLine($"✅ 创建Product实体元数据成功");
                Console.WriteLine($"   - 实体名称: {productEntity.EntityName}");
                Console.WriteLine($"   - 属性数量: {productEntity.Properties.Count}");
                Console.WriteLine();

                // 测试所有生成类型的文件路径
                Console.WriteLine("=== 文件路径映射测试 ===");
                var generationTypes = new[]
                {
                    GenerationType.Entity,
                    GenerationType.Interface,
                    GenerationType.Repository,
                    GenerationType.RepositoryImplementation,
                    GenerationType.Controller,
                    GenerationType.Grain,
                    GenerationType.Dto
                };

                foreach (var type in generationTypes)
                {
                    var ns = ProjectStructureMapping.GetNamespace(type, productEntity.EntityName);
                    var filePath = ProjectStructureMapping.GetFilePath(type, productEntity.EntityName, basePath);
                    var className = ProjectStructureMapping.GetClassName(type, productEntity.EntityName);
                    
                    Console.WriteLine($"{type}:");
                    Console.WriteLine($"  命名空间: {ns}");
                    Console.WriteLine($"  类名: {className}");
                    Console.WriteLine($"  文件路径: {filePath}");
                    Console.WriteLine();
                }

                // 生成各种类型的代码并保存到正确位置
                Console.WriteLine("=== 生成并保存文件 ===");

                // 1. 生成实体类
                var entityCode = EntityTemplate.Generate(productEntity);
                var entityPath = ProjectStructureMapping.GetFilePath(GenerationType.Entity, productEntity.EntityName, basePath);
                await SaveFileToCorrectLocation(entityPath, entityCode);
                Console.WriteLine($"✅ 实体类已保存到: {entityPath}");

                // 2. 生成仓储接口
                var repositoryInterfaceCode = RepositoryInterfaceTemplate.Generate(productEntity);
                var repositoryInterfacePath = ProjectStructureMapping.GetFilePath(GenerationType.Repository, productEntity.EntityName, basePath);
                await SaveFileToCorrectLocation(repositoryInterfacePath, repositoryInterfaceCode);
                Console.WriteLine($"✅ 仓储接口已保存到: {repositoryInterfacePath}");

                // 3. 生成仓储实现
                var repositoryImplementationCode = RepositoryImplementationTemplate.Generate(productEntity);
                var repositoryImplementationPath = ProjectStructureMapping.GetFilePath(GenerationType.RepositoryImplementation, productEntity.EntityName, basePath);
                await SaveFileToCorrectLocation(repositoryImplementationPath, repositoryImplementationCode);
                Console.WriteLine($"✅ 仓储实现已保存到: {repositoryImplementationPath}");

                // 4. 生成控制器
                var controllerCode = ControllerTemplate.Generate(productEntity);
                var controllerPath = ProjectStructureMapping.GetFilePath(GenerationType.Controller, productEntity.EntityName, basePath);
                await SaveFileToCorrectLocation(controllerPath, controllerCode);
                Console.WriteLine($"✅ 控制器已保存到: {controllerPath}");

                // 5. 生成Grain
                var grainCode = GrainTemplate.Generate(productEntity);
                var grainPath = ProjectStructureMapping.GetFilePath(GenerationType.Grain, productEntity.EntityName, basePath);
                await SaveFileToCorrectLocation(grainPath, grainCode);
                Console.WriteLine($"✅ Grain已保存到: {grainPath}");

                Console.WriteLine();
                Console.WriteLine("🎉 完整项目结构测试成功！所有文件都已保存到正确的项目位置。");
                Console.WriteLine();
                Console.WriteLine("=== 生成的文件位置总结 ===");
                Console.WriteLine($"实体类: {entityPath}");
                Console.WriteLine($"仓储接口: {repositoryInterfacePath}");
                Console.WriteLine($"仓储实现: {repositoryImplementationPath}");
                Console.WriteLine($"控制器: {controllerPath}");
                Console.WriteLine($"Grain: {grainPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 测试失败: {ex.Message}");
                Console.WriteLine($"详细信息: {ex}");
            }

            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        /// <summary>
        /// 保存文件到正确位置，确保目录存在
        /// </summary>
        private static async Task SaveFileToCorrectLocation(string filePath, string content)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Console.WriteLine($"  📁 创建目录: {directory}");
            }
            
            await File.WriteAllTextAsync(filePath, content);
        }
    }
}