using System;
using System.Threading.Tasks;
using FakeMicro.Utilities.CodeGenerator.Verification;

namespace FakeMicro.Utilities.CodeGenerator
{
    /// <summary>
    /// 验证脚本主程序
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== 代码生成器仓储功能验证程序 ===");
            Console.WriteLine($"运行时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            try
            {
                // 运行验证测试
                bool success = await SimpleVerification.TestRepositoryGeneration();
                
                if (success)
                {
                    Console.WriteLine("\n🎉 验证成功！仓储接口和实现生成功能已修复。");
                    
                    // 显示生成的代码示例
                    Console.WriteLine();
                    await SimpleVerification.ShowGeneratedCode();
                }
                else
                {
                    Console.WriteLine("\n❌ 验证失败！仍存在问题需要解决。");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ 程序运行出错: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            }
            
            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }
    }
}