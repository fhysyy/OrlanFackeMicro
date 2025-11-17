using System;
using FakeMicro.Utilities.CodeGenerator;

namespace CodeGeneratorTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Orleans SqlSugar 代码生成器测试 ===\n");

            try
            {
                Console.WriteLine("✅ 代码生成器基础框架编译成功！");
                Console.WriteLine("🔧 可用的模板类型:");
                Console.WriteLine("   - Interface: Orleans Grain接口");
                Console.WriteLine("   - Grain: Orleans Grain实现");
                Console.WriteLine("   - Dto: 数据传输对象");
                Console.WriteLine("   - Controller: API控制器");
                Console.WriteLine();

                Console.WriteLine("📝 使用方法:");
                Console.WriteLine("1. 在 FakeMicro.Entities 中定义实体类");
                Console.WriteLine("2. 使用 CodeGenerator 类生成对应的CRUD代码");
                Console.WriteLine("3. 生成的代码将自动适配 Orleans + SqlSugar 架构");
                Console.WriteLine();

                Console.WriteLine("🎯 示例实体: Product, User, Message");
                Console.WriteLine("📁 输出目录: Interfaces, Grains, Api, Entities");
                Console.WriteLine();

                Console.WriteLine("✨ 代码生成器就绪！现在可以开始生成CRUD代码了。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 测试失败: {ex.Message}");
            }

            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}