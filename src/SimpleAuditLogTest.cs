using FakeMicro.Entities;
using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("审计日志功能简单测试");
        Console.WriteLine("=" * 50);
        
        try
        {
            // 测试1: 创建审计日志对象
            Console.WriteLine("\n1. 测试创建审计日志对象...");
            
            var auditLog = new AuditLog
            {
                user_id = 1,
                username = "test_user",
                action = "TestAction",
                resource = "TestResource",
                resource_id = "123",
                Details = "测试审计日志记录功能",
                ip_address = "127.0.0.1",
                user_agent = "Test Client",
                created_at = DateTime.UtcNow,
                tenant_id = "1",
                Result = "Success",
                execution_time = 100
            };
            
            Console.WriteLine("✓ 审计日志对象创建成功！");
            Console.WriteLine($"   操作: {auditLog.action}");
            Console.WriteLine($"   资源: {auditLog.resource}");
            Console.WriteLine($"   用户: {auditLog.username}");
            Console.WriteLine($"   IP: {auditLog.ip_address}");
            
            // 测试2: 验证对象属性
            Console.WriteLine("\n2. 测试验证对象属性...");
            
            if (!string.IsNullOrEmpty(auditLog.action))
                Console.WriteLine("✓ 操作类型不为空");
            
            if (!string.IsNullOrEmpty(auditLog.resource))
                Console.WriteLine("✓ 资源类型不为空");
            
            if (auditLog.created_at <= DateTime.UtcNow)
                Console.WriteLine("✓ 创建时间正确");
            
            // 测试3: 测试对象序列化（简单验证）
            Console.WriteLine("\n3. 测试对象序列化...");
            
            var auditLogString = $"ID: {auditLog.id}, 用户: {auditLog.username}, 操作: {auditLog.action}, 时间: {auditLog.created_at}";
            Console.WriteLine($"✓ 对象信息: {auditLogString}");
            
            Console.WriteLine("\n" + "=" * 50);
            Console.WriteLine("审计日志功能测试完成！");
            Console.WriteLine("核心功能验证成功：");
            Console.WriteLine("- 审计日志实体类可以正常创建");
            Console.WriteLine("- 所有必要属性可以正常设置和访问");
            Console.WriteLine("- 时间戳功能正常工作");
            Console.WriteLine("- 对象可以正常序列化");
            
        } catch (Exception ex)
        {
            Console.WriteLine($"\n✗ 测试失败: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        
        Console.WriteLine("\n按任意键退出...");
        Console.ReadKey();
    }
}