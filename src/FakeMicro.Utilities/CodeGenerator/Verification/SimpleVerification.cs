using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FakeMicro.Utilities.CodeGenerator;
using FakeMicro.Utilities.CodeGenerator.Entities;

namespace FakeMicro.Utilities.CodeGenerator.Verification
{
    /// <summary>
    /// 简单验证脚本，用于测试仓储接口和实现生成功能
    /// </summary>
    public class SimpleVerification
    {
        public static async Task<bool> TestRepositoryGeneration()
        {
            Console.WriteLine("=== 开始验证仓储接口和实现生成功能 ===");
            
            try
            {
                // 创建测试输出目录
                var testOutputPath = @"f:\Orleans\OrlanFackeMicro\src\Generated";
                if (!Directory.Exists(testOutputPath))
                {
                    Directory.CreateDirectory(testOutputPath);
                }

                // 创建实体信息
                var entityInfo = new EntityInfo
                {
                    EntityName = "TestUser",
                    Namespace = "FakeMicro.Test",
                    PrimaryKeyType = "Guid",
                    Properties = new List<PropertyInfo>
                    {
                        new PropertyInfo { Name = "Id", Type = "Guid", IsPrimaryKey = true, IsNullable = false },
                        new PropertyInfo { Name = "UserName", Type = "string", IsNullable = false, IsRequired = true },
                        new PropertyInfo { Name = "Email", Type = "string", IsNullable = true },
                        new PropertyInfo { Name = "FullName", Type = "string", IsNullable = true },
                        new PropertyInfo { Name = "IsDeleted", Type = "bool", IsNullable = false, DefaultValue = "false" },
                        new PropertyInfo { Name = "CreatedAt", Type = "DateTime", IsNullable = false },
                        new PropertyInfo { Name = "UpdatedAt", Type = "DateTime", IsNullable = true }
                    }
                };

                Console.WriteLine($"测试实体: {entityInfo.EntityName}");
                Console.WriteLine($"主键类型: {entityInfo.PrimaryKeyType}");
                Console.WriteLine($"属性数量: {entityInfo.Properties.Count}");
                
                // 转换EntityInfo为EntityMetadata
                var entityMetadata = new EntityMetadata
                {
                    EntityName = entityInfo.EntityName,
                    EntityDescription = string.IsNullOrEmpty(entityInfo.Description) ? entityInfo.EntityName : entityInfo.Description,
                    Namespace = entityInfo.Namespace,
                    PrimaryKeyProperty = entityInfo.PrimaryKeyName ?? "Id",
                    PrimaryKeyType = entityInfo.PrimaryKeyType,
                    IsSoftDeletable = entityInfo.SupportSoftDelete,
                    //SupportMultiTenant = entityInfo.SupportMultiTenant,
                    Properties = entityInfo.Properties.Select(p => new PropertyMetadata
                    {
                        Name = p.Name,
                        Type = p.Type,
                        IsNullable = p.IsNullable,
                        IsRequired = p.IsRequired,
                        IsPrimaryKey = p.IsPrimaryKey,
                        //IsForeignKey = p.IsForeignKey,
                        IsNavigationProperty = !string.IsNullOrEmpty(p.ForeignEntityName),
                        //RelatedEntityName = p.ForeignEntityName,
                        DefaultValue = p.DefaultValue,
                        MaxLength = p.MaxLength
                    }).ToList()
                };

                // 测试仓储接口模板
                Console.WriteLine("\n1. 测试仓储接口模板生成...");
                var interfaceCode = Templates.RepositoryInterfaceTemplate.Generate(entityMetadata);
                
                if (!string.IsNullOrEmpty(interfaceCode) && interfaceCode.Contains($"I{entityInfo.EntityName}Repository"))
                {
                    Console.WriteLine("✅ 仓储接口模板生成成功");
                    Console.WriteLine($"生成的接口代码长度: {interfaceCode.Length} 字符");
                }
                else
                {
                    Console.WriteLine("❌ 仓储接口模板生成失败");
                    return false;
                }
                
                // 测试仓储实现模板
                Console.WriteLine("\n2. 测试仓储实现模板生成...");
                var implementationCode = Templates.RepositoryImplementationTemplate.Generate(entityMetadata);
                
                if (!string.IsNullOrEmpty(implementationCode) && implementationCode.Contains($"{entityInfo.EntityName}Repository"))
                {
                    Console.WriteLine("✅ 仓储实现模板生成成功");
                    Console.WriteLine($"生成的实现代码长度: {implementationCode.Length} 字符");
                }
                else
                {
                    Console.WriteLine("❌ 仓储实现模板生成失败");
                    return false;
                }
                
                // 保存生成的文件进行验证
                Console.WriteLine("\n3. 保存生成的代码到文件...");
                var repositoryPath = Path.Combine(testOutputPath, "FakeMicro.Domain", "Repositories");
                if (!Directory.Exists(repositoryPath))
                {
                    Directory.CreateDirectory(repositoryPath);
                }
                
                var interfaceFilePath = Path.Combine(repositoryPath, $"I{entityInfo.EntityName}Repository.cs");
                var implementationFilePath = Path.Combine(repositoryPath, $"{entityInfo.EntityName}Repository.cs");
                
                await File.WriteAllTextAsync(interfaceFilePath, interfaceCode);
                await File.WriteAllTextAsync(implementationFilePath, implementationCode);
                
                Console.WriteLine($"✅ 仓储接口文件已保存: {interfaceFilePath}");
                Console.WriteLine($"✅ 仓储实现文件已保存: {implementationFilePath}");
                
                // 验证文件内容
                Console.WriteLine("\n4. 验证生成的文件内容...");
                
                // 验证接口文件
                var interfaceContent = await File.ReadAllTextAsync(interfaceFilePath);
                var interfaceValidations = new[]
                {
                    ("接口声明", $"I{entityInfo.EntityName}Repository"),
                    ("继承IRepository", "IRepository"),
                    ("主键类型", "Guid"),
                    ("GetAsync方法", "GetAsync"),
                    ("InsertAsync方法", "InsertAsync"),
                    ("UpdateAsync方法", "UpdateAsync"),
                    ("DeleteAsync方法", "DeleteAsync")
                };
                
                bool interfaceValid = true;
                foreach (var (name, expected) in interfaceValidations)
                {
                    if (interfaceContent.Contains(expected))
                    {
                        Console.WriteLine($"✅ {name}: 包含 '{expected}'");
                    }
                    else
                    {
                        Console.WriteLine($"❌ {name}: 缺少 '{expected}'");
                        interfaceValid = false;
                    }
                }
                
                // 验证实现文件
                var implementationContent = await File.ReadAllTextAsync(implementationFilePath);
                var implementationValidations = new[]
                {
                    ("类声明", $"{entityInfo.EntityName}Repository"),
                    ("继承接口", $"I{entityInfo.EntityName}Repository"),
                    ("SqlSugarHelper", "SqlSugarHelper"),
                    ("Logger", "ILogger"),
                    ("Mapper", "IMapper"),
                    ("GetAsync实现", "GetAsync"),
                    ("InsertAsync实现", "InsertAsync"),
                    ("UpdateAsync实现", "UpdateAsync"),
                    ("DeleteAsync实现", "DeleteAsync"),
                    ("软删除支持", "IsDeleted")
                };
                
                bool implementationValid = true;
                foreach (var (name, expected) in implementationValidations)
                {
                    if (implementationContent.Contains(expected))
                    {
                        Console.WriteLine($"✅ {name}: 包含 '{expected}'");
                    }
                    else
                    {
                        Console.WriteLine($"❌ {name}: 缺少 '{expected}'");
                        implementationValid = false;
                    }
                }
                
                // 测试不同主键类型
                Console.WriteLine("\n5. 测试不同主键类型...");
                var primaryKeyTypes = new[] { "Guid", "long", "int", "string" };
                bool allTypesValid = true;
                
                foreach (var pkType in primaryKeyTypes)
                {
                    Console.WriteLine($"测试主键类型: {pkType}");
                    var testEntity = new EntityInfo
                    {
                        EntityName = $"TestEntity_{pkType}",
                        PrimaryKeyType = pkType,
                        Namespace = "FakeMicro.Test",
                        Properties = new List<PropertyInfo>
                        {
                            new PropertyInfo { Name = "Id", Type = pkType, IsPrimaryKey = true, IsNullable = false },
                            new PropertyInfo { Name = "Name", Type = "string", IsNullable = false }
                        }
                    };
                    
                    // 转换测试实体为EntityMetadata
                    var testEntityMetadata = new EntityMetadata
                    {
                        EntityName = testEntity.EntityName,
                        EntityDescription = string.IsNullOrEmpty(testEntity.Description) ? testEntity.EntityName : testEntity.Description,
                        Namespace = testEntity.Namespace,
                        PrimaryKeyProperty = testEntity.PrimaryKeyName ?? "Id",
                        PrimaryKeyType = testEntity.PrimaryKeyType,
                        IsSoftDeletable = testEntity.SupportSoftDelete,
                        //SupportMultiTenant = testEntity.SupportMultiTenant,
                        Properties = testEntity.Properties.Select(p => new PropertyMetadata
                        {
                            Name = p.Name,
                            Type = p.Type,
                            IsNullable = p.IsNullable,
                            IsRequired = p.IsRequired,
                            IsPrimaryKey = p.IsPrimaryKey,
                            //IsForeignKey = p.IsForeignKey,
                            IsNavigationProperty = !string.IsNullOrEmpty(p.ForeignEntityName),
                            //RelatedEntityName = p.ForeignEntityName,
                            DefaultValue = p.DefaultValue,
                            MaxLength = p.MaxLength
                        }).ToList()
                    };

                    var testInterfaceCode = Templates.RepositoryInterfaceTemplate.Generate(testEntityMetadata);
                    var testImplementationCode = Templates.RepositoryImplementationTemplate.Generate(testEntityMetadata);
                    
                    if (testInterfaceCode.Contains($"IRepository<{testEntity.EntityName}, {pkType}>") &&
                        testImplementationCode.Contains($"class {testEntity.EntityName}Repository"))
                    {
                        Console.WriteLine($"✅ 主键类型 {pkType} 测试通过");
                    }
                    else
                    {
                        Console.WriteLine($"❌ 主键类型 {pkType} 测试失败");
                        allTypesValid = false;
                    }
                }
                
                // 总结
                Console.WriteLine("\n=== 验证结果总结 ===");
                Console.WriteLine($"接口模板生成: {(interfaceValid ? "✅ 通过" : "❌ 失败")}");
                Console.WriteLine($"实现模板生成: {(implementationValid ? "✅ 通过" : "❌ 失败")}");
                Console.WriteLine($"多主键类型支持: {(allTypesValid ? "✅ 通过" : "❌ 失败")}");
                
                var overallSuccess = interfaceValid && implementationValid && allTypesValid;
                Console.WriteLine($"\n总体验证结果: {(overallSuccess ? "✅ 全部通过" : "❌ 部分失败")}");
                
                return overallSuccess;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 验证过程中发生错误: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                return false;
            }
        }
        
