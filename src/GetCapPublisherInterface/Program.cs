using System;
using System.Reflection;
using System.Text;
using DotNetCore.CAP;

namespace GetCapPublisherInterface
{
    class Program
    {
        static void Main(string[] args)
        {
            // 获取ICapPublisher接口的类型信息
            var capPublisherType = typeof(ICapPublisher);
            var sb = new StringBuilder();
            
            sb.AppendLine("// ICapPublisher接口完整定义");
            sb.AppendLine("public interface ICapPublisher");
            sb.AppendLine("{");
            
            // 添加属性
            foreach (var prop in capPublisherType.GetProperties())
            {
                var accessModifier = prop.CanWrite ? "{ get; set; }" : "{ get; }";
                sb.AppendLine($"    {prop.PropertyType.FullName} {prop.Name} {accessModifier};");
            }
            
            sb.AppendLine();
            
            // 添加方法
            foreach (var method in capPublisherType.GetMethods())
            {
                // 跳过Object类的方法
                if (method.DeclaringType == typeof(object)) continue;
                
                var genericParams = method.IsGenericMethodDefinition ? $"<{string.Join(", ", method.GetGenericArguments().Select(g => g.Name))}>" : "";
                var parameters = new StringBuilder();
                
                foreach (var param in method.GetParameters())
                {
                    var paramType = param.ParameterType;
                    string paramTypeName;
                    
                    if (paramType.IsGenericType)
                    {
                        var genericArgs = string.Join(", ", paramType.GetGenericArguments().Select(g => g.FullName));
                        paramTypeName = $"{paramType.GetGenericTypeDefinition().FullName}<{genericArgs}>";
                    }
                    else
                    {
                        paramTypeName = paramType.FullName;
                    }
                    
                    if (parameters.Length > 0) parameters.Append(", ");
                    parameters.Append($"{paramTypeName} {param.Name}");
                }
                
                var returnType = method.ReturnType.FullName;
                sb.AppendLine($"    {returnType} {method.Name}{genericParams}({parameters});");
            }
            
            sb.AppendLine("}");
            
            // 输出到控制台
            Console.WriteLine(sb.ToString());
            
            // 同时保存到文件
            System.IO.File.WriteAllText("ICapPublisher_FullDefinition.txt", sb.ToString());
            Console.WriteLine("\n接口定义已保存到ICapPublisher_FullDefinition.txt文件");
        }
    }
}