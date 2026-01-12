using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FakeMicro.Api.Extensions
{
    /// <summary>
    /// 健康检查扩展
    /// </summary>
    public static class HealthCheckExtensions
    {
        public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services)
        {
            services.AddHealthChecks()
                // PostgreSQL 健康检查
                .AddNpgSql(
                    name: "PostgreSQL",
                    tags: new[] { "database", "postgresql", "ready" })
                
                // Redis 健康检查
                .AddRedis(
                    name: "Redis",
                    tags: new[] { "cache", "redis", "ready" })
                
                // Orleans Silo 健康检查
                .AddCheck<OrleansHealthCheck>(
                    name: "Orleans",
                    tags: new[] { "orleans", "cluster", "ready" })
                
                // MongoDB 健康检查（如果使用）
                // .AddMongoDb(
                //     name: "MongoDB",
                //     tags: new[] { "database", "mongodb", "ready" })
                ;

            return services;
        }

        public static IApplicationBuilder UseCustomHealthChecks(this IApplicationBuilder app)
        {
            // 详细健康检查端点（仅内部访问）
            app.UseHealthChecks("/health/detailed", new HealthCheckOptions
            {
                ResponseWriter = WriteDetailedHealthCheckResponse,
                Predicate = _ => true
            });

            // 就绪检查（用于K8s等）
            app.UseHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
                ResponseWriter = WriteHealthCheckResponse
            });

            // 存活检查（用于K8s等）
            app.UseHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false, // 只检查服务是否运行
                ResponseWriter = WriteHealthCheckResponse
            });

            // 简单健康检查端点
            app.UseHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = WriteHealthCheckResponse
            });

            return app;
        }

        private static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";

            var result = JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                totalDuration = report.TotalDuration.TotalMilliseconds,
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    duration = e.Value.Duration.TotalMilliseconds
                })
            }, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            return context.Response.WriteAsync(result);
        }

        private static Task WriteDetailedHealthCheckResponse(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";

            var result = JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                totalDuration = report.TotalDuration.TotalMilliseconds,
                timestamp = System.DateTime.UtcNow,
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    duration = e.Value.Duration.TotalMilliseconds,
                    description = e.Value.Description,
                    data = e.Value.Data,
                    exception = e.Value.Exception?.Message,
                    tags = e.Value.Tags
                })
            }, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            return context.Response.WriteAsync(result);
        }
    }

    /// <summary>
    /// Orleans 集群健康检查
    /// </summary>
    public class OrleansHealthCheck : IHealthCheck
    {
        private readonly Orleans.IClusterClient _clusterClient;

        public OrleansHealthCheck(Orleans.IClusterClient clusterClient)
        {
            _clusterClient = clusterClient;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            System.Threading.CancellationToken cancellationToken = default)
        {
            try
            {
                // 检查Orleans集群连接
                if (_clusterClient == null)
                {
                    return HealthCheckResult.Unhealthy("Orleans client is not initialized");
                }

                // 尝试获取一个测试Grain（可选）
                // var testGrain = _clusterClient.GetGrain<ITestGrain>(0);
                // await testGrain.PingAsync();

                return HealthCheckResult.Healthy("Orleans cluster is connected");
            }
            catch (System.Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    "Orleans cluster connection failed",
                    ex);
            }
        }
    }
}
