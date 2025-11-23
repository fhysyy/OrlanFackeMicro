using FakeMicro.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace FakeMicro.Api.Controllers
{
    /// <summary>
    /// CAP事件总线测试控制器
    /// 用于测试事件发布和外部订阅功能
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CapTestController : ControllerBase
    {
        private readonly IEventPublisher _eventPublisher;
        private readonly IExtendedCapPublisher _extendedCapPublisher;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="eventPublisher">事件发布服务</param>
        /// <param name="extendedCapPublisher">扩展的CAP发布器</param>
        public CapTestController(
            IEventPublisher eventPublisher,
            IExtendedCapPublisher extendedCapPublisher)
        {
            _eventPublisher = eventPublisher;
            _extendedCapPublisher = extendedCapPublisher;
        }

        /// <summary>
        /// 测试发布用户创建事件
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="email">邮箱</param>
        /// <returns>操作结果</returns>
        [HttpPost("publish-user-created")]
        public async Task<IActionResult> PublishUserCreatedAsync([FromQuery] string username, [FromQuery] string email)
        {
            try
            {
                var userId = Guid.NewGuid();
                await _eventPublisher.PublishUserCreatedAsync(userId, username, email);
                return Ok(new { Message = "用户创建事件发布成功", UserId = userId });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "事件发布失败", Error = ex.Message });
            }
        }

        /// <summary>
        /// 测试发布自定义事件
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="payload">事件数据</param>
        /// <returns>操作结果</returns>
        [HttpPost("publish-custom-event")]
        public async Task<IActionResult> PublishCustomEventAsync([FromQuery] string eventName, [FromBody] object payload)
        {
            try
            {
                await _eventPublisher.PublishCustomEventAsync(eventName, payload);
                return Ok(new { Message = "自定义事件发布成功", EventName = eventName });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "事件发布失败", Error = ex.Message });
            }
        }

        /// <summary>
        /// 测试发布带标签的事件
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="tags">事件标签（逗号分隔）</param>
        /// <param name="payload">事件数据</param>
        /// <returns>操作结果</returns>
        [HttpPost("publish-tagged-event")]
        public async Task<IActionResult> PublishTaggedEventAsync([FromQuery] string eventName, [FromQuery] string tags, [FromBody] object payload)
        {
            try
            {
                var tagArray = tags.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                await _eventPublisher.PublishEventWithTagsAsync(eventName, payload, tagArray);
                return Ok(new { Message = "带标签事件发布成功", EventName = eventName, Tags = tagArray });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "事件发布失败", Error = ex.Message });
            }
        }

        /// <summary>
        /// 注册外部订阅者
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="callbackUrl">回调URL</param>
        /// <param name="description">订阅者描述</param>
        /// <returns>操作结果</returns>
        [HttpPost("register-subscriber")]
        public async Task<IActionResult> RegisterSubscriberAsync([FromQuery] string eventName, [FromQuery] string callbackUrl, [FromQuery] string description = null)
        {
            try
            {
                await _extendedCapPublisher.RegisterExternalSubscriberAsync(eventName, callbackUrl, description);
                return Ok(new { Message = "外部订阅者注册成功", EventName = eventName, CallbackUrl = callbackUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "订阅者注册失败", Error = ex.Message });
            }
        }

        /// <summary>
        /// 移除外部订阅者
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="callbackUrl">回调URL</param>
        /// <returns>操作结果</returns>
        [HttpDelete("remove-subscriber")]
        public async Task<IActionResult> RemoveSubscriberAsync([FromQuery] string eventName, [FromQuery] string callbackUrl)
        {
            try
            {
                await _extendedCapPublisher.RemoveExternalSubscriberAsync(eventName, callbackUrl);
                return Ok(new { Message = "外部订阅者移除成功", EventName = eventName, CallbackUrl = callbackUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { Message = "订阅者移除失败", Error = ex.Message });
            }
        }

        /// <summary>
        /// 健康检查端点
        /// 测试CAP服务是否正常注册
        /// </summary>
        /// <returns>服务状态</returns>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new 
            {
                Status = "Healthy",
                Message = "CAP服务注册正常",
                Timestamp = DateTime.UtcNow
            });
        }
    }
}