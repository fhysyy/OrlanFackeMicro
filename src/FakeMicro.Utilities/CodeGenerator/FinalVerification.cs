using System;
using System.Threading.Tasks;
using FakeMicro.Utilities.CodeGenerator;

class FinalVerification
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== 最终验证代码生成器修复效果 ===");
        Console.WriteLine();
        
        try
        {
            // 验证1: 序列化ID递增修复
            Console.WriteLine("1. 验证序列化ID递增修复...");
            bool idValidation = await RequestTemplateValidation.ValidateSerializatedIdIncrement();
            
            // 验证2: Repository分页方法修复
            Console.WriteLine("\n2. 验证Repository分页方法修复...");
            bool pagedValidation = await RequestTemplateValidation.ValidateRepositoryPagedMethods();
            
            // 最终结果
            Console.WriteLine("\n=== 最终验证结果 ===");
            Console.WriteLine($"序列化ID递增修复: {(idValidation ? "✅ 成功" : "❌ 失败")}");
            Console.WriteLine($"Repository分页方法修复: {(pagedValidation ? "✅ 成功" : "❌ 失败")}");
            
            if (idValidation && pagedValidation)
            {
                Console.WriteLine("\n🎉 所有修复验证成功！代码生成器现在正常工作。");
            }
            else
            {
                Console.WriteLine("\n⚠️  部分修复需要进一步处理。");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 验证过程出错: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
        }
        
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
}