using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeMicro.Utilities.CodeGenerator;

namespace FakeMicro.Utilities.CodeGenerator.Test
{
    /// <summary>
    /// 代码生成器测试程序
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== FakeMicro 代码生成器测试 ===\n");

            // 创建代码生成器配置
            var configuration = new CodeGeneratorConfiguration
            {
                Base = new BaseConfiguration
                {
                    OutputPath = "f:\\ProjectCode\\OrlanFackeMicro\\src\\Generated",
                    DefaultNamespace = "FakeMicro"
                }
            };

            // 创建代码生成器
            var generator = new CodeGenerator(configuration);

            // 创建测试实体元数据
            var testEntity = CreateTestUserEntity();

            Console.WriteLine("测试实体信息:");
            Console.WriteLine($"实体名称: {testEntity.EntityName}");
            Console.WriteLine($"实体描述: {testEntity.EntityDescription}");
            Console.WriteLine($"主键属性: {testEntity.PrimaryKeyProperty}");
            Console.WriteLine($"主键类型: {testEntity.PrimaryKeyType}");
            Console.WriteLine($"属性数量: {testEntity.Properties.Count}\n");

            // 测试生成所有类型的模板
            var generationTypes = new[]
            {
                GenerationType.Entity,
                GenerationType.Interface,
                GenerationType.Request,
                GenerationType.Result,
                GenerationType.Dto,
                GenerationType.Grain,
                GenerationType.Controller
            };

            foreach (var generationType in generationTypes)
            {
                Console.WriteLine($"=== 测试生成 {generationType} ===");
                
                try
                {
                    var preview = await generator.PreviewCodeAsync(testEntity, generationType);
                    
                    if (preview.ContainsKey(generationType))
                    {
                        var code = preview[generationType];
                        Console.WriteLine($"✅ {generationType} 模板生成成功!");
                        Console.WriteLine($"代码长度: {code.Length} 字符");
                        Console.WriteLine($"前100字符预览: {code.Substring(0, Math.Min(100, code.Length))}...");
                        
                        // 实际生成文件
                        var result = await generator.GenerateCodeAsync(new List<EntityMetadata> { testEntity }, generationType, OverwriteStrategy.Overwrite);
                        if (result.IsSuccess)
                        {
                            Console.WriteLine($"✅ {generationType} 文件生成成功!");
                            Console.WriteLine($"生成的文件: {string.Join(", ", result.GeneratedFiles)}");
                        }
                        else
                        {
                            Console.WriteLine($"❌ {generationType} 文件生成失败: {result.ErrorMessage}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ {generationType} 模板生成失败: 预览结果为空");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ {generationType} 生成异常: {ex.Message}");
                }
                
                Console.WriteLine();
            }

            Console.WriteLine("=== 测试完成 ===");
            Console.WriteLine("请检查生成的文件以验证代码质量。");
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