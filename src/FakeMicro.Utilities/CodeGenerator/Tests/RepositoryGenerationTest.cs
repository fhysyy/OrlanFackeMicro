using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FakeMicro.Utilities.CodeGenerator;
using FakeMicro.Utilities.CodeGenerator.Entities;

namespace FakeMicro.Utilities.CodeGenerator.Tests
{
    /// <summary>
    /// 代码生成器测试类，用于验证仓储接口和实现生成功能
    /// </summary>
    public class RepositoryGenerationTest
    {
        private const string TestOutputPath = @"f:\Orleans\OrlanFackeMicro\src\Generated";

        public static async Task TestRepositoryGeneration()
        {
            Console.WriteLine("开始测试代码生成器的仓储功能...");

            try
            {
                // 创建代码生成器实例
                var generator = new CodeGenerator(new CodeGeneratorConfiguration(), TestOutputPath);

                // 创建实体元数据
                var entity = new EntityMetadata
                {
                    EntityName = "User",
                    Namespace = "FakeMicro.Test",
                    PrimaryKeyType = "Guid",
                    Properties = new List<PropertyMetadata>
                    {
                        new PropertyMetadata { Name = "Id", Type = "Guid", IsPrimaryKey = true, IsNullable = false },
                        new PropertyMetadata { Name = "UserName", Type = "string", IsNullable = false, IsRequired = true },
                        new PropertyMetadata { Name = "Email", Type = "string", IsNullable = true, IsRequired = false },
                        new PropertyMetadata { Name = "FullName", Type = "string", IsNullable = true },
                        new PropertyMetadata { Name = "IsDeleted", Type = "bool", IsNullable = false, DefaultValue = "false" },
                        new PropertyMetadata { Name = "CreatedAt", Type = "DateTime", IsNullable = false },
                        new PropertyMetadata { Name = "UpdatedAt", Type = "DateTime", IsNullable = true }
                    }
                };

                // 只生成仓储接口和实现
                var generationTypes = GenerationType.Repository | GenerationType.RepositoryImplementation;

                Console.WriteLine($"开始生成 {entity.EntityName} 实体相关的代码...");

                // 生成代码
                var result = await generator.GenerateCodeAsync(new List<EntityMetadata> { entity }, generationTypes, OverwriteStrategy.Overwrite);

                if (result.IsSuccess)
                {
                    Console.WriteLine($"✅ 代码生成成功！");
                    Console.WriteLine($"生成的 {entity.EntityName} 文件：");
                    foreach (var file in result.GeneratedFiles)
                    {
                        Console.WriteLine($"  - {file}");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ 代码生成失败: {result.ErrorMessage}");
                    return;
                }

                // 预览生成的代码
                Console.WriteLine($"\n开始预览生成的代码...");
                var preview = await generator.PreviewCodeAsync(entity, generationTypes);

                foreach (var kvp in preview)
                {
                    Console.WriteLine($"\n=== {kvp.Key} 代码预览 ===");
                    Console.WriteLine(kvp.Value);
                    Console.WriteLine(new string('=', 50));
                }

                // 验证生成的文件
                var repositoryInterfacePath = Path.Combine(TestOutputPath, "FakeMicro.Domain/Repositories", $"I{entity.EntityName}Repository.cs");
                var repositoryImplementationPath = Path.Combine(TestOutputPath, "FakeMicro.Domain/Repositories", $"{entity.EntityName}Repository.cs");

                Console.WriteLine("\n验证生成的文件：");
                
                if (File.Exists(repositoryInterfacePath))
                {
                    Console.WriteLine($"✅ 仓储接口文件已生成: {repositoryInterfacePath}");
                    var interfaceContent = await File.ReadAllTextAsync(repositoryInterfacePath);
                    if (interfaceContent.Contains($"I{entity.EntityName}Repository"))
                    {
                        Console.WriteLine("✅ 仓储接口内容正确");
                    }
                    else
                    {
                        Console.WriteLine("❌ 仓储接口内容不正确");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ 仓储接口文件未生成: {repositoryInterfacePath}");
                }

                if (File.Exists(repositoryImplementationPath))
                {
                    Console.WriteLine($"✅ 仓储实现文件已生成: {repositoryImplementationPath}");
                    var implementationContent = await File.ReadAllTextAsync(repositoryImplementationPath);
                    if (implementationContent.Contains($"{entity.EntityName}Repository"))
                    {
                        Console.WriteLine("✅ 仓储实现内容正确");
                    }
                    else
                    {
                        Console.WriteLine("❌ 仓储实现内容不正确");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ 仓储实现文件未生成: {repositoryImplementationPath}");
                }

                Console.WriteLine("\n🎉 仓储代码生成功能测试完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 测试过程中发生错误: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }

        public static async Task TestDifferentEntityTypes()
        {
            Console.WriteLine("\n测试不同主键类型的实体生成...");

            var generator = new CodeGenerator(new CodeGeneratorConfiguration(), TestOutputPath);

            var testEntities = new[]
            {
                new { Name = "Product", PrimaryKeyType = "long" },
                new { Name = "OrderItem", PrimaryKeyType = "int" },
                new { Name = "Category", PrimaryKeyType = "string" }
            };

            foreach (var testEntity in testEntities)
            {
                Console.WriteLine($"\n生成 {testEntity.Name} 实体 (主键类型: {testEntity.PrimaryKeyType})...");

                var entity = new EntityMetadata
                {
                    EntityName = testEntity.Name,
                    Namespace = "FakeMicro.Test",
                    PrimaryKeyType = testEntity.PrimaryKeyType,
                    Properties = new List<PropertyMetadata>
                    {
                        new PropertyMetadata { Name = "Id", Type = testEntity.PrimaryKeyType, IsPrimaryKey = true, IsNullable = false },
                        new PropertyMetadata { Name = "Name", Type = "string", IsNullable = false, IsRequired = true },
                        new PropertyMetadata { Name = "IsDeleted", Type = "bool", IsNullable = false, DefaultValue = "false" }
                    }
                };

                var generationTypes = GenerationType.Repository | GenerationType.RepositoryImplementation;
                var result = await generator.GenerateCodeAsync(new List<EntityMetadata> { entity }, generationTypes, OverwriteStrategy.Overwrite);

                if (result.IsSuccess)
                {
                    Console.WriteLine($"✅ {testEntity.Name} 实体代码生成成功");
                }
                else
                {
                    Console.WriteLine($"❌ {testEntity.Name} 实体代码生成失败: {result.ErrorMessage}");
                }
            }
        }
    }
}