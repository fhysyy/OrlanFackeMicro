using System;
using System.Collections.Generic;
using FakeMicro.Utilities.CodeGenerator;
using FakeMicro.Utilities.CodeGenerator.Entities;
using FakeMicro.Utilities.CodeGenerator.Templates;

namespace TestGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== 代码生成器测试开始 ===");
            
            try
            {
                // 创建实体元数据
                var entityMetadata = new EntityMetadata
                {
                    EntityName = "User",
                    EntityDescription = "用户实体",
                    Namespace = "TestApp.Entities",
                    PrimaryKeyProperty = "Id",
                    PrimaryKeyType = "int",
                    TableName = "Users",
                    IsAuditable = true,
                    IsSoftDeletable = true,
                    Properties = new List<PropertyMetadata>
                    {
                        new PropertyMetadata
                        {
                            Name = "Id",
                            Type = "int",
                            IsPrimaryKey = true,
                            IsIdentity = true,
                            ColumnName = "Id"
                        },
                        new PropertyMetadata
                        {
                            Name = "UserName",
                            Type = "string",
                            IsNullable = false,
                            MaxLength = 50,
                            Description = "用户名"
                        },
                        new PropertyMetadata
                        {
                            Name = "Email",
                            Type = "string",
                            IsNullable = false,
                            MaxLength = 100,
                            Description = "邮箱地址"
                        },
                        new PropertyMetadata
                        {
                            Name = "CreatedAt",
                            Type = "DateTime",
                            IsNullable = false,
                            Description = "创建时间"
                        }
                    }
                };

                // 设置主键属性元数据
                entityMetadata.PrimaryKeyPropertyMetadata = entityMetadata.Properties[0];

                Console.WriteLine("实体元数据创建成功！");
                Console.WriteLine($"实体名称: {entityMetadata.EntityName}");
                Console.WriteLine($"属性数量: {entityMetadata.Properties?.Count ?? 0}");

                // 测试实体模板
                Console.WriteLine("\n=== 测试实体模板 ===");
                var entityCode = EntityTemplate.Generate(entityMetadata);
                Console.WriteLine("实体代码生成成功！");
                Console.WriteLine($"代码长度: {entityCode.Length} 字符");

                // 测试仓储接口模板
                Console.WriteLine("\n=== 测试仓储接口模板 ===");
                var repositoryInterfaceCode = RepositoryInterfaceTemplate.Generate(entityMetadata);
                Console.WriteLine("仓储接口代码生成成功！");
                Console.WriteLine($"代码长度: {repositoryInterfaceCode.Length} 字符");

                // 测试控制器模板
                Console.WriteLine("\n=== 测试控制器模板 ===");
                var controllerCode = ControllerTemplate.Generate(entityMetadata);
                Console.WriteLine("控制器代码生成成功！");
                Console.WriteLine($"代码长度: {controllerCode.Length} 字符");

                Console.WriteLine("\n=== 所有测试通过！代码生成器工作正常 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
            
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}