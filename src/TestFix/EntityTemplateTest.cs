using System;
using System.Threading.Tasks;
using FakeMicro.Utilities.CodeGenerator.Models;
using FakeMicro.Utilities.CodeGenerator.Templates;

namespace TestFix
{
    /// <summary>
    /// 实体模板测试类
    /// </summary>
    public static class EntityTemplateTest
    {
        /// <summary>
        /// 测试实体模板生成
        /// </summary>
        public static async Task TestEntityTemplate()
        {
            Console.WriteLine("=== 开始测试实体模板生成 ===");
            
            try
            {
                // 创建测试实体元数据
                var entity = new EntityMetadata
                {
                    EntityName = "Student",
                    EntityDescription = "学生信息",
                    TableName = "students",
                    Namespace = "FakeMicro.Domain",
                    IsSoftDeletable = true
                };
                
                // 添加属性
                entity.Properties.Add(new PropertyMetadata
                {
                    Name = "Id",
                    Type = "long",
                    Description = "学生ID",
                    IsPrimaryKey = true,
                    IsIdentity = true,
                    IsRequired = true
                });
                
                entity.Properties.Add(new PropertyMetadata
                {
                    Name = "Name",
                    Type = "string",
                    Description = "学生姓名",
                    IsRequired = true,
                    MaxLength = 50
                });
                
                entity.Properties.Add(new PropertyMetadata
                {
                    Name = "Age",
                    Type = "int",
                    Description = "学生年龄",
                    IsRequired = true
                });
                
                // 生成实体代码
                var entityCode = EntityTemplate.Generate(entity);
                
                Console.WriteLine("✅ 实体代码生成成功！");
                Console.WriteLine($"代码长度: {entityCode.Length} 字符");
                Console.WriteLine();
                
                // 验证生成的代码
                Console.WriteLine("=== 验证生成的实体代码 ===");
                
                // 验证是否包含正确的 SqlSugar 属性
                if (entityCode.Contains("[SugarColumn(IsOnlyIgnoreUpdate = true)]") && entityCode.Contains("CreatedAt"))
                {
                    Console.WriteLine("✅ CreatedAt 属性正确使用了 IsOnlyIgnoreUpdate");
                }
                else
                {
                    Console.WriteLine("❌ CreatedAt 属性未使用正确的 IsOnlyIgnoreUpdate");
                }
                
                if (entityCode.Contains("[SugarColumn(IsOnlyIgnoreInsert = true)]") && entityCode.Contains("UpdatedAt"))
                {
                    Console.WriteLine("✅ UpdatedAt 属性正确使用了 IsOnlyIgnoreInsert");
                }
                else
                {
                    Console.WriteLine("❌ UpdatedAt 属性未使用正确的 IsOnlyIgnoreInsert");
                }
                
                if (entityCode.Contains("[SugarColumn(IsOnlyIgnoreInsert = true)]") && entityCode.Contains("IsDeleted"))
                {
                    Console.WriteLine("✅ IsDeleted 属性正确使用了 IsOnlyIgnoreInsert");
                }
                else
                {
                    Console.WriteLine("❌ IsDeleted 属性未使用正确的 IsOnlyIgnoreInsert");
                }
                
                // 验证是否移除了错误的属性
                if (!entityCode.Contains("IsOnlyInsert =") && !entityCode.Contains("IsOnlyUpdate =") &&
                    !entityCode.Contains("IsInsert =") && !entityCode.Contains("IsUpdate ="))
                {
                    Console.WriteLine("✅ 已移除所有错误的 SqlSugar 属性");
                }
                else
                {
                    Console.WriteLine("❌ 仍包含错误的 SqlSugar 属性");
                }
                
                // 验证 Orleans 序列化特性
                if (!entityCode.Contains("[GenerateSerializer]"))
                {
                    Console.WriteLine("❌ 缺少 [GenerateSerializer] 特性");
                }
                else
                {
                    Console.WriteLine("✅ 包含 [GenerateSerializer] 特性");
                }
                
                // 验证 Orleans Id 特性
                var idMatches = System.Text.RegularExpressions.Regex.Matches(entityCode, @"\[Id\((\d+)\)\]");
                if (idMatches.Count == 0)
                {
                    Console.WriteLine("❌ 缺少 [Id] 特性");
                }
                else
                {
                    // 验证 Id 序号是否连续且从 0 开始
                    var ids = idMatches.Cast<System.Text.RegularExpressions.Match>().Select(m => int.Parse(m.Groups[1].Value)).OrderBy(x => x).ToList();
                    if (ids.First() != 0)
                    {
                        Console.WriteLine("❌ Id 序号应该从 0 开始");
                    }
                    else if (ids.Select((x, i) => x == i).Any(x => !x))
                    {
                        Console.WriteLine("❌ Id 序号应该连续");
                    }
                    else
                    {
                        Console.WriteLine("✅ Id 特性验证通过");
                    }
                }
                
                Console.WriteLine();
                Console.WriteLine("=== 生成的实体代码预览 ===");
                
                // 显示生成的代码片段
                var lines = entityCode.Split('\n');
                var startIdx = Array.FindIndex(lines, line => line.Contains("CreatedAt"));
                var endIdx = Array.FindIndex(lines, line => line.Contains("IsDeleted"));
                
                if (startIdx >= 0 && endIdx >= 0)
                {
                    for (int i = Math.Max(0, startIdx - 2); i <= Math.Min(lines.Length - 1, endIdx + 2); i++)
                    {
                        Console.WriteLine(lines[i]);
                    }
                }
                
                Console.WriteLine();
                Console.WriteLine("=== 实体模板验证完成 ===");
                
                if (entityCode.Contains("IsOnlyIgnoreInsert") && entityCode.Contains("IsOnlyIgnoreUpdate") && 
                    !entityCode.Contains("IsOnlyInsert =") && !entityCode.Contains("IsOnlyUpdate =") &&
                    !entityCode.Contains("IsInsert =") && !entityCode.Contains("IsUpdate ="))
                {
                    Console.WriteLine("✅ 所有验证项目都通过！");
                }
                else
                {
                    Console.WriteLine("❌ 部分验证项目未通过，请检查生成的代码");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 实体模板测试失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }
    }
}