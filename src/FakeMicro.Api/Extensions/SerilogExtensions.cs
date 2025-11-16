using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using FakeMicro.Api.Config;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.AspNetCore.Builder;

namespace FakeMicro.Api.Extensions
{
    /// <summary>
    /// Serilog扩展方法
    /// </summary>
    public static class SerilogExtensions
    {
        /// <summary>
        /// 创建Serilog日志记录器
        /// </summary>
        public static Serilog.ILogger CreateLogger(IConfiguration configuration)
        {
            var elasticsearchConfig = configuration.GetSection("Elasticsearch").Get<ElasticsearchConfig>() ?? new ElasticsearchConfig();
            
            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "FakeMicro.Api")
                .Enrich.WithProperty("Environment", configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development")
                //.WriteTo.Console(
                //    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/api-.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");

            // 配置Elasticsearch Sink
            if (!string.IsNullOrEmpty(elasticsearchConfig.Url) && IsValidUri(elasticsearchConfig.Url))
            {
                loggerConfiguration.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticsearchConfig.Url))
                {
                    IndexFormat = string.Format(elasticsearchConfig.IndexFormat, DateTime.UtcNow),
                    AutoRegisterTemplate = true,
                    AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                    NumberOfShards = elasticsearchConfig.NumberOfShards,
                    NumberOfReplicas = elasticsearchConfig.NumberOfReplicas,
                    ModifyConnectionSettings = conn =>
                    {
                        if (!string.IsNullOrEmpty(elasticsearchConfig.Username) && !string.IsNullOrEmpty(elasticsearchConfig.Password))
                        {
                            conn = conn.BasicAuthentication(elasticsearchConfig.Username, elasticsearchConfig.Password);
                        }
                        
                        // EnableDebugMode 属性已移除，使用默认配置
                        
                        return conn;
                    },
                    FailureCallback = (e, ex) => Console.WriteLine($"Unable to submit event to Elasticsearch: {e.MessageTemplate}"),
                    EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                                     EmitEventFailureHandling.WriteToFailureSink |
                                     EmitEventFailureHandling.RaiseCallback
                });
            }

            return loggerConfiguration.CreateLogger();
        }

        /// <summary>
        /// 创建请求日志中间件
        /// </summary>
        //public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
        //{
        //    return app.UseSerilogRequestLogging(options =>
        //    {
        //        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        //        options.GetLevel = (httpContext, elapsed, ex) =>
        //        {
        //            if (ex != null || httpContext.Response.StatusCode > 499)
        //            {
        //                return LogEventLevel.Error;
        //            }
        //            return LogEventLevel.Information;
        //        };
        //        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        //        {
        //            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? string.Empty);
        //            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme ?? string.Empty);
        //            diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        //            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown");
        //        };
        //    });
        //}

        /// <summary>
        /// 验证URI格式是否有效
        /// </summary>
        private static bool IsValidUri(string uriString)
        {
            if (string.IsNullOrWhiteSpace(uriString))
                return false;

            try
            {
                var uri = new Uri(uriString);
                return uri.IsAbsoluteUri && 
                      (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
            }
            catch (UriFormatException)
            {
                return false;
            }
        }
    }
}