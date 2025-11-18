using FakeMicro.Utilities.CodeGenerator.Templates;
using System;
using System.Collections.Generic;

namespace FakeMicro.Utilities.CodeGenerator.Test
{
    /// <summary>
    /// 测试 PropertyMetadata 修复的验证程序
    /// </summary>
    public class TestPropertyMetadataFix
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("开始测试 PropertyMetadata 修复...");
            
            try
            {
                // 创建测试属性
                var property = new PropertyMetadata
                {
                    Name = "Id",
                    Type = "long",
                    Description = "主键ID",
                    IsRequired = true,
                    IsPrimaryKey = true,
                    IsIdentity = true,  // 测试新增的属性
                    ColumnName = "id",  // 测试新增的属性
                    DefaultValue = "1"  // 测试新增的属性
                };
                
                // 创建测试实体
                var entity = new EntityMetadata
                {
                    EntityName = "TestEntity",
                    EntityDescription = "测试实体",
                    TableName = "test_entities",
                    Namespace = "FakeMicro.Models",
                    Properties = new List<PropertyMetadata> { property }
                };
                
                // 测试 EntityTemplate 生成
                var generatedCode = EntityTemplate.Generate(entity);
                
                Console.WriteLine("✅ PropertyMetadata 修复成功！");
                Console.WriteLine("✅ EntityTemplate.Generate() 执行成功！");
                Console.WriteLine("\n生成的代码预览：");
                Console.WriteLine("=" + new string('=', 50));
                Console.WriteLine(generatedCode.Length > 500 ? generatedCode.Substring(0, 500) + "..." : generatedCode);
                Console.WriteLine("=" + new string('=', 50));
                
                Console.WriteLine("\n🎉 所有测试通过！PropertyMetadata 的 IsIdentity 和 ColumnName 属性已成功添加。");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ 测试失败：" + ex.Message);
                Console.WriteLine("详细错误：" + ex.ToString());
                Environment.Exit(1);
            }
        }
    }
}