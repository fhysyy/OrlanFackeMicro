using System;
using System.Threading.Tasks;

namespace FakeMicro.Utilities
{
    /// <summary>
    /// 示例测试类 - 演示测试框架的使用
    /// </summary>
    public class ExampleTests : TestBase
    {
        /// <summary>
        /// 测试字符串扩展方法
        /// </summary>
        public void TestStringExtensions()
        {
            Context.Log("开始测试字符串扩展方法");
            
            // 测试IsNullOrEmpty扩展
            string? nullString = null;
            AssertTrue(nullString.IsNullOrEmpty(), "null字符串应该为空");
            
            string emptyString = "";
            AssertTrue(emptyString.IsNullOrEmpty(), "空字符串应该为空");
            
            string validString = "Hello";
            AssertFalse(validString.IsNullOrEmpty(), "有效字符串不应该为空");
            
            // 测试ToCamelCase扩展
            string pascalCase = "HelloWorld";
            string camelCase = pascalCase.ToCamelCase();
            AssertEqual("helloWorld", camelCase, "PascalCase应该转换为camelCase");
            
            Context.Log("字符串扩展方法测试完成");
        }
        
        /// <summary>
        /// 测试验证助手
        /// </summary>
        public void TestValidationHelper()
        {
            Context.Log("开始测试验证助手");
            
            // 测试邮箱验证
            AssertTrue(ValidationHelper.IsValidEmail("test@example.com"), "有效邮箱应该通过验证");
            AssertFalse(ValidationHelper.IsValidEmail("invalid-email"), "无效邮箱应该失败");
            
            // 测试手机号验证
            AssertTrue(ValidationHelper.IsValidChineseMobile("13812345678"), "有效手机号应该通过验证");
            AssertFalse(ValidationHelper.IsValidChineseMobile("12345678901"), "无效手机号应该失败");
            
            // 测试密码强度
            var weakPassword = ValidationHelper.ValidatePasswordStrength("123");
            AssertEqual(PasswordStrength.Weak, weakPassword, "弱密码应该被识别");
            
            var strongPassword = ValidationHelper.ValidatePasswordStrength("Abc123!@#");
            AssertEqual(PasswordStrength.Strong, strongPassword, "强密码应该被识别");
            
            Context.Log("验证助手测试完成");
        }
        
        /// <summary>
        /// 测试性能监控
        /// </summary>
        public void TestPerformanceMonitor()
        {
            Context.Log("开始测试性能监控");
            
            using (var scope = PerformanceMonitor.CreateScope("TestOperation"))
            {
                // 模拟一些操作
                System.Threading.Thread.Sleep(100);
                
                // 记录操作性能
                PerformanceMonitor.RecordOperation("TestOperation", 150.5);
            }
            
            var counters = PerformanceMonitor.GetAllCounters();
            AssertTrue(counters.ContainsKey("TestOperation"), "性能计数器应该包含测试操作");
            
            Context.Log("性能监控测试完成");
        }
        
        /// <summary>
        /// 测试异步操作
        /// </summary>
        public async Task TestAsyncOperations()
        {
            Context.Log("开始测试异步操作");
            
            await Task.Delay(50);
            
            AssertTrue(true, "异步操作应该成功完成");
            
            Context.Log("异步操作测试完成");
        }
        
        /// <summary>
        /// 测试异常处理
        /// </summary>
        public void TestExceptionHandling()
        {
            Context.Log("开始测试异常处理");
            
            // 测试断言抛出异常
            AssertThrows<InvalidOperationException>(() => 
            {
                throw new InvalidOperationException("测试异常");
            }, "应该捕获InvalidOperationException");
            
            Context.Log("异常处理测试完成");
        }
        
        /// <summary>
        /// 测试前的设置
        /// </summary>
        internal override void Setup()
        {
            Context.Log("测试设置完成");
            Context.SetTestData("TestValue", 42);
        }
        
        /// <summary>
        /// 测试后的清理
        /// </summary>
        internal override void Teardown()
        {
            Context.Log("测试清理完成");
        }
    }
    
    /// <summary>
    /// 集成测试示例
    /// </summary>
    public class IntegrationTests : TestBase
    {
        [IntegrationTest]
        public void TestDatabaseOperations()
        {
            Context.Log("开始集成测试");
            
            // 这里可以模拟数据库操作测试
            AssertTrue(true, "集成测试应该通过");
            
            Context.Log("集成测试完成");
        }
        
        [PerformanceTest(Iterations = 100)]
        public void TestPerformance()
        {
            Context.Log("开始性能测试");
            
            // 性能测试逻辑
            for (int i = 0; i < 1000; i++)
            {
                var result = i * i;
            }
            
            AssertTrue(true, "性能测试应该通过");
            
            Context.Log("性能测试完成");
        }
    }
}