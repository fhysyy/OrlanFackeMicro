using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using System;

namespace FakeMicro.Api.Extensions
{
    /// <summary>
    /// OpenTelemetry 配置扩展
    /// </summary>
    public static class OpenTelemetryExtensions
    {
        public static IServiceCollection AddOpenTelemetryInstrumentation(
            this IServiceCollection services,
            string serviceName = "FakeMicro.Api",
            string serviceVersion = "1.0.0")
        {
            services.AddOpenTelemetry()
                .ConfigureResource(builder => builder
                    .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
                    .AddTelemetrySdk()
                    .AddEnvironmentVariableDetector())
                .WithTracing(builder =>
                {
                    builder
                        // 添加源
                        .AddSource(serviceName)
                        .AddSource("FakeMicro.*")
                        
                        // 自动仪表化
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            options.RecordException = true;
                            options.Filter = (httpContext) =>
                            {
                                // 排除健康检查端点
                                return !httpContext.Request.Path.StartsWithSegments("/health");
                            };
                        })
                        .AddHttpClientInstrumentation(options =>
                        {
                            options.RecordException = true;
                        })
                        .AddSqlClientInstrumentation(options =>
                        {
                            options.SetDbStatementForText = true;
                            options.RecordException = true;
                            options.EnableConnectionLevelAttributes = true;
                        })
                        .AddRedisInstrumentation()
                        
                        // 采样策略
                        .SetSampler(new TraceIdRatioBasedSampler(1.0)) // 100% 采样（生产环境建议降低）
                        
                        // 导出器配置（根据环境选择）
                        .AddConsoleExporter() // 开发环境
                        // .AddOtlpExporter(options =>
                        // {
                        //     // 生产环境配置
                        //     options.Endpoint = new Uri("http://localhost:4317");
                        // })
                        // .AddJaegerExporter(options =>
                        // {
                        //     options.AgentHost = "localhost";
                        //     options.AgentPort = 6831;
                        // })
                        ;
                })
                .WithMetrics(builder =>
                {
                    builder
                        .AddMeter(serviceName)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddProcessInstrumentation()
                        
                        // 导出器
                        .AddConsoleExporter() // 开发环境
                        // .AddOtlpExporter(options =>
                        // {
                        //     options.Endpoint = new Uri("http://localhost:4317");
                        // })
                        // .AddPrometheusExporter()
                        ;
                });

            return services;
        }

        /// <summary>
        /// 添加自定义活动源
        /// </summary>
        public static IServiceCollection AddCustomActivitySource(
            this IServiceCollection services,
            string sourceName,
            string sourceVersion = "1.0.0")
        {
            services.AddSingleton(new System.Diagnostics.ActivitySource(sourceName, sourceVersion));
            return services;
        }
    }
}
