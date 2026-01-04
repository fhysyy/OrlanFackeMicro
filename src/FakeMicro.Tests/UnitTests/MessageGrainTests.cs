using FakeMicro.DatabaseAccess;
using FakeMicro.Entities;
using FakeMicro.Entities.Enums;
using FakeMicro.Grains;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Events;
using FakeMicro.Interfaces.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace FakeMicro.Tests.UnitTests
{
    public class MessageGrainTests
    {
        private readonly Mock<IMessageRepository> _messageRepositoryMock;
        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly Mock<ILogger<MessageGrain>> _loggerMock;
        private readonly MessageGrain _messageGrain;

        public MessageGrainTests()
        {
            _messageRepositoryMock = new Mock<IMessageRepository>();
            _eventPublisherMock = new Mock<IEventPublisher>();
            _loggerMock = new Mock<ILogger<MessageGrain>>();

            _messageGrain = new MessageGrain(
                _messageRepositoryMock.Object,
                _eventPublisherMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task SendMessageAsync_ValidRequest_CreatesMessageAndReturnsSuccess()
        {
            var request = new MessageRequest
            {
                SenderId = 1,
                ReceiverId = 2,
                Title = "Test Message",
                Content = "Test content",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp,
                Metadata = new Dictionary<string, object> { { "key", "value" } }
            };

            var expectedMessage = new Message
            {
                id = 1234567890,
                sender_id = 1,
                receiver_id = 2,
                title = "Test Message",
                content = "Test content",
                message_type = MessageType.Notification.ToString(),
                message_channel = MessageChannel.InApp.ToString(),
                status = MessageStatus.Pending.ToString(),
                metadata = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object> { { "key", "value" } })
            };

            _messageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Message>()))
                .ReturnsAsync(expectedMessage);

            _eventPublisherMock
                .Setup(x => x.PublishCustomEventAsync(It.IsAny<string>(), It.IsAny<MessageSentEvent>()))
                .Returns(Task.CompletedTask);

            var result = await _messageGrain.SendMessageAsync(request);

            Assert.True(result.Success);
            Assert.Equal(1234567890, result.MessageId);
            Assert.Equal(MessageStatus.Pending, result.Status);
            _messageRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Message>()), Times.Once);
            _eventPublisherMock.Verify(x => x.PublishCustomEventAsync("message.sent", It.IsAny<MessageSentEvent>()), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_RepositoryThrowsException_ReturnsFailure()
        {
            var request = new MessageRequest
            {
                SenderId = 1,
                ReceiverId = 2,
                Title = "Test Message",
                Content = "Test content",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp
            };

            _messageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Message>()))
                .ThrowsAsync(new Exception("Database error"));

            var result = await _messageGrain.SendMessageAsync(request);

            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
            Assert.Equal(MessageStatus.Failed, result.Status);
            _eventPublisherMock.Verify(x => x.PublishCustomEventAsync(It.IsAny<string>(), It.IsAny<MessageSentEvent>()), Times.Never);
        }

        [Fact]
        public async Task GetMessageAsync_CurrentMessageExists_ReturnsMessageDto()
        {
            var message = new Message
            {
                id = 1234567890,
                sender_id = 1,
                receiver_id = 2,
                title = "Test Message",
                content = "Test content",
                message_type = MessageType.Notification.ToString(),
                message_channel = MessageChannel.InApp.ToString(),
                status = MessageStatus.Sent.ToString(),
                sent_at = DateTime.UtcNow,
                delivered_at = DateTime.UtcNow.AddMinutes(1),
                read_at = DateTime.UtcNow.AddMinutes(2),
                retry_count = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _messageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Message>()))
                .ReturnsAsync(message);

            await _messageGrain.SendMessageAsync(new MessageRequest
            {
                SenderId = 1,
                ReceiverId = 2,
                Title = "Test Message",
                Content = "Test content",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp
            });

            var result = await _messageGrain.GetMessageAsync();

            Assert.NotNull(result);
            Assert.Equal(1234567890, result.Id);
            Assert.Equal(1, result.SenderId);
            Assert.Equal(2, result.ReceiverId);
            Assert.Equal("Test Message", result.Title);
            Assert.Equal("Test content", result.Content);
            Assert.Equal(MessageType.Notification, result.MessageType);
            Assert.Equal(MessageChannel.InApp, result.Channel);
            Assert.Equal(MessageStatus.Sent, result.Status);
        }

        [Fact]
        public async Task GetMessageAsync_CurrentMessageIsNull_ReturnsNull()
        {
            var result = await _messageGrain.GetMessageAsync();

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateStatusAsync_CurrentMessageExists_UpdatesStatusAndReturnsSuccess()
        {
            var message = new Message
            {
                id = 1234567890,
                sender_id = 1,
                receiver_id = 2,
                title = "Test Message",
                content = "Test content",
                message_type = MessageType.Notification.ToString(),
                message_channel = MessageChannel.InApp.ToString(),
                status = MessageStatus.Pending.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _messageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Message>()))
                .ReturnsAsync(message);

            _messageRepositoryMock
                .Setup(x => x.UpdateStatusAsync(It.IsAny<long>(), It.IsAny<MessageStatus>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            await _messageGrain.SendMessageAsync(new MessageRequest
            {
                SenderId = 1,
                ReceiverId = 2,
                Title = "Test Message",
                Content = "Test content",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp
            });

            var result = await _messageGrain.UpdateStatusAsync(MessageStatus.Sent);

            Assert.True(result);
            _messageRepositoryMock.Verify(x => x.UpdateStatusAsync(1234567890, MessageStatus.Sent, null), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_WithErrorMessage_UpdatesStatusAndErrorMessage()
        {
            var message = new Message
            {
                id = 1234567890,
                sender_id = 1,
                receiver_id = 2,
                title = "Test Message",
                content = "Test content",
                message_type = MessageType.Notification.ToString(),
                message_channel = MessageChannel.InApp.ToString(),
                status = MessageStatus.Pending.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _messageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Message>()))
                .ReturnsAsync(message);

            _messageRepositoryMock
                .Setup(x => x.UpdateStatusAsync(It.IsAny<long>(), It.IsAny<MessageStatus>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            await _messageGrain.SendMessageAsync(new MessageRequest
            {
                SenderId = 1,
                ReceiverId = 2,
                Title = "Test Message",
                Content = "Test content",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp
            });

            var result = await _messageGrain.UpdateStatusAsync(MessageStatus.Failed, "Delivery failed");

            Assert.True(result);
            _messageRepositoryMock.Verify(x => x.UpdateStatusAsync(1234567890, MessageStatus.Failed, "Delivery failed"), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_CurrentMessageIsNull_ReturnsFalse()
        {
            var result = await _messageGrain.UpdateStatusAsync(MessageStatus.Sent);

            Assert.False(result);
            _messageRepositoryMock.Verify(x => x.UpdateStatusAsync(It.IsAny<long>(), It.IsAny<MessageStatus>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateStatusAsync_RepositoryUpdateFails_ReturnsFalse()
        {
            var message = new Message
            {
                id = 1234567890,
                sender_id = 1,
                receiver_id = 2,
                title = "Test Message",
                content = "Test content",
                message_type = MessageType.Notification.ToString(),
                message_channel = MessageChannel.InApp.ToString(),
                status = MessageStatus.Pending.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _messageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Message>()))
                .ReturnsAsync(message);

            _messageRepositoryMock
                .Setup(x => x.UpdateStatusAsync(It.IsAny<long>(), It.IsAny<MessageStatus>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            await _messageGrain.SendMessageAsync(new MessageRequest
            {
                SenderId = 1,
                ReceiverId = 2,
                Title = "Test Message",
                Content = "Test content",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp
            });

            var result = await _messageGrain.UpdateStatusAsync(MessageStatus.Sent);

            Assert.False(result);
        }

        [Fact]
        public async Task MarkAsDeliveredAsync_CurrentMessageExists_MarksAsDeliveredAndPublishesEvent()
        {
            var message = new Message
            {
                id = 1234567890,
                sender_id = 1,
                receiver_id = 2,
                title = "Test Message",
                content = "Test content",
                message_type = MessageType.Notification.ToString(),
                message_channel = MessageChannel.InApp.ToString(),
                status = MessageStatus.Pending.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _messageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Message>()))
                .ReturnsAsync(message);

            _messageRepositoryMock
                .Setup(x => x.MarkAsDeliveredAsync(It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            _eventPublisherMock
                .Setup(x => x.PublishCustomEventAsync(It.IsAny<string>(), It.IsAny<MessageDeliveredEvent>()))
                .Returns(Task.CompletedTask);

            await _messageGrain.SendMessageAsync(new MessageRequest
            {
                SenderId = 1,
                ReceiverId = 2,
                Title = "Test Message",
                Content = "Test content",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp
            });

            var result = await _messageGrain.MarkAsDeliveredAsync("delivery-info-123");

            Assert.True(result);
            _messageRepositoryMock.Verify(x => x.MarkAsDeliveredAsync(1234567890, "delivery-info-123"), Times.Once);
            _eventPublisherMock.Verify(x => x.PublishCustomEventAsync("message.delivered", It.IsAny<MessageDeliveredEvent>()), Times.Once);
        }

        [Fact]
        public async Task MarkAsDeliveredAsync_CurrentMessageIsNull_ReturnsFalse()
        {
            var result = await _messageGrain.MarkAsDeliveredAsync("delivery-info-123");

            Assert.False(result);
            _messageRepositoryMock.Verify(x => x.MarkAsDeliveredAsync(It.IsAny<long>(), It.IsAny<string>()), Times.Never);
            _eventPublisherMock.Verify(x => x.PublishCustomEventAsync(It.IsAny<string>(), It.IsAny<MessageDeliveredEvent>()), Times.Never);
        }

        [Fact]
        public async Task MarkAsReadAsync_CurrentMessageExists_MarksAsReadAndPublishesEvent()
        {
            var message = new Message
            {
                id = 1234567890,
                sender_id = 1,
                receiver_id = 2,
                title = "Test Message",
                content = "Test content",
                message_type = MessageType.Notification.ToString(),
                message_channel = MessageChannel.InApp.ToString(),
                status = MessageStatus.Delivered.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _messageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Message>()))
                .ReturnsAsync(message);

            _messageRepositoryMock
                .Setup(x => x.MarkAsReadAsync(It.IsAny<long>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            _eventPublisherMock
                .Setup(x => x.PublishCustomEventAsync(It.IsAny<string>(), It.IsAny<MessageReadEvent>()))
                .Returns(Task.CompletedTask);

            await _messageGrain.SendMessageAsync(new MessageRequest
            {
                SenderId = 1,
                ReceiverId = 2,
                Title = "Test Message",
                Content = "Test content",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp
            });

            var result = await _messageGrain.MarkAsReadAsync("user-123");

            Assert.True(result);
            _messageRepositoryMock.Verify(x => x.MarkAsReadAsync(1234567890, "user-123"), Times.Once);
            _eventPublisherMock.Verify(x => x.PublishCustomEventAsync("message.read", It.IsAny<MessageReadEvent>()), Times.Once);
        }

        [Fact]
        public async Task MarkAsReadAsync_CurrentMessageIsNull_ReturnsFalse()
        {
            var result = await _messageGrain.MarkAsReadAsync("user-123");

            Assert.False(result);
            _messageRepositoryMock.Verify(x => x.MarkAsReadAsync(It.IsAny<long>(), It.IsAny<string>()), Times.Never);
            _eventPublisherMock.Verify(x => x.PublishCustomEventAsync(It.IsAny<string>(), It.IsAny<MessageReadEvent>()), Times.Never);
        }

        [Fact]
        public async Task GetStatisticsAsync_ReturnsStatisticsFromRepository()
        {
            var expectedStatistics = new MessageStatistics
            {
                TotalMessages = 100,
                SentMessages = 80,
                ReadMessages = 70,
                FailedMessages = 5,
                PendingMessages = 20,
                TypeStatistics = new Dictionary<MessageType, int> { { MessageType.Notification, 50 }, { MessageType.Marketing, 30 } },
                ChannelStatistics = new Dictionary<MessageChannel, int> { { MessageChannel.InApp, 60 }, { MessageChannel.Email, 40 } }
            };

            _messageRepositoryMock
                .Setup(x => x.GetStatisticsAsync())
                .ReturnsAsync(expectedStatistics);

            var result = await _messageGrain.GetStatisticsAsync();

            Assert.NotNull(result);
            Assert.Equal(100, result.TotalMessages);
            Assert.Equal(80, result.SentMessages);
            Assert.Equal(70, result.ReadMessages);
            Assert.Equal(5, result.FailedMessages);
            Assert.Equal(20, result.PendingMessages);
            Assert.Equal(2, result.TypeStatistics.Count);
            Assert.Equal(2, result.ChannelStatistics.Count);
            _messageRepositoryMock.Verify(x => x.GetStatisticsAsync(), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_WithScheduledAt_CreatesMessageWithScheduledTime()
        {
            var scheduledTime = DateTime.UtcNow.AddHours(1);
            var request = new MessageRequest
            {
                SenderId = 1,
                ReceiverId = 2,
                Title = "Test Message",
                Content = "Test content",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp,
                ScheduledAt = scheduledTime
            };

            var expectedMessage = new Message
            {
                id = 1234567890,
                sender_id = 1,
                receiver_id = 2,
                title = "Test Message",
                content = "Test content",
                message_type = MessageType.Notification.ToString(),
                message_channel = MessageChannel.InApp.ToString(),
                status = MessageStatus.Pending.ToString(),
                scheduled_at = scheduledTime
            };

            _messageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Message>()))
                .ReturnsAsync(expectedMessage);

            _eventPublisherMock
                .Setup(x => x.PublishCustomEventAsync(It.IsAny<string>(), It.IsAny<MessageSentEvent>()))
                .Returns(Task.CompletedTask);

            var result = await _messageGrain.SendMessageAsync(request);

            Assert.True(result.Success);
            _messageRepositoryMock.Verify(x => x.AddAsync(It.Is<Message>(m => m.scheduled_at == scheduledTime)), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_WithExpiresAt_CreatesMessageWithExpiryTime()
        {
            var expiresAt = DateTime.UtcNow.AddHours(24);
            var request = new MessageRequest
            {
                SenderId = 1,
                ReceiverId = 2,
                Title = "Test Message",
                Content = "Test content",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp,
                ExpiresAt = expiresAt
            };

            var expectedMessage = new Message
            {
                id = 1234567890,
                sender_id = 1,
                receiver_id = 2,
                title = "Test Message",
                content = "Test content",
                message_type = MessageType.Notification.ToString(),
                message_channel = MessageChannel.InApp.ToString(),
                status = MessageStatus.Pending.ToString(),
                expires_at = expiresAt
            };

            _messageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Message>()))
                .ReturnsAsync(expectedMessage);

            _eventPublisherMock
                .Setup(x => x.PublishCustomEventAsync(It.IsAny<string>(), It.IsAny<MessageSentEvent>()))
                .Returns(Task.CompletedTask);

            var result = await _messageGrain.SendMessageAsync(request);

            Assert.True(result.Success);
            _messageRepositoryMock.Verify(x => x.AddAsync(It.Is<Message>(m => m.expires_at == expiresAt)), Times.Once);
        }

        [Fact]
        public async Task SendMessageAsync_WithNullMetadata_CreatesMessageWithNullMetadata()
        {
            var request = new MessageRequest
            {
                SenderId = 1,
                ReceiverId = 2,
                Title = "Test Message",
                Content = "Test content",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp,
                Metadata = null
            };

            var expectedMessage = new Message
            {
                id = 1234567890,
                sender_id = 1,
                receiver_id = 2,
                title = "Test Message",
                content = "Test content",
                message_type = MessageType.Notification.ToString(),
                message_channel = MessageChannel.InApp.ToString(),
                status = MessageStatus.Pending.ToString(),
                metadata = null
            };

            _messageRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Message>()))
                .ReturnsAsync(expectedMessage);

            _eventPublisherMock
                .Setup(x => x.PublishCustomEventAsync(It.IsAny<string>(), It.IsAny<MessageSentEvent>()))
                .Returns(Task.CompletedTask);

            var result = await _messageGrain.SendMessageAsync(request);

            Assert.True(result.Success);
            _messageRepositoryMock.Verify(x => x.AddAsync(It.Is<Message>(m => m.metadata == null)), Times.Once);
        }
    }
}
