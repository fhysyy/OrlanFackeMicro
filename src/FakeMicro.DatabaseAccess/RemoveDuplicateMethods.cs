using System;
using System.IO;

namespace FakeMicro.DatabaseAccess
{
    class Program
    {
        static void Main()
        {
            string filePath = "F:\\Orleans\\OrlanFackeMicro\\src\\FakeMicro.DatabaseAccess\\DynamicRepositoryFactory.cs";
            string[] lines = File.ReadAllLines(filePath);
            
            // 创建一个新的数组来存储结果
            string[] newLines = new string[lines.Length];
            int newIndex = 0;
            bool skipMethod = false;
            
            // 遍历所有行
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                
                // 如果遇到了重复的方法定义开始，开始跳过
                if (line.Contains("public IRepository<TEntity, TKey> CreateRepository<TEntity, TKey>(TKey key)"))
                {
                    skipMethod = true;
                }
                
                // 如果没有在跳过模式中，就添加到新数组
                if (!skipMethod)
                {
                    newLines[newIndex] = line;
                    newIndex++;
                }
                
                // 如果遇到了下一个方法的开始（以/// <summary>开头），并且在跳过模式中，就结束跳过
                if (skipMethod && line.StartsWith("/// <summary>") && i > 160)
                {
                    skipMethod = false;
                    newLines[newIndex] = line;
                    newIndex++;
                }
            }
            
            // 调整数组大小并写入文件
            Array.Resize(ref newLines, newIndex);
            File.WriteAllLines(filePath, newLines);
            
            Console.WriteLine("重复方法已删除！");
        }
    }
}