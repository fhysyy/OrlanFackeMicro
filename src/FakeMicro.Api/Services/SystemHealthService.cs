using Microsoft.Extensions.Logging;
using System;
using Orleans;
using FakeMicro.Interfaces;
using SqlSugar;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace FakeMicro.Api.Services
{
    /// <summary>
    /// 系统健康检查服务
    /// 用于Hangfire定时任务执行系统健康检查
    /// </summary>
    public class SystemHealthService
    {
        private readonly ILogger<SystemHealthService> _logger;
        private readonly IClusterClient _clusterClient;
        private readonly ISqlSugarClient _sqlSugarClient;

        public SystemHealthService(ILogger<SystemHealthService> logger, IClusterClient clusterClient, ISqlSugarClient sqlSugarClient)
        {
            _logger = logger;
            _clusterClient = clusterClient;
            _sqlSugarClient = sqlSugarClient;
        }

        /// <summary>
        /// 执行系统健康检查
        /// </summary>
        public async Task PerformHealthCheck()
        {
            _logger.LogInformation("开始系统健康检查 - {Timestamp}", DateTime.Now);
            
            var services = new Dictionary<string, string>();
            var errors = new Dictionary<string, string>();

            try
            {
                // 测试 Orleans 连接
                var helloGrain = _clusterClient.GetGrain<IHelloGrain>("health-check");
                var orleansResponse = await helloGrain.SayHelloAsync("health-check");
                services["orleans"] = "connected";
                _logger.LogInformation("Orleans 服务健康检查: 正常");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Orleans 服务健康检查失败");
                services["orleans"] = "disconnected";
                errors["orleans"] = ex.Message;
            }

            try
            {
                // 测试数据库连接
                var dbResponse = await _sqlSugarClient.Ado.GetScalarAsync("SELECT 1");
                if (dbResponse != null && int.TryParse(dbResponse.ToString(), out int result) && result == 1)
                {
                    services["database"] = "connected";
                    _logger.LogInformation("数据库服务健康检查: 正常");
                }
                else
                {
                    services["database"] = "disconnected";
                    errors["database"] = "数据库查询返回意外结果";
                    _logger.LogError("数据库服务健康检查失败: 数据库查询返回意外结果");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库服务健康检查失败");
                services["database"] = "disconnected";
                errors["database"] = ex.Message;
            }

            // 确定整体状态
            var overallStatus = services.All(s => s.Value == "connected") ? "healthy" : "unhealthy";
            
            if (overallStatus == "healthy")
            {
                _logger.LogInformation("系统健康检查完成 - 所有服务正常");
            }
            else
            {
                _logger.LogWarning("系统健康检查完成 - 部分服务异常: {Errors}", string.Join(", ", errors.Select(e => $"{e.Key}: {e.Value}")));
            }
        }
    }
}