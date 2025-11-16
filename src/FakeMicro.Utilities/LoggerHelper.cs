using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FakeMicro.Utilities
{
    /// <summary>
    /// 日志助手类
    /// </summary>
    public static class LoggerHelper
    {
        private static readonly object _lockObject = new object();
        
        /// <summary>
        /// 日志级别
        /// </summary>
        public enum LogLevel
        {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3,
            Critical = 4
        }

        /// <summary>
        /// 日志配置
        /// </summary>
        public class LoggerConfig
        {
            public string LogDirectory { get; set; } = "Logs";
            public LogLevel MinimumLevel { get; set; } = LogLevel.Info;
            public bool WriteToConsole { get; set; } = true;
            public bool WriteToFile { get; set; } = true;
            public int MaxFileSizeMB { get; set; } = 10;
            public int MaxFilesToKeep { get; set; } = 10;
        }

        private static LoggerConfig _config = new LoggerConfig();

        /// <summary>
        /// 配置日志器
        /// </summary>
        public static void Configure(Action<LoggerConfig> configure)
        {
            lock (_lockObject)
            {
                configure?.Invoke(_config);
                
                // 确保日志目录存在
                if (_config.WriteToFile && !Directory.Exists(_config.LogDirectory))
                {
                    Directory.CreateDirectory(_config.LogDirectory);
                }
            }
        }

        /// <summary>
        /// 记录调试信息
        /// </summary>
        public static void Debug(string message, Exception? exception = null)
        {
            Log(LogLevel.Debug, message, exception);
        }

        /// <summary>
        /// 记录信息
        /// </summary>
        public static void Info(string message, Exception? exception = null)
        {
            Log(LogLevel.Info, message, exception);
        }

        /// <summary>
        /// 记录警告信息
        /// </summary>
        public static void Warning(string message, Exception? exception = null)
        {
            Log(LogLevel.Warning, message, exception);
        }

        /// <summary>
        /// 记录错误信息
        /// </summary>
        public static void Error(string message, Exception? exception = null)
        {
            Log(LogLevel.Error, message, exception);
        }

        /// <summary>
        /// 记录严重错误信息
        /// </summary>
        public static void Critical(string message, Exception? exception = null)
        {
            Log(LogLevel.Critical, message, exception);
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        private static void Log(LogLevel level, string message, Exception? exception)
        {
            if (level < _config.MinimumLevel) return;

            var logEntry = CreateLogEntry(level, message, exception);
            
            if (_config.WriteToConsole)
            {
                WriteToConsole(logEntry);
            }

            if (_config.WriteToFile)
            {
                WriteToFile(logEntry);
            }
        }

        /// <summary>
        /// 创建日志条目
        /// </summary>
        private static string CreateLogEntry(LogLevel level, string message, Exception? exception)
        {
            var sb = new StringBuilder();
            sb.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ");
            sb.Append($"[{level.ToString().ToUpper()}] ");
            sb.Append(message);

            if (exception != null)
            {
                sb.AppendLine();
                sb.Append($"Exception: {exception.GetType().Name} - {exception.Message}");
                sb.AppendLine();
                sb.Append($"StackTrace: {exception.StackTrace}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 写入控制台
        /// </summary>
        private static void WriteToConsole(string logEntry)
        {
            var originalColor = Console.ForegroundColor;
            
            try
            {
                switch (logEntry)
                {
                    case string s when s.Contains("[ERROR]") || s.Contains("[CRITICAL]"):
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case string s when s.Contains("[WARNING]"):
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case string s when s.Contains("[INFO]"):
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                    case string s when s.Contains("[DEBUG]"):
                        Console.ForegroundColor = ConsoleColor.Gray;
                        break;
                }

                Console.WriteLine(logEntry);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }

        /// <summary>
        /// 写入文件
        /// </summary>
        private static void WriteToFile(string logEntry)
        {
            lock (_lockObject)
            {
                try
                {
                    var logFile = GetCurrentLogFile();
                    File.AppendAllText(logFile, logEntry + Environment.NewLine);
                    
                    // 检查文件大小并轮转
                    RotateLogFilesIfNeeded(logFile);
                }
                catch (Exception ex)
                {
                    // 如果文件写入失败，回退到控制台
                    Console.WriteLine($"Failed to write to log file: {ex.Message}");
                    Console.WriteLine(logEntry);
                }
            }
        }

        /// <summary>
        /// 获取当前日志文件路径
        /// </summary>
        private static string GetCurrentLogFile()
        {
            var today = DateTime.Today;
            var fileName = $"log_{today:yyyyMMdd}.txt";
            return Path.Combine(_config.LogDirectory, fileName);
        }

        /// <summary>
        /// 轮转日志文件（如果需要）
        /// </summary>
        private static void RotateLogFilesIfNeeded(string currentLogFile)
        {
            if (!File.Exists(currentLogFile)) return;

            var fileInfo = new FileInfo(currentLogFile);
            if (fileInfo.Length <= _config.MaxFileSizeMB * 1024 * 1024) return;

            // 重命名当前文件
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var rotatedFile = Path.Combine(_config.LogDirectory, $"log_{timestamp}.txt");
            File.Move(currentLogFile, rotatedFile);

            // 清理旧文件
            CleanupOldLogFiles();
        }

        /// <summary>
        /// 清理旧日志文件
        /// </summary>
        private static void CleanupOldLogFiles()
        {
            try
            {
                var logFiles = Directory.GetFiles(_config.LogDirectory, "log_*.txt");
                if (logFiles.Length <= _config.MaxFilesToKeep) return;

                Array.Sort(logFiles);
                var filesToDelete = logFiles.Length - _config.MaxFilesToKeep;

                for (int i = 0; i < filesToDelete; i++)
                {
                    File.Delete(logFiles[i]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to cleanup old log files: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步记录日志
        /// </summary>
        public static Task LogAsync(LogLevel level, string message, Exception? exception = null)
        {
            return Task.Run(() => Log(level, message, exception));
        }

        /// <summary>
        /// 记录方法进入日志
        /// </summary>
        public static void LogMethodEntry(string methodName, params object[] parameters)
        {
            var parameterString = parameters.Length > 0 ? $"Parameters: {string.Join(", ", parameters)}" : "No parameters";
            Debug($"Entering {methodName} - {parameterString}");
        }

        /// <summary>
        /// 记录方法退出日志
        /// </summary>
        public static void LogMethodExit(string methodName, object? result = null)
        {
            var resultString = result != null ? $"Result: {result}" : "Void method";
            Debug($"Exiting {methodName} - {resultString}");
        }

        /// <summary>
        /// 记录性能日志
        /// </summary>
        public static IDisposable LogPerformance(string operationName)
        {
            return new PerformanceLogger(operationName);
        }

        /// <summary>
        /// 性能日志记录器
        /// </summary>
        private class PerformanceLogger : IDisposable
        {
            private readonly string _operationName;
            private readonly DateTime _startTime;
            private bool _disposed = false;

            public PerformanceLogger(string operationName)
            {
                _operationName = operationName;
                _startTime = DateTime.Now;
                Debug($"Starting performance measurement for: {_operationName}");
            }

            public void Dispose()
            {
                if (_disposed) return;
                
                var elapsed = DateTime.Now - _startTime;
                Info($"Performance measurement for {_operationName}: {elapsed.TotalMilliseconds}ms");
                _disposed = true;
            }
        }
    }
}