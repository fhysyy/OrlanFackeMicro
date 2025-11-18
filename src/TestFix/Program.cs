using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeMicro.Utilities.CodeGenerator;
using FakeMicro.Utilities.CodeGenerator.Models;
using FakeMicro.Utilities.CodeGenerator.Templates;

namespace TestFix
{
    class Program
    {
        static async Task Main(string[] args)
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
                
                Console.WriteLine("✅ PropertyMetadata 创建成功！");
                Console.WriteLine($"   - IsIdentity: {property.IsIdentity}");
                Console.WriteLine($"   - ColumnName: {property.ColumnName}");
                Console.WriteLine($"   - DefaultValue: {property.DefaultValue}");
                
                // 创建测试实体
                var entity = new EntityMetadata
                {
                    EntityName = "TestEntity",
                    EntityDescription = "测试实体",
                    TableName = "test_entities",
                    Namespace = "FakeMicro.Models",
                    Properties = new List<PropertyMetadata> { property }
                };
                
                Console.WriteLine("✅ EntityMetadata 创建成功！");
                
                Console.WriteLine("\n🎉 PropertyMetadata 修复验证成功！所有新增属性都可以正常访问。");
                
                // 运行实体模板验证测试
                Console.WriteLine("\n" + new string('=', 50));
                await EntityTemplateTest.TestEntityTemplate();
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