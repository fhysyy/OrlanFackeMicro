using FakeMicro.Entities.Enums;
using FakeMicro.Entities;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Events;
using FakeMicro.Interfaces.Models;
using Microsoft.Extensions.Logging;
using Orleans;
using FakeMicro.DatabaseAccess;
using Newtonsoft.Json;

namespace FakeMicro.Grains
{
    /// <summary>
    /// 消息Grain实现
    /// </summary>
    public class MessageGrain : OrleansGrainBase, IMessageGrain
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IEventPublisher _eventPublisher;
        private Message? _currentMessage;

        public MessageGrain(IMessageRepository messageRepository, IEventPublisher eventPublisher, ILogger<MessageGrain> logger) : base(logger)
        {
            _messageRepository = messageRepository;
            _eventPublisher = eventPublisher;
        }

        public override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            // 由于IMessageRepository.GetByIdAsync需要long类型，而grain key是字符串，
            // 在消息实际发送前_currentMessage可能不存在，所以这里不需要查询
            _currentMessage = null;
            await base.OnActivateAsync(cancellationToken);
        }

        public async Task<MessageResult> SendMessageAsync(MessageRequest request)
        {
            try
            {
                // 生成一个新的消息ID，而不是尝试转换grain key
                var message = new Message
                {
                    id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), // 使用时间戳作为消息ID
                   sender_id = request.SenderId,
                   receiver_id = request.ReceiverId,
                    title = request.Title,
                    content = request.Content,
                    message_type = request.MessageType.ToString(),
                    message_channel = request.Channel.ToString(),
                   status = FakeMicro.Entities.Enums.MessageStatus.Pending.ToString(),
                    scheduled_at = request.ScheduledAt,
                    expires_at = request.ExpiresAt,
                    metadata = request.Metadata != null ? JsonConvert.SerializeObject(request.Metadata) : null
                };

                _currentMessage = await _messageRepository.AddAsync(message);

                // 发布消息发送事件
                await _eventPublisher.PublishCustomEventAsync("message.sent", new MessageSentEvent
                {
                    MessageId = message.id,
                    SenderId = message.sender_id,
                    ReceiverId = message.receiver_id,
                    Title = message.title,
                    Content = message.content,
                    MessageType = Enum.Parse<FakeMicro.Entities.Enums.MessageType>(message.message_type),
                    Channel = Enum.Parse<FakeMicro.Entities.Enums.MessageChannel>(message.message_channel),
                    Metadata = request.Metadata
                });

                _logger.LogInformation("消息已创建并准备发送: MessageId={MessageId}", message.id);

                return new MessageResult
                {
                    Success = true,
                    MessageId = message.id,
                    Status = FakeMicro.Entities.Enums.MessageStatus.Pending
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送消息失败");
                return new MessageResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Status = FakeMicro.Entities.Enums.MessageStatus.Failed
                };
            }
        }

        public async Task<MessageDto?> GetMessageAsync()
        {
            if (_currentMessage == null) return null;
            
            return new MessageDto
            {
                Id = _currentMessage.id,
                SenderId = _currentMessage.sender_id,
                ReceiverId = _currentMessage.receiver_id,
                Title = _currentMessage.title,
                Content = _currentMessage.content,
                MessageType = Enum.Parse<FakeMicro.Entities.Enums.MessageType>(_currentMessage.message_type),
                Channel = Enum.Parse<FakeMicro.Entities.Enums.MessageChannel>(_currentMessage.message_channel),
                Status = Enum.Parse<FakeMicro.Entities.Enums.MessageStatus>(_currentMessage.status),
                SentAt = _currentMessage.sent_at,
                DeliveredAt = _currentMessage.delivered_at,
                ReadAt = _currentMessage.read_at,
                FailedAt = _currentMessage.failed_at,
                RetryCount = _currentMessage.retry_count,
                ErrorMessage = _currentMessage.error_message,
                Metadata = _currentMessage.metadata != null ? JsonConvert.DeserializeObject<Dictionary<string, object>>(_currentMessage.metadata) : new Dictionary<string, object>(),
                ScheduledAt = _currentMessage.scheduled_at,
                ExpiresAt = _currentMessage.expires_at,
                CreatedAt = _currentMessage.CreatedAt,
                UpdatedAt = _currentMessage.UpdatedAt.Value
            };
        }

        public async Task<bool> UpdateStatusAsync(FakeMicro.Entities.Enums.MessageStatus status, string? errorMessage = null)
        {
            if (_currentMessage == null) return false;

            var success = await _messageRepository.UpdateStatusAsync(_currentMessage.id, status, errorMessage);
            if (success)
            {
                _currentMessage.status = status.ToString();
                _currentMessage.UpdatedAt = DateTime.UtcNow;

                if (status == FakeMicro.Entities.Enums.MessageStatus.Sent)
                {
                    _currentMessage.sent_at = DateTime.UtcNow;
                }
                else if (status == FakeMicro.Entities.Enums.MessageStatus.Failed)
                {
                    _currentMessage.failed_at = DateTime.UtcNow;
                    _currentMessage.error_message = errorMessage;
                }

                _logger.LogInformation("消息状态已更新: MessageId={MessageId}, Status={Status}", 
                    _currentMessage.id, status);
            }

            return success;
        }

        public async Task<bool> MarkAsDeliveredAsync(string deliveryInfo)
        {
            if (_currentMessage == null) return false;

            var success = await _messageRepository.MarkAsDeliveredAsync(_currentMessage.id, deliveryInfo);
            if (success)
            {
                _currentMessage.status = FakeMicro.Entities.Enums.MessageStatus.Delivered.ToString();
                _currentMessage.delivered_at = DateTime.UtcNow;
                _currentMessage.UpdatedAt = DateTime.UtcNow;

                // 发布消息投递事件
                await _eventPublisher.PublishCustomEventAsync("message.delivered", new MessageDeliveredEvent
                {
                    MessageId = _currentMessage.id,
                    DeliveryInfo = deliveryInfo
                });

                _logger.LogInformation("消息已标记为已投递: MessageId={MessageId}", _currentMessage.id);
            }

            return success;
        }

        public async Task<bool> MarkAsReadAsync(string readBy)
        {
            if (_currentMessage == null) return false;

            var success = await _messageRepository.MarkAsReadAsync(_currentMessage.id, readBy);
            if (success)
            {
                _currentMessage.status = FakeMicro.Entities.Enums.MessageStatus.Read.ToString();
                _currentMessage.read_at = DateTime.UtcNow;
                _currentMessage.UpdatedAt= DateTime.UtcNow;

                // 发布消息阅读事件
                await _eventPublisher.PublishCustomEventAsync("message.read", new MessageReadEvent
                {
                    MessageId = _currentMessage.id,
                    ReadBy = readBy
                });

                _logger.LogInformation("消息已标记为已阅读: MessageId={MessageId}", _currentMessage.id);
            }

            return success;
        }

        public async Task<MessageStatistics> GetStatisticsAsync()
        {
            var statistics = await _messageRepository.GetStatisticsAsync();
            // 直接返回从仓储获取的统计信息
            return statistics;
        }
    }

    /// <summary>
    /// 消息模板Grain实现
    /// </summary>
    public class MessageTemplateGrain : OrleansGrainBase, IMessageTemplateGrain
    {
        private readonly IMessageTemplateRepository _templateRepository;
        private MessageTemplate? _currentTemplate;

        public MessageTemplateGrain(IMessageTemplateRepository templateRepository, ILogger<MessageTemplateGrain> logger) : base(logger)
        {
            _templateRepository = templateRepository;
        }

        public override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            var templateCode = this.GetPrimaryKeyString();
            _currentTemplate = await _templateRepository.GetByCodeAsync(templateCode);
            await base.OnActivateAsync(cancellationToken);
        }

        public async Task<MessageTemplateDto?> GetTemplateAsync()
        {
            if (_currentTemplate == null) return null;
            
            return new MessageTemplateDto
            {
                Id = _currentTemplate.id,
                Name = _currentTemplate.name,
                Code = _currentTemplate.code,
                Title = _currentTemplate.title,
                Content = _currentTemplate.content,
                MessageType = Enum.TryParse<FakeMicro.Entities.Enums.MessageType>(_currentTemplate.message_type, true, out var messageType) ? messageType : FakeMicro.Entities.Enums.MessageType.System,
                Variables = _currentTemplate.variables != null ? (System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(_currentTemplate.variables) ?? new Dictionary<string, string>()) : new Dictionary<string, string>(),
                IsEnabled = _currentTemplate.is_enabled,
                CreatedAt = _currentTemplate.CreatedAt,
                UpdatedAt = _currentTemplate.UpdatedAt.Value
            };
        }

        public Task<string> RenderTemplateAsync(Dictionary<string, object> variables)
        {
            if (_currentTemplate == null)
                return Task.FromResult(string.Empty);

            var content = _currentTemplate.content;
            
            // 简单的模板变量替换
            foreach (var variable in variables)
            {
                content = content.Replace($"{{{{{variable.Key}}}}}", variable.Value.ToString());
            }

            return Task.FromResult(content);
        }

        public Task<bool> ValidateVariablesAsync(Dictionary<string, object> variables)
        {
            if (_currentTemplate == null || string.IsNullOrEmpty(_currentTemplate.variables))
                return Task.FromResult(true);

            try
            {
                var requiredVariables = System.Text.Json.JsonSerializer.Deserialize<List<string>>(_currentTemplate.variables);
                if (requiredVariables == null)
                    return Task.FromResult(true);

                var missingVariables = requiredVariables.Except(variables.Keys).ToList();
                return Task.FromResult(!missingVariables.Any());
            }
            catch
            {
                return Task.FromResult(false);
            }
        }
    }

    /// <summary>
    /// 消息服务Grain实现
    /// </summary>
    public class MessageServiceGrain : OrleansGrainBase, IMessageServiceGrain
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IClusterClient _clusterClient;

        public MessageServiceGrain(IMessageRepository messageRepository, IClusterClient clusterClient, ILogger<MessageServiceGrain> logger) : base(logger)
        {
            _messageRepository = messageRepository;
            _clusterClient = clusterClient;
        }

        public async Task<BatchMessageResult> SendBatchMessagesAsync(List<MessageRequest> requests)
        {
            var startTime = DateTime.UtcNow;
            var results = new List<MessageResult>();

            foreach (var request in requests)
            {
                var messageId = Guid.NewGuid();
                var messageGrain = _clusterClient.GetGrain<IMessageGrain>(messageId.ToString());
                var result = await messageGrain.SendMessageAsync(request);
                results.Add(result);
            }

            var processingTime = DateTime.UtcNow - startTime;

            return new BatchMessageResult
            {
                Total = requests.Count,
                Success = results.Count(r => r.Success),
                Failed = results.Count(r => !r.Success),
                Results = results,
                ProcessingTime = processingTime
            };
        }

        public async Task<int> ProcessPendingMessagesAsync(int batchSize = 100)
        {
            var pendingMessages = await _messageRepository.GetPendingMessagesAsync(batchSize);
            var processedCount = 0;

            foreach (var message in pendingMessages)
            {
                try
                {
                    // 这里可以实现具体的消息发送逻辑
                    // 例如：发送邮件、短信、推送等
                    
                    // 模拟消息发送
                    await Task.Delay(100);
                    
                    // 更新消息状态为已发送
                    await _messageRepository.UpdateStatusAsync(message.id, FakeMicro.Entities.Enums.MessageStatus.Sent);
                    processedCount++;
                    
                    _logger.LogInformation("消息已发送: MessageId={MessageId}", message.id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "发送消息失败: MessageId={MessageId}", message.id);
                    await _messageRepository.UpdateStatusAsync(message.id, FakeMicro.Entities.Enums.MessageStatus.Failed, ex.Message);
                }
            }

            return processedCount;
        }

        public async Task<int> CleanupExpiredMessagesAsync()
        {
            var cleanedCount = await _messageRepository.CleanupExpiredMessagesAsync();
            _logger.LogInformation("已清理过期消息: {Count}条", cleanedCount);
            return cleanedCount;
        }

        public async Task<SystemMessageStatistics> GetSystemStatisticsAsync()
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var totalStats = await _messageRepository.GetStatisticsAsync();
            var todayStats = await _messageRepository.GetStatisticsAsync(today, now);
            var weekStats = await _messageRepository.GetStatisticsAsync(weekStart, now);
            var monthStats = await _messageRepository.GetStatisticsAsync(monthStart, now);

            return new SystemMessageStatistics
            {
                TotalMessages = totalStats.TotalMessages,
                MessagesToday = todayStats.TotalMessages,
                MessagesThisWeek = weekStats.TotalMessages,
                MessagesThisMonth = monthStats.TotalMessages,
                AverageDeliveryTime = 0, // 需要实际计算
                SuccessRate = totalStats.TotalMessages > 0 ? 
                    (double)(totalStats.SentMessages + totalStats.ReadMessages) / totalStats.TotalMessages * 100 : 0,
                TypeDistribution = totalStats.TypeStatistics.ToDictionary(kv => (FakeMicro.Entities.Enums.MessageType)kv.Key, kv => kv.Value),
                ChannelDistribution = totalStats.ChannelStatistics.ToDictionary(kv => (FakeMicro.Entities.Enums.MessageChannel)kv.Key, kv => kv.Value)
            };
        }
    }
}