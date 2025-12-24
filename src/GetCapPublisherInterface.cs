using System;
using System.Reflection;
using DotNetCore.CAP;

namespace GetCapPublisherInterface
{
    class Program
    {
        static void Main(string[] args)
        {
            // 获取ICapPublisher接口的类型信息
            var capPublisherType = typeof(ICapPublisher);
            Console.WriteLine("ICapPublisher接口的属性:");
            
            // 输出所有属性
            foreach (var prop in capPublisherType.GetProperties())
            {
                Console.WriteLine($"  {prop.Name}: {prop.PropertyType.FullName} (可写: {prop.CanWrite})");
            }
            
            Console.WriteLine("\nICapPublisher接口的方法:");
            
            // 输出所有方法
            foreach (var method in capPublisherType.GetMethods())
            {
                var parameters = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.FullName} {p.Name}"));
                Console.WriteLine($"  {method.ReturnType.FullName} {method.Name}<{string.Join(",", method.GetGenericArguments().Select(g => g.Name))}>({parameters})");
            }
        }
    }
}