using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using FakeMicro.Interfaces;
using FakeMicro.Api.Services;
using FakeMicro.Interfaces.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace FakeMicro.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HangfireJobsController : ControllerBase
    {
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly IClusterClient _clusterClient;
        private readonly OrleansTaskExecutor _orleansTaskExecutor;

        public HangfireJobsController(
            IBackgroundJobClient backgroundJobClient,
            IRecurringJobManager recurringJobManager,
            IClusterClient clusterClient,
            OrleansTaskExecutor orleansTaskExecutor)
        {
            _backgroundJobClient = backgroundJobClient;
            _recurringJobManager = recurringJobManager;
            _clusterClient = clusterClient;
            _orleansTaskExecutor = orleansTaskExecutor;
        }

        /// <summary>
        /// 获取所有定时任务
        /// </summary>
        [HttpGet("recurring")]
        public IActionResult GetRecurringJobs()
        {
            var recurringJobs = new List<object>();
            
            // 由于Hangfire API限制，这里返回空列表
            // 在实际应用中，可以通过数据库查询或Hangfire的存储API获取定时任务信息
            return Ok(recurringJobs);
        }

        /// <summary>
        /// 创建定时任务
        /// </summary>
        [HttpPost("recurring")]
        public IActionResult CreateRecurringJob([FromBody] CreateRecurringJobRequest request)
        {
            if (string.IsNullOrEmpty(request.JobId) || string.IsNullOrEmpty(request.CronExpression))
            {
                return BadRequest("JobId和CronExpression不能为空");
            }

            _recurringJobManager.AddOrUpdate(
                request.JobId,
                () => Console.WriteLine($"执行定时任务: {request.JobId} - {DateTime.Now}"),
                request.CronExpression);

            return Ok(new { Message = $"定时任务 '{request.JobId}' 创建成功", JobId = request.JobId });
        }

        /// <summary>
        /// 更新定时任务
        /// </summary>
        [HttpPut("recurring/{jobId}")]
        public IActionResult UpdateRecurringJob(string jobId, [FromBody] UpdateRecurringJobRequest request)
        {
            if (string.IsNullOrEmpty(request.CronExpression))
            {
                return BadRequest("CronExpression不能为空");
            }

            _recurringJobManager.AddOrUpdate(
                jobId,
                () => Console.WriteLine($"执行定时任务: {jobId} - {DateTime.Now}"),
                request.CronExpression);

            return Ok(new { Message = $"定时任务 '{jobId}' 更新成功", JobId = jobId });
        }

        /// <summary>
        /// 删除定时任务
        /// </summary>
        [HttpDelete("recurring/{jobId}")]
        public IActionResult DeleteRecurringJob(string jobId)
        {
            _recurringJobManager.RemoveIfExists(jobId);
            return Ok(new { Message = $"定时任务 '{jobId}' 删除成功" });
        }

        /// <summary>
        /// 触发定时任务立即执行
        /// </summary>
        [HttpPost("recurring/{jobId}/trigger")]
        public IActionResult TriggerRecurringJob(string jobId)
        {
            _recurringJobManager.Trigger(jobId);
            return Ok(new { Message = $"定时任务 '{jobId}' 已触发执行" });
        }

        /// <summary>
        /// 创建一次性后台任务
        /// </summary>
        [HttpPost("background")]
        public IActionResult CreateBackgroundJob([FromBody] CreateBackgroundJobRequest request)
        {
            var jobId = _backgroundJobClient.Enqueue(() => 
                Console.WriteLine($"执行后台任务: {request.JobName} - {DateTime.Now}"));

            return Ok(new { 
                Message = $"后台任务 '{request.JobName}' 创建成功", 
                JobId = jobId,
                JobName = request.JobName
            });
        }

        /// <summary>
        /// 创建与Silo交互的后台任务
        /// </summary>
        [HttpPost("orleans")]
        public IActionResult CreateOrleansJob([FromBody] CreateOrleansJobRequest request)
        {
            // 使用OrleansTaskExecutor执行Orleans任务，保持代码一致性
            var jobId = _backgroundJobClient.Enqueue(() => 
                _orleansTaskExecutor.ExecuteGrainOperationAsync(
                    request.GrainType, 
                    request.Operation, 
                    request.Parameters));

            return Ok(new { 
                Message = $"Orleans任务 '{request.Operation}' 创建成功", 
                JobId = jobId,
                GrainType = request.GrainType,
                Operation = request.Operation
            });
        }

        /// <summary>
        /// 获取后台任务状态
        /// </summary>
        [HttpGet("background/{jobId}")]
        public IActionResult GetBackgroundJobStatus(string jobId)
        {
            var jobMonitoringApi = JobStorage.Current.GetMonitoringApi();
            var jobDetails = jobMonitoringApi.JobDetails(jobId);

            if (jobDetails == null)
            {
                return NotFound($"任务 '{jobId}' 不存在");
            }

            return Ok(new
            {
                JobId = jobId,
                State = jobDetails.History.LastOrDefault()?.StateName ?? "Unknown",
                CreatedAt = jobDetails.CreatedAt,
                History = jobDetails.History.Select(h => new
                {
                    h.StateName,
                    h.CreatedAt,
                    h.Reason
                })
            });
        }

        // 注意: 不再需要本地的ExecuteOrleansTaskAsync等私有方法，因为我们现在使用OrleansTaskExecutor服务
        // 这保持了代码的一致性和可维护性，避免了重复逻辑
    }
    
    // 请求模型已移至 FakeMicro.Interfaces.Models 命名空间下的 HangfireModels.cs 文件中
}