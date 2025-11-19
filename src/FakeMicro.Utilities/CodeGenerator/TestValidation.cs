using FakeMicro.Utilities.CodeGenerator;
using System;
using System.Threading.Tasks;

class TestValidation
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== 开始验证代码生成器修复效果 ===");
        
        try
        {
            var result = await RequestTemplateValidation.RunAllValidations();
            Console.WriteLine($"\n验证完成，结果: {(result ? "成功" : "失败")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"验证过程出错: {ex.Message}");
        }
        
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
    }
}