        public static async Task ShowGeneratedCode()
        {
            Console.WriteLine("\n=== 显示生成的代码示例 ===");
            
            var entityInfo = new EntityInfo
            {
                EntityName = "User",
                Namespace = "FakeMicro.Test",
                PrimaryKeyType = "Guid",
                Properties = new List<PropertyInfo>
                {
                    new PropertyInfo { Name = "Id", Type = "Guid", IsPrimaryKey = true, IsNullable = false },
                    new PropertyInfo { Name = "UserName", Type = "string", IsNullable = false, IsRequired = true },
                    new PropertyInfo { Name = "Email", Type = "string", IsNullable = true },
                    new PropertyInfo { Name = "FullName", Type = "string", IsNullable = true },
                    new PropertyInfo { Name = "IsDeleted", Type = "bool", IsNullable = false, DefaultValue = "false" }
                }
            };
            
            // 转换EntityInfo为EntityMetadata
            var entityMetadata = new EntityMetadata
            {
                EntityName = entityInfo.EntityName,
                EntityDescription = string.IsNullOrEmpty(entityInfo.Description) ? entityInfo.EntityName : entityInfo.Description,
                Namespace = entityInfo.Namespace,
                PrimaryKeyProperty = entityInfo.PrimaryKeyName ?? "Id",
                PrimaryKeyType = entityInfo.PrimaryKeyType,
                IsSoftDeletable = entityInfo.SupportSoftDelete,
                //SupportMultiTenant = entityInfo.SupportMultiTenant,
                Properties = entityInfo.Properties.Select(p => new PropertyMetadata
                {
                    Name = p.Name,
                    Type = p.Type,
                    IsNullable = p.IsNullable,
                    IsRequired = p.IsRequired,
                    IsPrimaryKey = p.IsPrimaryKey,
                    //IsForeignKey = p.IsForeignKey,
                    IsNavigationProperty = !string.IsNullOrEmpty(p.ForeignEntityName),
                    //RelatedEntityName = p.ForeignEntityName,
                    DefaultValue = p.DefaultValue,
                    MaxLength = p.MaxLength
                }).ToList()
            };

            Console.WriteLine("\n--- 仓储接口代码 ---");
            var interfaceCode = Templates.RepositoryInterfaceTemplate.Generate(entityMetadata);
            Console.WriteLine(interfaceCode);
            
            Console.WriteLine("\n--- 仓储实现代码（前100行）---");
            var implementationCode = Templates.RepositoryImplementationTemplate.Generate(entityMetadata);
            var lines = implementationCode.Split('\n');
            for (int i = 0; i < Math.Min(100, lines.Length); i++)
            {
                Console.WriteLine(lines[i]);
            }
            if (lines.Length > 100)
            {
                Console.WriteLine($"... (还有 {lines.Length - 100} 行)");
            }
        }
    }
}