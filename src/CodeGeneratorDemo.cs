using System;
using System.Threading.Tasks;
using FakeMicro.Utilities.CodeGenerator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace CodeGeneratorDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Orleans SqlSugar 代码生成器演示");
            Console.WriteLine("=" + new string('=', 50));
            Console.WriteLine();

            try
            {
                // 设置服务容器
                var services = new ServiceCollection();
                
                // 添加配置
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.codegen.json", optional: true)
                    .Build();

                services.AddSingleton<IConfiguration>(configuration);

                // 添加代码生成器服务
                services.AddCodeGenerator();

                var serviceProvider = services.BuildServiceProvider();
                var codeGenerator = serviceProvider.GetRequiredService<global::FakeMicro.Utilities.CodeGenerator.CodeGenerator>();

                // 演示功能
                await DemoListEntities(codeGenerator);
                await DemoGenerateCode(codeGenerator);
                
                Console.WriteLine();
                Console.WriteLine("✨ 代码生成器演示完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 演示失败: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
            }

            Console.WriteLine();
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        /// <summary>
        /// 演示列出实体类型
        /// </summary>
        static async Task DemoListEntities(global::FakeMicro.Utilities.CodeGenerator.CodeGenerator codeGenerator)
        {
            Console.WriteLine("📋 可用的实体类型:");
            Console.WriteLine("-" + new string('-', 30));

            var entities = codeGenerator.GetAvailableEntityTypes();
            
            foreach (var entity in entities)
            {
                Console.WriteLine($"   🏗️  {entity.Name}");
            }

            Console.WriteLine($"   总计: {entities.Count()} 个实体类");
            Console.WriteLine();
        }

        /// <summary>
        /// 演示生成代码
        /// </summary>
        static async Task DemoGenerateCode(global::FakeMicro.Utilities.CodeGenerator.CodeGenerator codeGenerator)
        {
            Console.WriteLine("🔧 代码生成演示:");
            Console.WriteLine("-" + new string('-', 30));

            var entityName = "Product";
            var types = GenerationType.Interface | GenerationType.Grain | GenerationType.Dto;

            Console.WriteLine($"🎯 目标实体: {entityName}");
            Console.WriteLine($"📝 生成类型: {types}");
            Console.WriteLine();

            try
            {
                Console.WriteLine("⚡ 正在生成代码...");
                var result = await codeGenerator.GenerateCodeAsync(entityName, types);

                if (result.IsSuccess)
                {
                    Console.WriteLine("✅ 代码生成成功！");
                    Console.WriteLine($"📁 生成的文件数量: {result.GeneratedFiles.Count}");
                    Console.WriteLine();

                    foreach (var file in result.GeneratedFiles)
                    {
                        var fileName = System.IO.Path.GetFileName(file);
                        var size = new System.IO.FileInfo(file).Length;
                        Console.WriteLine($"   📄 {fileName} ({size} bytes)");
                    }

                    if (result.Warnings.Count > 0)
                    {
                        Console.WriteLine();
                        Console.WriteLine("⚠️ 警告:");
                        foreach (var warning in result.Warnings)
                        {
                            Console.WriteLine($"   {warning}");
                        }
                    }

                    Console.WriteLine();
                    Console.WriteLine("💡 提示: 生成的代码遵循 Orleans + SqlSugar 最佳实践");
                    Console.WriteLine("   - 使用异步编程模式");
                    Console.WriteLine("   - 集成 SqlSugar 仓储模式");
                    Console.WriteLine("   - 支持依赖注入");
                    Console.WriteLine("   - 包含完整的 CRUD 操作");
                }
                else
                {
                    Console.WriteLine($"❌ 代码生成失败: {result.ErrorMessage}");
                    Console.WriteLine($"🔍 错误类型: {result.ErrorType}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 生成过程中发生异常: {ex.Message}");
            }

            Console.WriteLine();
        }
    }
}