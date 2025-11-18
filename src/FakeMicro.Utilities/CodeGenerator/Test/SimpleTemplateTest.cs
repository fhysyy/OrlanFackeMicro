using System;
using System.Collections.Generic;

namespace FakeMicro.Utilities.CodeGenerator.Test
{
    /// <summary>
    /// 简单的模板生成测试
    /// </summary>
    public class SimpleTemplateTest
    {
        public static void Main()
        {
            Console.WriteLine("=== 模板生成测试 ===\n");

            // 创建测试实体元数据
            var testEntity = CreateTestUserEntity();

            Console.WriteLine("测试实体信息:");
            Console.WriteLine($"实体名称: {testEntity.EntityName}");
            Console.WriteLine($"实体描述: {testEntity.EntityDescription}");
            Console.WriteLine($"主键属性: {testEntity.PrimaryKeyProperty}");
            Console.WriteLine($"主键类型: {testEntity.PrimaryKeyType}");
            Console.WriteLine($"属性数量: {testEntity.Properties.Count}\n");

            // 测试生成所有类型的模板
            TestTemplate("Entity", () => Templates.EntityTemplate.Generate(testEntity));
            TestTemplate("Interface", () => Templates.InterfaceTemplate.Generate(testEntity));
            TestTemplate("Request", () => Templates.RequestTemplate.Generate(testEntity));
            TestTemplate("Result", () => Templates.ResultTemplate.Generate(testEntity));
            TestTemplate("Dto", () => Templates.DtoTemplate.Generate(testEntity));
            TestTemplate("Grain", () => Templates.GrainTemplate.Generate(testEntity));
            TestTemplate("Controller", () => Templates.ControllerTemplate.Generate(testEntity));

            Console.WriteLine("=== 测试完成 ===");
        }

        private static void TestTemplate(string templateName, Func<string> generateFunc)
        {
            Console.WriteLine($"=== 测试生成 {templateName} ===");
            
            try
            {
                var code = generateFunc();
                Console.WriteLine($"✅ {templateName} 模板生成成功!");
                Console.WriteLine($"代码长度: {code.Length} 字符");
                Console.WriteLine($"前100字符预览: {code.Substring(0, Math.Min(100, code.Length))}...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {templateName} 生成异常: {ex.Message}");
            }
            
            Console.WriteLine();
        }

        /// <summary>
        /// 创建测试用户实体
        /// </summary>
        private static EntityMetadata CreateTestUserEntity()
        {
            return new EntityMetadata
            {
                EntityName = "User",
                EntityDescription = "用户实体",
                Namespace = "FakeMicro.Domain.Entities",
                PrimaryKeyProperty = "Id",
                PrimaryKeyType = "long",
                Properties = new List<PropertyMetadata>
                {
                    new PropertyMetadata
                    {
                        Name = "Id",
                        Type = "long",
                        Description = "用户ID",
                        IsPrimaryKey = true,
                        IsRequired = true,
                        IsAutoIncrement = true
                    },
                    new PropertyMetadata
                    {
                        Name = "Username",
                        Type = "string",
                        Description = "用户名",
                        IsRequired = true,
                        MinLength = 3,
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
                        Name = "PasswordHash",
                        Type = "string",
                        Description = "密码哈希",
                        IsRequired = true,
                        MaxLength = 255
                    },
                    new PropertyMetadata
                    {
                        Name = "FullName",
                        Type = "string",
                        Description = "全名",
                        IsRequired = false,
                        MaxLength = 100
                    },
                    new PropertyMetadata
                    {
                        Name = "IsActive",
                        Type = "bool",
                        Description = "是否激活",
                        IsRequired = false,
                        DefaultValue = "true"
                    },
                    new PropertyMetadata
                    {
                        Name = "CreatedAt",
                        Type = "DateTime",
                        Description = "创建时间",
                        IsRequired = true,
                        IsReadOnly = true
                    },
                    new PropertyMetadata
                    {
                        Name = "UpdatedAt",
                        Type = "DateTime",
                        Description = "更新时间",
                        IsRequired = true,
                        IsReadOnly = true
                    }
                }
            };
        }
    }
}