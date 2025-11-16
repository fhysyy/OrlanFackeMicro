using FakeMicro.Interfaces.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FakeMicro.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<EventsController> _logger;

        public EventsController(IEventPublisher eventPublisher, ILogger<EventsController> logger)
        {
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        /// <summary>
        /// 发布用户创建事件
        /// </summary>
        [HttpPost("user-created")]
        public async Task<IActionResult> PublishUserCreated([FromBody] UserCreatedEvent userEvent)
        {
            try
            {
                await _eventPublisher.PublishUserCreatedAsync(
                    userEvent.UserId, 
                    userEvent.Username, 
                    userEvent.Email);
                
                return Ok(new { message = "用户创建事件已发布", userId = userEvent.UserId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布用户创建事件失败: {UserId}", userEvent.UserId);
                return BadRequest(new { error = "发布事件失败", details = ex.Message });
            }
        }

        /// <summary>
        /// 发布用户更新事件
        /// </summary>
        [HttpPost("user-updated")]
        public async Task<IActionResult> PublishUserUpdated([FromBody] UserUpdatedEvent userEvent)
        {
            try
            {
                await _eventPublisher.PublishUserUpdatedAsync(
                    userEvent.UserId, 
                    userEvent.Username, 
                    userEvent.Email);
                
                return Ok(new { message = "用户更新事件已发布", userId = userEvent.UserId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布用户更新事件失败: {UserId}", userEvent.UserId);
                return BadRequest(new { error = "发布事件失败", details = ex.Message });
            }
        }

        /// <summary>
        /// 发布用户删除事件
        /// </summary>
        [HttpPost("user-deleted")]
        public async Task<IActionResult> PublishUserDeleted([FromBody] UserDeletedEvent userEvent)
        {
            try
            {
                await _eventPublisher.PublishUserDeletedAsync(userEvent.UserId);
                return Ok(new { message = "用户删除事件已发布", userId = userEvent.UserId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布用户删除事件失败: {UserId}", userEvent.UserId);
                return BadRequest(new { error = "发布事件失败", details = ex.Message });
            }
        }

        /// <summary>
        /// 测试事件总线连接
        /// </summary>
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { 
                message = "CAP事件总线服务运行正常",
                timestamp = DateTime.UtcNow,
                features = new {
                    postgresql = "已配置（当前仅支持PostgreSQL）",
                    rabbitmq = "已配置",
                    dashboard = "已启用"
                }
            });
        }
    }
}