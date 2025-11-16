using Microsoft.AspNetCore.Mvc;
using Orleans;
using FakeMicro.Interfaces;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;

namespace FakeMicro.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IClusterClient _clusterClient;
        private readonly ILogger<HealthController> _logger;

        public HealthController(IClusterClient clusterClient, ILogger<HealthController> logger)
        {
            _clusterClient = clusterClient;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetHealth()
        {
            try
            {
                // 测试 Orleans 连接
                var helloGrain = _clusterClient.GetGrain<IHelloGrain>("health-check");
                var response = await helloGrain.SayHelloAsync("health-check");
                
                return Ok(new 
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    orleans = "connected",
                    message = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "健康检查失败");
                return StatusCode(503, new 
                {
                    status = "unhealthy", 
                    timestamp = DateTime.UtcNow,
                    orleans = "disconnected",
                    error = ex.Message
                });
            }
        }
    }
}