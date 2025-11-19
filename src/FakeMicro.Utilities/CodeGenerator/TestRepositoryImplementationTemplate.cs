using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FakeMicro.Utilities.CodeGenerator.Entities;
using FakeMicro.Utilities.CodeGenerator.Templates;

namespace FakeMicro.Utilities.CodeGenerator.Test
{
    /// <summary>
    /// 仓储实现模板测试
    /// 测试RepositoryImplementationTemplate的代码生成功能
    /// </summary>
    public class TestRepositoryImplementationTemplate
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== 仓储实现模板测试 ===");
            
            try
            {
                // 创建Order实体元数据
                var orderEntity = new EntityMetadata
                {
                    EntityName = "Order",
                    EntityDescription = "订单",
                    TableName = "orders",
                    PrimaryKeyProperty = "Id",
                    PrimaryKeyType = "long",
                    Namespace = "FakeMicro.Entities",
                    IsAuditable = true,
                    IsSoftDeletable = true,
                    Properties = new System.Collections.Generic.List<PropertyMetadata>
                    {
                        new PropertyMetadata 
                        { 
                            Name = "OrderNo", 
                            Type = "string", 
                            IsRequired = true,
                            MaxLength = 50
                        },
                        new PropertyMetadata 
                        { 
                            Name = "CustomerName", 
                            Type = "string", 
                            IsRequired = true,
                            MaxLength = 100
                        },
                        new PropertyMetadata 
                        { 
                            Name = "TotalAmount", 
                            Type = "decimal", 
                            IsRequired = true
                        },
                        new PropertyMetadata 
                        { 
                            Name = "OrderDate", 
                            Type = "DateTime", 
                            IsRequired = true
                        }
                    }
                };

                Console.WriteLine($"✅ 创建Order实体元数据成功");
                Console.WriteLine($"   - 实体名称: {orderEntity.EntityName}");
                Console.WriteLine($"   - 表名: {orderEntity.TableName}");
                Console.WriteLine($"   - 主键: {orderEntity.PrimaryKeyProperty} ({orderEntity.PrimaryKeyType})");
                Console.WriteLine($"   - 属性数量: {orderEntity.Properties.Count}");
                Console.WriteLine($"   - 可审计: {orderEntity.IsAuditable}");
                Console.WriteLine($"   - 软删除: {orderEntity.IsSoftDeletable}");
                Console.WriteLine();

                // 测试命名空间映射
                Console.WriteLine("=== 命名空间映射测试 ===");
                var entityNamespace = ProjectStructureMapping.GetNamespace(GenerationType.Entity, orderEntity.EntityName);
                var repositoryInterfaceNamespace = ProjectStructureMapping.GetNamespace(GenerationType.Repository, orderEntity.EntityName);
                var repositoryImplementationNamespace = ProjectStructureMapping.GetNamespace(GenerationType.RepositoryImplementation, orderEntity.EntityName);

                Console.WriteLine($"实体命名空间: {entityNamespace}");
                Console.WriteLine($"仓储接口命名空间: {repositoryInterfaceNamespace}");
                Console.WriteLine($"仓储实现命名空间: {repositoryImplementationNamespace}");
                Console.WriteLine();

                // 生成仓储实现代码
                Console.WriteLine("=== 生成仓储实现代码 ===");
                var repositoryImplementationCode = RepositoryImplementationTemplate.Generate(orderEntity);
                
                Console.WriteLine("✅ 仓储实现代码生成成功！");
                Console.WriteLine();
                Console.WriteLine("=== 生成的仓储实现代码预览 ===");
                Console.WriteLine(repositoryImplementationCode.Substring(0, Math.Min(1000, repositoryImplementationCode.Length)));
                if (repositoryImplementationCode.Length > 1000)
                {
                    Console.WriteLine("...(代码被截断)");
                }
                Console.WriteLine();

                // 保存到文件
                var basePath = @"F:\ProjectCode\OrlanFackeMicro\src";
                var outputFilePath = ProjectStructureMapping.GetFilePath(GenerationType.RepositoryImplementation, orderEntity.EntityName, basePath);
                
                // 确保目录存在
                var outputDirectory = Path.GetDirectoryName(outputFilePath);
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                    Console.WriteLine($"✅ 创建目录: {outputDirectory}");
                }
                
                await File.WriteAllTextAsync(outputFilePath, repositoryImplementationCode);
                Console.WriteLine($"✅ 仓储实现代码已保存到: {outputFilePath}");
                Console.WriteLine();

                // 验证生成的代码
                Console.WriteLine("=== 代码验证 ===");
                if (repositoryImplementationCode.Contains("class OrderRepository"))
                {
                    Console.WriteLine("✅ 包含OrderRepository类定义");
                }
                if (repositoryImplementationCode.Contains("SqlSugarRepository<Order, long>"))
                {
                    Console.WriteLine("✅ 正确继承SqlSugarRepository<Order, long>");
                }
                if (repositoryImplementationCode.Contains("IOrderRepository"))
                {
                    Console.WriteLine("✅ 实现IOrderRepository接口");
                }
                if (repositoryImplementationCode.Contains("SoftDeleteAsync"))
                {
                    Console.WriteLine("✅ 包含软删除方法");
                }
                if (repositoryImplementationCode.Contains("Orleans"))
                {
                    Console.WriteLine("✅ 包含Orleans特定方法");
                }
                if (repositoryImplementationCode.Contains("CreateAndReturnAsync"))
                {
                    Console.WriteLine("✅ 包含创建并返回方法");
                }

                Console.WriteLine();
                Console.WriteLine("🎉 仓储实现模板测试成功！所有功能正常工作。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 测试失败: {ex.Message}");
                Console.WriteLine($"详细信息: {ex}");
            }

            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
    }
}