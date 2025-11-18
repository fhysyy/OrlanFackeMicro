using System;
using System.Collections.Generic;

namespace FakeMicro.Utilities.CodeGenerator.Test
{
    /// <summary>
    /// 手动验证模板生成
    /// </summary>
    public class ManualTest
    {
        public static void Main()
        {
            Console.WriteLine("=== 手动验证模板生成 ===\n");

            // 创建测试实体
            var testEntity = new EntityMetadata
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
                        IsIdentity = true
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
                    }
                }
            };

            try
            {
                // 测试Entity模板
                Console.WriteLine("=== 生成Entity模板 ===");
                var entityCode = Templates.EntityTemplate.Generate(testEntity);
                Console.WriteLine("✅ Entity模板生成成功!");
                Console.WriteLine($"代码长度: {entityCode.Length} 字符");
                Console.WriteLine("代码预览:");
                Console.WriteLine(entityCode.Substring(0, Math.Min(500, entityCode.Length)) + (entityCode.Length > 500 ? "..." : ""));
                Console.WriteLine();

                // 测试Interface模板
                Console.WriteLine("=== 生成Interface模板 ===");
                var interfaceCode = Templates.InterfaceTemplate.Generate(testEntity);
                Console.WriteLine("✅ Interface模板生成成功!");
                Console.WriteLine($"代码长度: {interfaceCode.Length} 字符");
                Console.WriteLine("代码预览:");
                Console.WriteLine(interfaceCode.Substring(0, Math.Min(500, interfaceCode.Length)) + (interfaceCode.Length > 500 ? "..." : ""));
                Console.WriteLine();

                // 测试Controller模板
                Console.WriteLine("=== 生成Controller模板 ===");
                var controllerCode = Templates.ControllerTemplate.Generate(testEntity);
                Console.WriteLine("✅ Controller模板生成成功!");
                Console.WriteLine($"代码长度: {controllerCode.Length} 字符");
                Console.WriteLine("代码预览:");
                Console.WriteLine(controllerCode.Substring(0, Math.Min(500, controllerCode.Length)) + (controllerCode.Length > 500 ? "..." : ""));
                Console.WriteLine();

                Console.WriteLine("=== 所有模板验证完成 ===");
                Console.WriteLine("✅ 所有模板都能正常生成代码!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 模板生成失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }
    }
}