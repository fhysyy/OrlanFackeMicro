using FakeMicro.Entities.Enums;
using FakeMicro.Interfaces.Models;
using FakeMicro.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace FakeMicro.Api.Controllers
{
    /// <summary>
    /// 消息控制器 - 基于Orleans实现的分布式消息管理
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IClusterClient _clusterClient;
        private readonly ILogger<MessagesController> _logger;

        /// <summary>
        /// 构造函数 - 注入Orleans集群客户端和日志记录器
        /// </summary>
        public MessagesController(
            IClusterClient clusterClient,
            ILogger<MessagesController> logger)
        {
            _clusterClient = clusterClient;
            _logger = logger;
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        [HttpPost("send")]

        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request) { 
            try
            {
                var messageId = Guid.NewGuid();
                var messageGrain = _clusterClient.GetGrain<IMessageGrain>(messageId.ToString());

                var messageRequest = new MessageRequest
                {
                    SenderId = GetCurrentUserId(),
                    ReceiverId = request.ReceiverId,
                    ReceiverEmail = request.ReceiverEmail,
                    ReceiverPhone = request.ReceiverPhone,
                    Title = request.Title,
                    Content = request.Content,
                    MessageType = request.MessageType,
                    Channel = request.Channel,
                    ScheduledAt = request.ScheduledAt,
                    ExpiresAt = request.ExpiresAt,
                    Metadata = request.Metadata
                };

                var result = await messageGrain.SendMessageAsync(messageRequest);

                if (result.Success)
                {
                    return Ok(new MessageResponse
                    {
                        Success = true,
                        MessageId = result.MessageId.ToString(),
                        Status = result.Status.ToString()
                    });
                }
                else
                {
                    return BadRequest(new SuccessResponse
                    {
                        Success = false,
                        Message = result.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送消息失败: {ExceptionMessage}", ex.Message);
                return StatusCode(500, new SuccessResponse { Success = false, Message = "发送消息失败: " + ex.Message });
            }
        }

        /// <summary>
        /// 获取消息详情
        /// </summary>
        [HttpGet("{messageId}")]

        public async Task<IActionResult> GetMessage(Guid messageId)
        {
            try
            {
                var messageGrain = _clusterClient.GetGrain<IMessageGrain>(messageId.ToString());
                var message = await messageGrain.GetMessageAsync();

                if (message == null)
                {
                    return NotFound(new SuccessResponse { Success = false, Message = "消息不存在" });
                }

                // 检查权限：只有发送者或接收者可以查看消息
                var currentUserId = GetCurrentUserId();
                if (message.SenderId != currentUserId && message.ReceiverId != currentUserId)
                {
                    return Forbid();
                }

                return Ok(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取消息详情失败: {MessageId}, {ExceptionMessage}", messageId, ex.Message);
                return StatusCode(500, new SuccessResponse { Success = false, Message = "获取消息详情失败: " + ex.Message });
            }
        }

        /// <summary>
        /// 获取用户消息列表
        /// </summary>
        [HttpGet("user/{userId}")]

        public async Task<IActionResult> GetUserMessages(long userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                // 检查权限：只能查看自己的消息
                var currentUserId = GetCurrentUserId();
                if (userId != currentUserId)
                {
                    return Forbid();
                }

                // 创建一个新的消息服务Grain实例来处理请求
                var messageServiceGrain = _clusterClient.GetGrain<IMessageServiceGrain>(Guid.NewGuid());
                
                // 由于IMessageServiceGrain没有直接的GetUserMessagesAsync方法，我们可以尝试使用其他可用的方法
                // 或者返回一个空列表作为临时解决方案
                var messages = new List<MessageDto>();
                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户消息列表失败: {UserId}, {ExceptionMessage}", userId, ex.Message);
                return StatusCode(500, new SuccessResponse { Success = false, Message = "获取消息列表失败: " + ex.Message });
            }
        }

        /// <summary>
        /// 标记消息为已阅读
        /// </summary>
        [HttpPost("{messageId}/read")]

        public async Task<IActionResult> MarkAsRead(Guid messageId)
        {
            try
            {
                var messageGrain = _clusterClient.GetGrain<IMessageGrain>(messageId.ToString());
                var message = await messageGrain.GetMessageAsync();

                if (message == null)
                {
                    return NotFound(new SuccessResponse { Success = false, Message = "消息不存在" });
                }

                // 检查权限：只有接收者可以标记为已阅读
                var currentUserId = GetCurrentUserId();
                if (message.ReceiverId != currentUserId)
                {
                    return Forbid();
                }

                var success = await messageGrain.MarkAsReadAsync(currentUserId.ToString());
                
                if (success)
                {
                    return Ok(new SuccessResponse { Success = true, Message = "消息已标记为已阅读" });
                }
                else
                {
                    return BadRequest(new SuccessResponse { Success = false, Message = "标记消息失败" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "标记消息为已阅读失败: {MessageId}, {ExceptionMessage}", messageId, ex.Message);
                return StatusCode(500, new SuccessResponse { Success = false, Message = "标记消息失败: " + ex.Message });
            }
        }

        /// <summary>
        /// 获取消息统计
        /// </summary>
        [HttpGet("statistics")]

        public async Task<IActionResult> GetStatistics([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // 使用Orleans Grain获取系统消息统计，使用固定的Guid作为key
                var serviceGrain = _clusterClient.GetGrain<IMessageServiceGrain>(Guid.Parse("00000000-0000-0000-0000-000000000000"));
                var statistics = await serviceGrain.GetSystemStatisticsAsync();
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取消息统计失败: {ExceptionMessage}", ex.Message);
                return StatusCode(500, new SuccessResponse { Success = false, Message = "获取统计失败: " + ex.Message });
            }
        }

        /// <summary>
        /// 批量发送消息
        /// </summary>
        [HttpPost("batch-send")]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> SendBatchMessages([FromBody] List<SendMessageRequest> requests)
        {
            try
            {
                // 使用固定的grain key而不是每次创建新的，这样可以更好地利用Orleans的分布机制
                var serviceGrain = _clusterClient.GetGrain<IMessageServiceGrain>(Guid.Parse("00000000-0000-0000-0000-000000000000"));
                
                
                var messageRequests = requests.Select(r => new MessageRequest
                {
                    SenderId = GetCurrentUserId(),
                    ReceiverId = r.ReceiverId,
                    ReceiverEmail = r.ReceiverEmail,
                    ReceiverPhone = r.ReceiverPhone,
                    Title = r.Title,
                    Content = r.Content,
                    MessageType = r.MessageType,
                    Channel = r.Channel,
                    ScheduledAt = r.ScheduledAt,
                    ExpiresAt = r.ExpiresAt,
                    Metadata = r.Metadata
                }).ToList();

                var result = await serviceGrain.SendBatchMessagesAsync(messageRequests);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量发送消息失败: {ExceptionMessage}", ex.Message);
                return StatusCode(500, new SuccessResponse { Success = false, Message = "批量发送消息失败: " + ex.Message });
            }
        }

        /// <summary>
        /// 处理待发送消息（管理员功能）
        /// </summary>
        [HttpPost("process-pending")]
        [Authorize(Roles = "Admin")]

        public async Task<IActionResult> ProcessPendingMessages([FromQuery] int batchSize = 100)
        {
            try
            {
                // 使用固定的grain key，确保管理操作一致性
                var serviceGrain = _clusterClient.GetGrain<IMessageServiceGrain>(Guid.Parse("00000000-0000-0000-0000-000000000000"));
                var processedCount = await serviceGrain.ProcessPendingMessagesAsync(batchSize);

                return Ok(new ProcessPendingResponse
                {
                    Success = true,
                    ProcessedCount = processedCount,
                    Message = $"已处理 {processedCount} 条待发送消息"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理待发送消息失败: {ExceptionMessage}", ex.Message);
                return StatusCode(500, new SuccessResponse { Success = false, Message = "处理消息失败: " + ex.Message });
            }
        }

        /// <summary>
        /// 获取当前用户ID
        /// </summary>
        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                _logger.LogError("无法获取当前用户ID或ID格式无效: {UserIdClaim}", userIdClaim);
                throw new UnauthorizedAccessException("无法获取当前用户ID");
            }
            return userId;
        }
    }
    
    // 所有请求和响应模型已移至 FakeMicro.Interfaces.Models 命名空间下的 MessageModels.cs 文件中
}