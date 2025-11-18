using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FakeMicro.Utilities.CodeGenerator
{
    class SimpleVerificationTest
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== 开始验证代码生成器修复效果 ===");
            Console.WriteLine($"验证时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            try
            {
                // 创建测试用的实体元数据
                var entityMetadata = new EntityMetadata
                {
                    EntityName = "TestUser",
                    EntityDescription = "测试用户实体",
                    Namespace = "FakeMicro.Test",
                    PrimaryKeyProperty = "Id",
                    PrimaryKeyType = "Guid",
                    Properties = new List<PropertyMetadata>
                    {
                        new PropertyMetadata { Name = "Id", Type = "Guid", IsPrimaryKey = true, IsNullable = false },
                        new PropertyMetadata { Name = "UserName", Type = "string", IsNullable = false, IsRequired = true, MaxLength = 50 },
                        new PropertyMetadata { Name = "Email", Type = "string", IsNullable = true, MaxLength = 100 },
                        new PropertyMetadata { Name = "FullName", Type = "string", IsNullable = true, MaxLength = 100 },
                        new PropertyMetadata { Name = "IsDeleted", Type = "bool", IsNullable = false, DefaultValue = "false" },
                        new PropertyMetadata { Name = "CreatedAt", Type = "DateTime", IsNullable = false },
                        new PropertyMetadata { Name = "UpdatedAt", Type = "DateTime", IsNullable = true }
                    }
                };

                Console.WriteLine($"测试实体: {entityMetadata.EntityName}");
                Console.WriteLine($"主键类型: {entityMetadata.PrimaryKeyType}");
                Console.WriteLine($"属性数量: {entityMetadata.Properties.Count}");
                
                // 创建代码生成器实例
                var outputPath = @"f:\Orleans\OrlanFackeMicro\src\Generated";
                var config = new CodeGeneratorConfiguration
                {
                    Base = new BaseConfiguration
                    {
                        DefaultNamespace = "FakeMicro.Test"
                    }
                };
                
                var generator = new CodeGenerator(config, outputPath);
                
                // 测试代码预览功能
                Console.WriteLine("\n1. 测试代码预览功能...");
                var preview = await generator.PreviewCodeAsync(
                    entityMetadata, 
                    GenerationType.Repository | GenerationType.RepositoryImplementation
                );
                
                bool interfacePreviewSuccess = false;
                bool implementationPreviewSuccess = false;
                
                if (preview.ContainsKey(GenerationType.Repository) && !string.IsNullOrEmpty(preview[GenerationType.Repository]))
                {
                    Console.WriteLine("✅ 仓储接口预览代码生成成功");
                    Console.WriteLine($"生成的接口代码长度: {preview[GenerationType.Repository].Length} 字符");
                    interfacePreviewSuccess = true;
                }
                else
                {
                    Console.WriteLine("❌ 仓储接口预览代码生成失败");
                }
                
                if (preview.ContainsKey(GenerationType.RepositoryImplementation) && !string.IsNullOrEmpty(preview[GenerationType.RepositoryImplementation]))
                {
                    Console.WriteLine("✅ 仓储实现预览代码生成成功");
                    Console.WriteLine($"生成的实现代码长度: {preview[GenerationType.RepositoryImplementation].Length} 字符");
                    implementationPreviewSuccess = true;
                }
                else
                {
                    Console.WriteLine("❌ 仓储实现预览代码生成失败");
                }
                
                // 测试实体元数据创建
                Console.WriteLine("\n2. 测试实体元数据创建...");
                var properties = new List<PropertyMetadata>
                {
                    new PropertyMetadata { Name = "Id", Type = "int", IsPrimaryKey = true, IsNullable = false },
                    new PropertyMetadata { Name = "Name", Type = "string", IsNullable = false, IsRequired = true }
                };
                
                var createdEntity = generator.CreateEntityMetadata("TestEntity", properties);
                
                if (createdEntity.EntityName == "TestEntity" && createdEntity.Properties.Count == 2)
                {
                    Console.WriteLine("✅ 实体元数据创建成功");
                }
                else
                {
                    Console.WriteLine("❌ 实体元数据创建失败");
                }
                
                // 验证关键功能
                Console.WriteLine("\n3. 验证关键功能...");
                bool allFeaturesWorking = true;
                
                // 检查GenerationType枚举
                var allTypes = GenerationType.All;
                if (allTypes.HasFlag(GenerationType.Repository) && allTypes.HasFlag(GenerationType.RepositoryImplementation))
                {
                    Console.WriteLine("✅ GenerationType枚举包含仓储相关类型");
                }
                else
                {
                    Console.WriteLine("❌ GenerationType枚举缺少仓储相关类型");
                    allFeaturesWorking = false;
                }
                
                // 检查预览功能
                if (interfacePreviewSuccess && implementationPreviewSuccess)
                {
                    Console.WriteLine("✅ 仓储接口和实现预览功能正常");
                }
                else
                {
                    Console.WriteLine("❌ 仓储接口和实现预览功能异常");
                    allFeaturesWorking = false;
                }
                
                // 验证实体元数据创建
                if (createdEntity != null)
                {
                    Console.WriteLine("✅ 实体元数据创建功能正常");
                }
                else
                {
                    Console.WriteLine("❌ 实体元数据创建功能异常");
                    allFeaturesWorking = false;
                }
                
                // 总结验证结果
                Console.WriteLine("\n=== 验证结果总结 ===");
                if (interfacePreviewSuccess)
                {
                    Console.WriteLine("✅ 仓储接口生成: 通过");
                }
                else
                {
                    Console.WriteLine("❌ 仓储接口生成: 失败");
                }
                
                if (implementationPreviewSuccess)
                {
                    Console.WriteLine("✅ 仓储实现生成: 通过");
                }
                else
                {
                    Console.WriteLine("❌ 仓储实现生成: 失败");
                }
                
                if (allFeaturesWorking)
                {
                    Console.WriteLine("✅ 核心功能验证: 通过");
                }
                else
                {
                    Console.WriteLine("❌ 核心功能验证: 失败");
                }
                
                // 显示生成的代码片段
                if (interfacePreviewSuccess && implementationPreviewSuccess)
                {
                    Console.WriteLine("\n=== 生成的代码片段预览 ===");
                    Console.WriteLine("仓储接口关键代码:");
                    var interfaceLines = preview[GenerationType.Repository].Split('\n');
                    foreach (var line in interfaceLines)
                    {
                        if (line.Contains("interface") || line.Contains("IRepository") || line.Contains("Task<"))
                        {
                            Console.WriteLine(line.Trim());
                        }
                    }
                    
                    Console.WriteLine("\n仓储实现关键代码:");
                    var implementationLines = preview[GenerationType.RepositoryImplementation].Split('\n');
                    foreach (var line in implementationLines)
                    {
                        if (line.Contains("class") || line.Contains("Repository") || line.Contains("SqlSugarRepository"))
                        {
                            Console.WriteLine(line.Trim());
                        }
                    }
                }
                
                if (allFeaturesWorking)
                {
                    Console.WriteLine("\n🎉 所有验证通过！代码生成器仓储接口和实现生成功能已成功修复。");
                }
                else
                {
                    Console.WriteLine("\n❌ 验证过程中发现问题，请检查代码生成器的实现。");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 验证过程中发生错误: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}