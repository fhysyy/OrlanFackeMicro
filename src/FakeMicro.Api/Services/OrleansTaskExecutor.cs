using FakeMicro.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FakeMicro.Api.Services
{
    /// <summary>
    /// Orleans任务执行器
    /// 用于在HangFire后台任务中执行Orleans Grain操作
    /// </summary>
    public class OrleansTaskExecutor
    {
        private readonly ILogger<OrleansTaskExecutor> _logger;

        public OrleansTaskExecutor(ILogger<OrleansTaskExecutor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 执行Orleans Grain操作
        /// </summary>
        /// <param name="grainType">Grain类型</param>
        /// <param name="operation">操作名称</param>
        /// <param name="parameters">操作参数</param>
        public async Task ExecuteGrainOperationAsync(string grainType, string operation, Dictionary<string, object> parameters = null)
        {
            try
            {
                _logger.LogInformation("[{Timestamp}] 开始执行Orleans任务: {GrainType}.{Operation}", DateTime.Now, grainType, operation);

                parameters ??= new Dictionary<string, object>();

                switch (grainType.ToLower())
                {
                    case "counter":
                        await ExecuteCounterGrainOperationAsync(operation, parameters);
                        break;
                    case "hello":
                        await ExecuteHelloGrainOperationAsync(operation, parameters);
                        break;
                    default:
                        _logger.LogWarning("未知的Grain类型: {GrainType}", grainType);
                        break;
                }

                _logger.LogInformation("[{Timestamp}] Orleans任务执行完成: {GrainType}.{Operation}", DateTime.Now, grainType, operation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{Timestamp}] Orleans任务执行失败: {GrainType}.{Operation}", DateTime.Now, grainType, operation);
                throw;
            }
        }

        /// <summary>
        /// 执行CounterGrain操作
        /// </summary>
        private async Task ExecuteCounterGrainOperationAsync(string operation, Dictionary<string, object> parameters)
        {
            var grainId = parameters.TryGetValue("grainId", out var id) ? id.ToString() : "default";
            // 模拟响应，不使用_clusterClient
            switch (operation.ToLower())
            {
                case "increment":
                    _logger.LogInformation("模拟CounterGrain增量完成: {GrainId}, 当前值: {Count}", grainId, 1);
                    break;
                case "decrement":
                    _logger.LogInformation("模拟CounterGrain减量完成: {GrainId}, 当前值: {Count}", grainId, 0);
                    break;
                case "get":
                    _logger.LogInformation("模拟CounterGrain查询完成: {GrainId}, 当前值: {Count}", grainId, 0);
                    break;
                case "reset":
                    _logger.LogInformation("模拟CounterGrain重置完成: {GrainId}", grainId);
                    break;
                default:
                    _logger.LogWarning("未知的CounterGrain操作: {Operation}", operation);
                    break;
            }
        }

        /// <summary>
        /// 执行HelloGrain操作
        /// </summary>
        private async Task ExecuteHelloGrainOperationAsync(string operation, Dictionary<string, object> parameters)
        {
            var grainId = parameters.TryGetValue("grainId", out var id) ? id.ToString() : "default";
            
            // 模拟响应，不使用_clusterClient
            switch (operation.ToLower())
            {
                case "sayhello":
                    var greeting = parameters.TryGetValue("greeting", out var greet) ? greet.ToString() : "Hello from Hangfire";
                    var response = $"模拟响应: Hello, {greeting ?? "Hello from Hangfire"}!";
                    _logger.LogInformation("模拟HelloGrain响应: {Response}", response);
                    break;
                default:
                    _logger.LogWarning("未知的HelloGrain操作: {Operation}", operation);
                    break;
            }
        }
    }
}