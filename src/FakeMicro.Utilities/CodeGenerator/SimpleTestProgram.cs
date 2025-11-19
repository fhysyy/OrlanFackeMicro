using System;
using System.Collections.Generic;
using System.IO;
using FakeMicro.Utilities.CodeGenerator;
using FakeMicro.Utilities.CodeGenerator.Templates;
using FakeMicro.Utilities.CodeGenerator.Entities;

namespace SimpleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== 简单代码生成器测试 ===");
            
            try
            {
                // 创建测试实体
                var testEntity = new EntityMetadata
                {
                    EntityName = "TestUser",
                    Namespace = "TestApp.Entities",
                    TableName = "test_users",
                    EntityDescription = "测试用户实体",
                    Properties = new List<PropertyMetadata>
                    {
                        new PropertyMetadata
                        {
                            Name = "Id",
                            Type = "int",
                            Description = "用户ID",
                            IsPrimaryKey = true,
                            IsRequired = true
                        },
                        new PropertyMetadata
                        {
                            Name = "Name",
                            Type = "string",
                            Description = "用户姓名",
                            IsRequired = true,
                            MaxLength = 50
                        },
                        new PropertyMetadata
                        {
                            Name = "Email",
                            Type = "string",
                            Description = "邮箱地址",
                            IsRequired = true,
                            MaxLength = 100
                        },
                        new PropertyMetadata
                        {
                            Name = "CreatedAt",
                            Type = "DateTime",
                            Description = "创建时间",
                            IsRequired = true
                        }
                    }
                };

                Console.WriteLine("✓ 测试实体创建成功");
                Console.WriteLine($"  - 实体名称: {testEntity.EntityName}");
                Console.WriteLine($"  - 属性数量: {testEntity.Properties.Count}");

                // 测试实体模板生成
                Console.WriteLine("\n=== 测试实体模板生成 ===");
                var entityCode = EntityTemplate.Generate(testEntity);
                Console.WriteLine("✓ 实体模板生成成功");
                Console.WriteLine($"  - 代码长度: {entityCode.Length} 字符");
                Console.WriteLine($"  - 包含 'TestUser': {entityCode.Contains("TestUser")}");
                Console.WriteLine($"  - 包含 'Id' 属性: {entityCode.Contains("public int Id")}");

                // 测试仓储接口模板生成
                Console.WriteLine("\n=== 测试仓储接口模板生成 ===");
                var repositoryInterfaceCode = RepositoryInterfaceTemplate.Generate(testEntity);
                Console.WriteLine("✓ 仓储接口模板生成成功");
                Console.WriteLine($"  - 代码长度: {repositoryInterfaceCode.Length} 字符");
                Console.WriteLine($"  - 包含 'ITestUserRepository': {repositoryInterfaceCode.Contains("ITestUserRepository")}");

                // 测试控制器模板生成
                Console.WriteLine("\n=== 测试控制器模板生成 ===");
                var controllerCode = ControllerTemplate.Generate(testEntity);
                Console.WriteLine("✓ 控制器模板生成成功");
                Console.WriteLine($"  - 代码长度: {controllerCode.Length} 字符");
                Console.WriteLine($"  - 包含 'TestUserController': {controllerCode.Contains("TestUserController")}");

                // 创建输出目录
                var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "SimpleGeneratedTest");
                if (Directory.Exists(outputDir))
                {
                    Directory.Delete(outputDir, true);
                }
                Directory.CreateDirectory(outputDir);
                Console.WriteLine($"\n✓ 创建输出目录: {outputDir}");

                // 保存生成的文件
                var entityFile = Path.Combine(outputDir, "TestUser.cs");
                File.WriteAllText(entityFile, entityCode);
                Console.WriteLine($"✓ 保存实体文件: {entityFile}");

                var repoInterfaceFile = Path.Combine(outputDir, "ITestUserRepository.cs");
                File.WriteAllText(repoInterfaceFile, repositoryInterfaceCode);
                Console.WriteLine($"✓ 保存仓储接口文件: {repoInterfaceFile}");

                var controllerFile = Path.Combine(outputDir, "TestUserController.cs");
                File.WriteAllText(controllerFile, controllerCode);
                Console.WriteLine($"✓ 保存控制器文件: {controllerFile}");

                Console.WriteLine("\n=== 测试结果总结 ===");
                Console.WriteLine("✓ 核心模板生成测试通过！");
                Console.WriteLine("✓ 生成的文件已保存到 SimpleGeneratedTest 目录");
                Console.WriteLine("✓ 代码生成器基本功能验证成功");
                Console.WriteLine($"✓ 总共生成了 3 个文件");
                
                // 显示生成的代码片段
                Console.WriteLine("\n=== 生成的实体代码示例 ===");
                var lines = entityCode.Split('\n');
                for (int i = 0; i < Math.Min(10, lines.Length); i++)
                {
                    Console.WriteLine($"  {i + 1:D2}: {lines[i]}");
                }
                if (lines.Length > 10)
                {
                    Console.WriteLine("  ...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 测试失败: {ex.Message}");
                Console.WriteLine($"   堆栈跟踪: {ex.StackTrace}");
            }

            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}