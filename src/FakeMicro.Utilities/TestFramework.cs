using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace FakeMicro.Utilities
{
    /// <summary>
    /// 测试框架基础类
    /// </summary>
    public abstract class TestBase
    {
        internal TestContext Context { get; private set; } = new TestContext();
        
        /// <summary>
        /// 测试前执行
        /// </summary>
        internal virtual void Setup() { }
        
        /// <summary>
        /// 测试后执行
        /// </summary>
        internal virtual void Teardown() { }
        
        /// <summary>
        /// 断言相等
        /// </summary>
        internal void AssertEqual<T>(T expected, T actual, string? message = null)
        {
            if (!Equals(expected, actual))
            {
                throw new AssertionException($"Expected: {expected}, Actual: {actual}. {message}");
            }
        }
        
        /// <summary>
        /// 断言为真
        /// </summary>
        internal void AssertTrue(bool condition, string? message = null)
        {
            if (!condition)
            {
                throw new AssertionException($"Expected true, but got false. {message}");
            }
        }
        
        /// <summary>
        /// 断言为假
        /// </summary>
        internal void AssertFalse(bool condition, string? message = null)
        {
            if (condition)
            {
                throw new AssertionException($"Expected false, but got true. {message}");
            }
        }
        
        /// <summary>
        /// 断言为空
        /// </summary>
        internal void AssertNull(object? obj, string? message = null)
        {
            if (obj != null)
            {
                throw new AssertionException($"Expected null, but got {obj}. {message}");
            }
        }
        
        /// <summary>
        /// 断言不为空
        /// </summary>
        internal void AssertNotNull(object? obj, string? message = null)
        {
            if (obj == null)
            {
                throw new AssertionException($"Expected not null, but got null. {message}");
            }
        }
        
        /// <summary>
        /// 断言抛出异常
        /// </summary>
        internal void AssertThrows<TException>(Action action, string? message = null) where TException : Exception
        {
            try
            {
                action();
                throw new AssertionException($"Expected {typeof(TException).Name}, but no exception was thrown. {message}");
            }
            catch (TException)
            {
                // 期望的异常，测试通过
            }
            catch (Exception ex)
            {
                throw new AssertionException($"Expected {typeof(TException).Name}, but got {ex.GetType().Name}. {message}");
            }
        }
    }
    
    /// <summary>
    /// 测试上下文
    /// </summary>
    public class TestContext
    {
        public Dictionary<string, object> TestData { get; } = new Dictionary<string, object>();
        public List<string> LogMessages { get; } = new List<string>();
        
        public void Log(string message)
        {
            LogMessages.Add($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }
        
        public T GetTestData<T>(string key, T defaultValue = default!)
        {
            return TestData.ContainsKey(key) ? (T)TestData[key] : defaultValue;
        }
        
        public void SetTestData<T>(string key, T value)
        {
            TestData[key] = value!;
        }
    }
    
    /// <summary>
    /// 断言异常
    /// </summary>
    public class AssertionException : Exception
    {
        public AssertionException(string message) : base(message) { }
    }
    
    /// <summary>
    /// 测试运行器
    /// </summary>
    public static class TestRunner
    {
        /// <summary>
        /// 测试结果
        /// </summary>
        public class TestResult
        {
            public string TestName { get; set; } = string.Empty;
            public bool Passed { get; set; }
            public TimeSpan Duration { get; set; }
            public Exception? Error { get; set; }
            public List<string> Logs { get; set; } = new List<string>();
        }
        
        /// <summary>
        /// 测试套件结果
        /// </summary>
        public class TestSuiteResult
        {
            public string SuiteName { get; set; } = string.Empty;
            public List<TestResult> TestResults { get; set; } = new List<TestResult>();
            public int TotalTests => TestResults.Count;
            public int PassedTests => TestResults.Count(r => r.Passed);
            public int FailedTests => TestResults.Count(r => !r.Passed);
            public TimeSpan TotalDuration => TimeSpan.FromMilliseconds(TestResults.Sum(r => r.Duration.TotalMilliseconds));
        }
        
        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static async Task<TestSuiteResult> RunAllTests(Assembly assembly)
        {
            var testTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(TestBase)) && !t.IsAbstract)
                .ToList();
            
            var suiteResult = new TestSuiteResult { SuiteName = assembly.GetName().Name ?? "Unknown" };
            
            foreach (var testType in testTypes)
            {
                var testResults = await RunTestsInClass(testType);
                suiteResult.TestResults.AddRange(testResults);
            }
            
            return suiteResult;
        }
        
        /// <summary>
        /// 运行指定类的所有测试方法
        /// </summary>
        public static async Task<List<TestResult>> RunTestsInClass(Type testClass)
        {
            var testMethods = testClass.GetMethods()
                .Where(m => m.IsPublic && m.ReturnType == typeof(void) && 
                           m.GetParameters().Length == 0 &&
                           m.Name.StartsWith("Test"))
                .ToList();
            
            var results = new List<TestResult>();
            
            foreach (var method in testMethods)
            {
                var result = await RunTest(testClass, method);
                results.Add(result);
            }
            
            return results;
        }
        
        /// <summary>
        /// 运行单个测试方法
        /// </summary>
        public static async Task<TestResult> RunTest(Type testClass, MethodInfo testMethod)
        {
            var result = new TestResult { TestName = $"{testClass.Name}.{testMethod.Name}" };
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                var testInstance = Activator.CreateInstance(testClass) as TestBase;
                if (testInstance == null)
                {
                    throw new InvalidOperationException($"无法创建测试实例: {testClass.Name}");
                }
                
                // 执行Setup
                testInstance.Setup();
                
                // 执行测试方法
                if (testMethod.ReturnType == typeof(Task))
                {
                    var task = (Task?)testMethod.Invoke(testInstance, null);
                    if (task != null) await task;
                }
                else
                {
                    testMethod.Invoke(testInstance, null);
                }
                
                // 执行Teardown
                testInstance.Teardown();
                
                result.Passed = true;
                result.Logs.AddRange(testInstance.Context.LogMessages);
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.Error = ex is TargetInvocationException tie ? tie.InnerException : ex;
                result.Logs.Add($"测试失败: {result.Error.Message}");
            }
            finally
            {
                stopwatch.Stop();
                result.Duration = stopwatch.Elapsed;
            }
            
            return result;
        }
        
        /// <summary>
        /// 生成测试报告
        /// </summary>
        public static string GenerateReport(TestSuiteResult suiteResult)
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== 测试报告 ===");
            report.AppendLine($"测试套件: {suiteResult.SuiteName}");
            report.AppendLine($"总测试数: {suiteResult.TotalTests}");
            report.AppendLine($"通过数: {suiteResult.PassedTests}");
            report.AppendLine($"失败数: {suiteResult.FailedTests}");
            report.AppendLine($"总耗时: {suiteResult.TotalDuration:hh\\:mm\\:ss\\.fff}");
            report.AppendLine();
            
            foreach (var testResult in suiteResult.TestResults)
            {
                var status = testResult.Passed ? "✓" : "✗";
                report.AppendLine($"{status} {testResult.TestName} ({testResult.Duration.TotalMilliseconds}ms)");
                
                if (!testResult.Passed && testResult.Error != null)
                {
                    report.AppendLine($"  错误: {testResult.Error.Message}");
                }
                
                foreach (var log in testResult.Logs)
                {
                    report.AppendLine($"  日志: {log}");
                }
            }
            
            return report.ToString();
        }
    }
    
    /// <summary>
    /// 单元测试属性标记
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TestAttribute : Attribute
    {
        public string? Description { get; set; }
    }
    
    /// <summary>
    /// 集成测试属性标记
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class IntegrationTestAttribute : Attribute
    {
        public string? Description { get; set; }
    }
    
    /// <summary>
    /// 性能测试属性标记
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class PerformanceTestAttribute : Attribute
    {
        public int Iterations { get; set; } = 1000;
    }
}