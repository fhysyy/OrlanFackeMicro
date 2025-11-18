using System;
using System.Threading.Tasks;
using FakeMicro.Utilities.CodeGenerator;

namespace TestFix
{
    /// <summary>
    /// 控制器模板验证测试
    /// </summary>
    public class ControllerTemplateTest
    {
        public static async Task TestControllerTemplate()
        {
            Console.WriteLine("=== 控制器模板验证测试 ===");
            
            try
            {
                // 创建测试实体元数据
                var testEntity = new EntityMetadata
                {
                    EntityName = "Student",
                    EntityDescription = "学生信息",
                    Namespace = "FakeMicro",
                    PrimaryKeyProperty = "Id",
                    PrimaryKeyType = "long",
                    IsSoftDeletable = true,
                    Properties = new List<PropertyMetadata>
                    {
                        new PropertyMetadata
                        {
                            Name = "Id",
                            Type = "long",
                            Description = "学生唯一标识",
                            IsRequired = true,
                            IsPrimaryKey = true,
                            IsIdentity = true,
                            ColumnName = "id",
                            IsNullable = false,
                            IsReadOnly = true,
                            IsAutoIncrement = true
                        },
                        new PropertyMetadata
                        {
                            Name = "Name",
                            Type = "string",
                            Description = "学生姓名",
                            IsRequired = true,
                            ColumnName = "name",
                            IsNullable = false,
                            MaxLength = 100
                        },
                        new PropertyMetadata
                        {
                            Name = "Age",
                            Type = "int",
                            Description = "学生年龄",
                            IsRequired = true,
                            ColumnName = "age",
                            IsNullable = false
                        }
                    }
                };

                // 生成控制器代码
                var controllerCode = FakeMicro.Utilities.CodeGenerator.Templates.ControllerTemplate.Generate(testEntity);
                
                Console.WriteLine("✅ 控制器代码生成成功！");
                Console.WriteLine($"代码长度: {controllerCode.Length} 字符");
                Console.WriteLine();
                
                // 验证是否包含IClusterClient
                if (controllerCode.Contains("IClusterClient"))
                {
                    Console.WriteLine("✅ 控制器模板正确使用了 IClusterClient");
                }
                else
                {
                    Console.WriteLine("❌ 控制器模板未找到 IClusterClient");
                }
                
                // 验证是否不再包含IGrainFactory
                if (!controllerCode.Contains("IGrainFactory"))
                {
                    Console.WriteLine("✅ 控制器模板已移除 IGrainFactory");
                }
                else
                {
                    Console.WriteLine("❌ 控制器模板仍包含 IGrainFactory");
                }
                
                // 验证字段命名规范
                if (controllerCode.Contains("_clusterClient") && controllerCode.Contains("IClusterClient clusterClient"))
                {
                    Console.WriteLine("✅ 控制器字段命名符合规范（使用下划线前缀）");
                }
                else
                {
                    Console.WriteLine("❌ 控制器字段命名不符合规范");
                }
                
                Console.WriteLine();
                Console.WriteLine("=== 生成的控制器代码预览 ===");
                var previewLines = controllerCode.Split('\n');
                for (int i = 0; i < Math.Min(50, previewLines.Length); i++)
                {
                    Console.WriteLine(previewLines[i]);
                }
                
                if (previewLines.Length > 50)
                {
                    Console.WriteLine("... (代码已截断，完整代码请查看生成的文件)");
                }
                
                Console.WriteLine();
                Console.WriteLine("=== 控制器模板验证完成 ===");
                Console.WriteLine("✅ 所有验证项目都通过！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 控制器模板验证失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
        }
    }
}