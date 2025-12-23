using System;
using DotNetCore.CAP;

public class TestCapPublisher
{
    public static void Main()
    {
        // 获取ICapPublisher接口的类型信息
        var capPublisherType = typeof(ICapPublisher);
        Console.WriteLine("ICapPublisher接口的属性:");
        
        // 输出所有属性
        foreach (var prop in capPublisherType.GetProperties())
        {
            Console.WriteLine($"  {prop.Name}: {prop.PropertyType.FullName}");
        }
        
        Console.WriteLine("\nICapPublisher接口的方法:");
        
        // 输出所有方法
        foreach (var method in capPublisherType.GetMethods())
        {
            Console.WriteLine($"  {method.Name}: {method.ReturnType.FullName}");
        }
    }
}