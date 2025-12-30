using Microsoft.AspNetCore.Mvc;
using Orleans;
using FakeMicro.Interfaces;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using SqlSugar;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;

namespace FakeMicro.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IClusterClient _clusterClient;
        private readonly ILogger<HealthController> _logger;
        private readonly ISqlSugarClient _sqlSugarClient;

        public HealthController(IClusterClient clusterClient, ILogger<HealthController> logger, ISqlSugarClient sqlSugarClient)
        {
            _clusterClient = clusterClient;
            _logger = logger;
            _sqlSugarClient = sqlSugarClient;
        }

        [HttpGet]
        public async Task<IActionResult> GetHealth()
        {
            var healthResult = new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                services = new {}
            };
            
            var services = new Dictionary<string, string>();
            var errors = new Dictionary<string, string>();

            try
            {
                // 测试 Orleans 连接
                var helloGrain = _clusterClient.GetGrain<IHelloGrain>("health-check");
                var orleansResponse = await helloGrain.SayHelloAsync("health-check");
                services["orleans"] = "connected";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Orleans 连接检查失败");
                services["orleans"] = "disconnected";
                errors["orleans"] = ex.Message;
            }

            try
            {
                // 测试数据库连接
                var dbResponse = await _sqlSugarClient.Ado.GetScalarAsync("SELECT 1");
                if (dbResponse != null && int.TryParse(dbResponse.ToString(), out int dbResult) && dbResult == 1)
                {
                    services["database"] = "connected";
                }
                else
                {
                    services["database"] = "disconnected";
                    errors["database"] = "数据库查询返回意外结果";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库连接检查失败");
                services["database"] = "disconnected";
                errors["database"] = ex.Message;
            }

            // 确定整体状态
            var overallStatus = services.All(s => s.Value == "connected") ? "healthy" : "unhealthy";

            var result = new
            {
                status = overallStatus,
                timestamp = DateTime.UtcNow,
                services = services,
                errors = errors.Count > 0 ? errors : null
            };

            if (overallStatus == "healthy")
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(503, result);
            }
        }
    }
}