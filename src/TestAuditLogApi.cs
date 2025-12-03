using System;using System.Net.Http;using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // 启动API服务器
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --project FakeMicro.Api/FakeMicro.Api.csproj",
            WorkingDirectory = "F:\\ProjectCode\\OrlanFackeMicro\\src",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        var apiProcess = System.Diagnostics.Process.Start(startInfo);
        
        Console.WriteLine("正在启动API服务器...");
        await Task.Delay(5000); // 等待服务器启动
        
        try
        {
            // 创建HTTP客户端
            using var client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:5000/");
            client.DefaultRequestHeaders.Add("User-Agent", "TestClient");
            
            // 尝试调用一个简单的API来触发审计日志
            Console.WriteLine("尝试调用API来触发审计日志...");
            var response = await client.GetAsync("api/health");
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("API调用成功！审计日志应该已记录");
            }
            else
            {
                Console.WriteLine($"API调用失败: {response.StatusCode}");
            }
            
        } catch (Exception ex)
        {
            Console.WriteLine($"发生错误: {ex.Message}");
        } finally {
            // 关闭API服务器
            if (apiProcess != null && !apiProcess.HasExited)
            {
                apiProcess.Kill();
            }
        }
    }
}