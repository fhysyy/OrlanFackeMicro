using Microsoft.Extensions.Logging;
using System;

namespace FakeMicro.Api.Services
{
    /// <summary>
    /// 系统健康检查服务
    /// 用于Hangfire定时任务执行系统健康检查
    /// </summary>
    public class SystemHealthService
    {
        private readonly ILogger<SystemHealthService> _logger;

        public SystemHealthService(ILogger<SystemHealthService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 执行系统健康检查
        /// </summary>
        public void PerformHealthCheck()
        {
            _logger.LogInformation("系统健康检查 - {Timestamp}", DateTime.Now);
        }
    }
}