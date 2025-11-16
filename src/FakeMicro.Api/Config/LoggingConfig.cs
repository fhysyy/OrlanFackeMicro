namespace FakeMicro.Api.Config
{
    /// <summary>
    /// 日志配置
    /// </summary>
    public class LoggingConfig
    {
        public ElasticsearchConfig Elasticsearch { get; set; } = new();
        public bool EnableConsoleLogging { get; set; } = true;
        public bool EnableFileLogging { get; set; } = false;
        public string FileLogPath { get; set; } = "logs";
        public string MinimumLevel { get; set; } = "Information";
        public bool EnableRequestLogging { get; set; } = true;
        public bool EnablePerformanceLogging { get; set; } = true;
    }
}