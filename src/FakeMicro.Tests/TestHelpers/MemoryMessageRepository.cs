using FakeMicro.DatabaseAccess;
using FakeMicro.Entities;
using FakeMicro.Entities.Enums;
using FakeMicro.Interfaces.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FakeMicro.Tests.TestHelpers
{
    public class MemoryMessageRepository : IMessageRepository
    {
        private readonly List<Message> _messages = new List<Message>();
        private long _nextId = 1;

        public Task<Message?> GetByIdAsync(long id)
        {
            return Task.FromResult(_messages.FirstOrDefault(m => m.id == id));
        }

        public Task<List<Message>> GetUserMessagesAsync(long userId, int page = 1, int pageSize = 20)
        {
            return Task.FromResult(_messages
                .Where(m => m.receiver_id == userId)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList());
        }

        public Task<List<Message>> GetPendingMessagesAsync(int batchSize = 100)
        {
            return Task.FromResult(_messages
                .Where(m => m.status == MessageStatus.Pending.ToString())
                .Take(batchSize)
                .ToList());
        }

        public Task<Message> AddAsync(Message message)
        {
            message.id = _nextId++;
            message.CreatedAt = DateTime.UtcNow;
            message.UpdatedAt = DateTime.UtcNow;
            _messages.Add(message);
            return Task.FromResult(message);
        }

        public Task<Message> UpdateAsync(Message message)
        {
            var existingMessage = _messages.FirstOrDefault(m => m.id == message.id);
            if (existingMessage == null)
            {
                throw new ArgumentException($"Message with id {message.id} not found");
            }

            // 更新现有消息的所有属性
            _messages.Remove(existingMessage);
            message.UpdatedAt = DateTime.UtcNow;
            _messages.Add(message);
            return Task.FromResult(message);
        }

        public Task<bool> UpdateStatusAsync(long messageId, MessageStatus status, string? errorMessage = null)
        {
            var message = _messages.FirstOrDefault(m => m.id == messageId);
            if (message == null)
            {
                return Task.FromResult(false);
            }

            message.status = status.ToString();
            message.error_message = errorMessage;
            message.UpdatedAt = DateTime.UtcNow;
            return Task.FromResult(true);
        }

        public Task<bool> MarkAsDeliveredAsync(long messageId, string deliveryInfo)
        {
            var message = _messages.FirstOrDefault(m => m.id == messageId);
            if (message == null)
            {
                return Task.FromResult(false);
            }

            message.status = MessageStatus.Delivered.ToString();
            // 注意：Message实体没有DeliveryInfo属性
            message.delivered_at = DateTime.UtcNow;
            message.UpdatedAt = DateTime.UtcNow;
            return Task.FromResult(true);
        }

        public Task<bool> MarkAsReadAsync(long messageId, string readBy)
        {
            var message = _messages.FirstOrDefault(m => m.id == messageId);
            if (message == null)
            {
                return Task.FromResult(false);
            }

            message.read_at = DateTime.UtcNow;
            // 注意：Message实体没有ReadBy属性
            message.UpdatedAt = DateTime.UtcNow;
            return Task.FromResult(true);
        }

        public Task<MessageStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var filteredMessages = _messages.AsQueryable();
            if (startDate.HasValue)
            {
                filteredMessages = filteredMessages.Where(m => m.CreatedAt >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                filteredMessages = filteredMessages.Where(m => m.CreatedAt <= endDate.Value);
            }

            var statistics = new MessageStatistics
            {
                TotalMessages = filteredMessages.Count(),
                StartTime = startDate ?? filteredMessages.Min(m => m.CreatedAt),
                EndTime = endDate ?? DateTime.UtcNow,
                PendingMessages = filteredMessages.Count(m => m.status == MessageStatus.Pending.ToString()),
                FailedMessages = filteredMessages.Count(m => m.status == MessageStatus.Failed.ToString()),
                ReadMessages = filteredMessages.Count(m => m.read_at.HasValue),
                StatusStatistics = filteredMessages
                    .GroupBy(m => (MessageStatus)Enum.Parse(typeof(MessageStatus), m.status))
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return Task.FromResult(statistics);
        }

        public Task<int> CleanupExpiredMessagesAsync()
        {
            var expiredMessages = _messages.Where(m => m.expires_at.HasValue && m.expires_at.Value < DateTime.UtcNow).ToList();
            foreach (var message in expiredMessages)
            {
                _messages.Remove(message);
            }
            return Task.FromResult(expiredMessages.Count);
        }
    }
}