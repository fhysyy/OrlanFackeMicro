using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FakeMicro.Utilities.CodeGenerator.Entities;

namespace FakeMicro.Utilities.CodeGenerator
{
    public static class RequestTemplateValidation
    {
        public static async Task<bool> ValidateSerializatedIdIncrement()
        {
            Console.WriteLine("=== 验证RequestTemplate序列化ID递增修复效果 ===");
            
            try
            {
                // 创建测试实体
                var entityMetadata = new EntityMetadata
                {
                    EntityName = "TestUser",
                    EntityDescription = "测试用户",
                    Namespace = "FakeMicro.Test",
                    PrimaryKeyProperty = "Id",
                    PrimaryKeyType = "Guid",
                    TableName = "TestUsers",
                    IsAuditable = true,
                    IsSoftDeletable = true,
                    Properties = new List<PropertyMetadata>
                    {
                        new PropertyMetadata 
                        { 
                            Name = "Id", 
                            Type = "Guid", 
                            IsPrimaryKey = true,
                            IsRequired = true,
                            Description = "主键ID"
                        },
                        new PropertyMetadata 
                        { 
                            Name = "UserName", 
                            Type = "string", 
                            IsRequired = true,
                            MaxLength = 50,
                            Description = "用户名"
                        },
                        new PropertyMetadata 
                        { 
                            Name = "Email", 
                            Type = "string", 
                            IsRequired = false,
                            MaxLength = 100,
                            Description = "邮箱"
                        },
                        new PropertyMetadata 
                        { 
                            Name = "FullName", 
                            Type = "string", 
                            IsRequired = false,
                            MaxLength = 100,
                            Description = "全名"
                        },
                        new PropertyMetadata 
                        { 
                            Name = "Age", 
                            Type = "int", 
                            IsRequired = false,
                            Description = "年龄"
                        }
                    }
                };

                // 生成Create请求类代码
                string generatedCode = Templates.RequestTemplate.Generate(entityMetadata);
                
                Console.WriteLine("生成的代码长度: " + generatedCode.Length + " 字符");
                
                // 检查序列化ID是否递增
                var lines = generatedCode.Split('\n');
                var createRequestStart = false;
                int currentId = -1;
                bool hasError = false;
                
                foreach (var line in lines)
                {
                    // 找到Create请求类开始
                    if (line.Contains($"class Create{entityMetadata.EntityName}Request"))
                    {
                        createRequestStart = true;
                        Console.WriteLine("✓ 找到Create请求类");
                        continue;
                    }
                    
                    // 在Create请求类内，检查属性ID
                    if (createRequestStart && line.Contains("[Id("))
                    {
                        // 提取ID值
                        var startIndex = line.IndexOf("[Id(") + 4;
                        var endIndex = line.IndexOf(")]", startIndex);
                        if (startIndex > 3 && endIndex > startIndex)
                        {
                            var idValue = int.Parse(line.Substring(startIndex, endIndex - startIndex));
                            Console.WriteLine($"找到属性ID: {idValue}");
                            
                            // 检查ID是否递增
                            if (idValue <= currentId)
                            {
                                Console.WriteLine($"❌ ID序列错误: 期望 > {currentId}, 实际是 {idValue}");
                                hasError = true;
                            }
                            
                            currentId = idValue;
                        }
                    }
                    
                    // 如果遇到其他类，说明Create请求类结束
                    if (createRequestStart && line.Contains("    }") && currentId > 0)
                    {
                        createRequestStart = false;
                        Console.WriteLine("Create请求类检查完成");
                        break;
                    }
                }
                
                // 验证ID序列的连续性
                Console.WriteLine($"\n最终的ID值: {currentId}");
                Console.WriteLine($"期望的属性数量: {entityMetadata.Properties.Count(p => !p.IsPrimaryKey)}");
                
                if (currentId == entityMetadata.Properties.Count(p => !p.IsPrimaryKey) - 1)
                {
                    Console.WriteLine("✅ 序列化ID递增修复成功！");
                    Console.WriteLine($"✓ 从0开始到{currentId}，共{currentId + 1}个属性");
                    return true;
                }
                else
                {
                    Console.WriteLine($"❌ 序列化ID递增验证失败！");
                    Console.WriteLine($"期望: 0-{entityMetadata.Properties.Count(p => !p.IsPrimaryKey) - 1}");
                    Console.WriteLine($"实际: 0-{currentId}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 验证过程出错: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                return false;
            }
        }
        
        public static async Task<bool> ValidateRepositoryPagedMethods()
        {
            Console.WriteLine("\n=== 验证Repository实现模板分页方法修复 ===");
            
            try
            {
                // 创建测试实体
                var entityMetadata = new EntityMetadata
                {
                    EntityName = "Product",
                    EntityDescription = "产品",
                    Namespace = "FakeMicro.Entities",
                    PrimaryKeyProperty = "Id",
                    PrimaryKeyType = "int",
                    TableName = "Products",
                    IsAuditable = true,
                    IsSoftDeletable = true,
                    Properties = new List<PropertyMetadata>
                    {
                        new PropertyMetadata { Name = "Id", Type = "int", IsPrimaryKey = true, IsRequired = true },
                        new PropertyMetadata { Name = "Name", Type = "string", IsRequired = true, MaxLength = 200 },
                        new PropertyMetadata { Name = "Price", Type = "decimal", IsRequired = true }
                    }
                };

                // 生成Repository实现类代码
                string generatedCode = Templates.RepositoryImplementationTemplate.Generate(entityMetadata);
                
                Console.WriteLine("生成的Repository实现代码长度: " + generatedCode.Length + " 字符");
                
                // 检查分页方法
                bool hasPagedResultReturn = generatedCode.Contains("Task<PagedResult<" + entityMetadata.EntityName + ">> GetPagedAsync");
                bool hasConditionPagedResultReturn = generatedCode.Contains("Task<PagedResult<" + entityMetadata.EntityName + ">> GetPagedByConditionAsync");
                bool hasUpdateAndReturn = generatedCode.Contains("UpdateAndReturnAsync");
                bool hasCreateRangeAndReturn = generatedCode.Contains("CreateRangeAndReturnAsync");
                
                Console.WriteLine($"✓ 包含GetPagedAsync方法: {hasPagedResultReturn}");
                Console.WriteLine($"✓ 包含GetPagedByConditionAsync方法: {hasConditionPagedResultReturn}");
                Console.WriteLine($"✓ 包含UpdateAndReturnAsync方法: {hasUpdateAndReturn}");
                Console.WriteLine($"✓ 包含CreateRangeAndReturnAsync方法: {hasCreateRangeAndReturn}");
                
                if (hasPagedResultReturn && hasConditionPagedResultReturn && hasUpdateAndReturn && hasCreateRangeAndReturn)
                {
                    Console.WriteLine("✅ Repository实现模板分页方法修复成功！");
                    return true;
                }
                else
                {
                    Console.WriteLine("❌ Repository实现模板修复不完整");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Repository实现验证出错: {ex.Message}");
                return false;
            }
        }
        
        public static async Task<bool> RunAllValidations()
        {
            Console.WriteLine("=== 开始完整验证所有修复 ===");
            Console.WriteLine($"验证时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();
            
            bool requestValidation = await ValidateSerializatedIdIncrement();
            bool repositoryValidation = await ValidateRepositoryPagedMethods();
            
            Console.WriteLine("\n=== 验证结果汇总 ===");
            Console.WriteLine($"RequestTemplate序列化ID修复: {(requestValidation ? "✅ 通过" : "❌ 失败")}");
            Console.WriteLine($"RepositoryImplementationTemplate分页方法修复: {(repositoryValidation ? "✅ 通过" : "❌ 失败")}");
            
            bool allPassed = requestValidation && repositoryValidation;
            Console.WriteLine($"\n总体结果: {(allPassed ? "✅ 所有修复验证通过" : "❌ 存在失败的修复项")}");
            
            return allPassed;
        }
    }
}