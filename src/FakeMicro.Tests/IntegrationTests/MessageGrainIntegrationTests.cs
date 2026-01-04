using Microsoft.Extensions.Logging;
using Moq;
using Orleans;
using Orleans.TestingHost;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
using FakeMicro.Entities.Enums;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FakeMicro.Tests.IntegrationTests
{
    public class MessageGrainIntegrationTests : IClassFixture<TestClusterFixture>
    {
        private readonly TestClusterFixture _fixture;

        public MessageGrainIntegrationTests(TestClusterFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task MessageGrain_SendMessage_Success()
        {
            var grainId = Guid.NewGuid().ToString();
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IMessageGrain>(grainId);

            var request = new MessageRequest
            {
                SenderId = 1,
                ReceiverId = 2,
                Title = "Test Message",
                Content = "This is a test message",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp
            };

            var result = await grain.SendMessageAsync(request);

            Assert.True(result.Success);
            Assert.NotEqual(0, result.MessageId);
            Assert.Equal(MessageStatus.Pending, result.Status);
        }

        [Fact]
        public async Task MessageGrain_SendAndGetMessage_Success()
        {
            var grainId = Guid.NewGuid().ToString();
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IMessageGrain>(grainId);

            var request = new MessageRequest
            {
                SenderId = 1,
                ReceiverId = 2,
                Title = "Test Message",
                Content = "This is a test message",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp
            };

            await grain.SendMessageAsync(request);
            var message = await grain.GetMessageAsync();

            Assert.NotNull(message);
            Assert.Equal(1, message.SenderId);
            Assert.Equal(2, message.ReceiverId);
            Assert.Equal("Test Message", message.Title);
            Assert.Equal("This is a test message", message.Content);
            Assert.Equal(MessageType.Notification, message.MessageType);
            Assert.Equal(MessageChannel.InApp, message.Channel);
        }

        [Fact]
        public async Task MessageGrain_UpdateStatus_Success()
        {
            var grainId = Guid.NewGuid().ToString();
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IMessageGrain>(grainId);

            var request = new MessageRequest
            {
                SenderId = 1,
                ReceiverId = 2,
                Title = "Test Message",
                Content = "This is a test message",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp
            };

            await grain.SendMessageAsync(request);
            var result = await grain.UpdateStatusAsync(MessageStatus.Sent);

            Assert.True(result);

            var message = await grain.GetMessageAsync();
            Assert.NotNull(message);
            Assert.Equal(MessageStatus.Sent, message.Status);
        }

        [Fact]
        public async Task MessageGrain_MarkAsDelivered_Success()
        {
            var grainId = Guid.NewGuid().ToString();
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IMessageGrain>(grainId);

            var request = new MessageRequest
            {
                SenderId = 1,
                ReceiverId = 2,
                Title = "Test Message",
                Content = "This is a test message",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp
            };

            await grain.SendMessageAsync(request);
            await grain.UpdateStatusAsync(MessageStatus.Sent);

            var result = await grain.MarkAsDeliveredAsync("delivery-info-123");

            Assert.True(result);

            var message = await grain.GetMessageAsync();
            Assert.NotNull(message);
            Assert.Equal(MessageStatus.Delivered, message.Status);
            Assert.NotNull(message.DeliveredAt);
        }

        [Fact]
        public async Task MessageGrain_MarkAsRead_Success()
        {
            var grainId = Guid.NewGuid().ToString();
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IMessageGrain>(grainId);

            var request = new MessageRequest
            {
                SenderId = 1,
                ReceiverId = 2,
                Title = "Test Message",
                Content = "This is a test message",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp
            };

            await grain.SendMessageAsync(request);
            await grain.UpdateStatusAsync(MessageStatus.Sent);
            await grain.MarkAsDeliveredAsync("delivery-info-123");

            var result = await grain.MarkAsReadAsync("user-123");

            Assert.True(result);

            var message = await grain.GetMessageAsync();
            Assert.NotNull(message);
            Assert.Equal(MessageStatus.Read, message.Status);
            Assert.NotNull(message.ReadAt);
        }

        [Fact]
        public async Task MessageGrain_UpdateStatusToFailed_WithErrorMessage()
        {
            var grainId = Guid.NewGuid().ToString();
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IMessageGrain>(grainId);

            var request = new MessageRequest
            {
                SenderId = 1,
                ReceiverId = 2,
                Title = "Test Message",
                Content = "This is a test message",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp
            };

            await grain.SendMessageAsync(request);
            var result = await grain.UpdateStatusAsync(MessageStatus.Failed, "Delivery failed");

            Assert.True(result);

            var message = await grain.GetMessageAsync();
            Assert.NotNull(message);
            Assert.Equal(MessageStatus.Failed, message.Status);
            Assert.Equal("Delivery failed", message.ErrorMessage);
        }

        [Fact]
        public async Task MessageGrain_SendMessageWithMetadata_Success()
        {
            var grainId = Guid.NewGuid().ToString();
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IMessageGrain>(grainId);

            var request = new MessageRequest
            {
                SenderId = 1,
                ReceiverId = 2,
                Title = "Test Message",
                Content = "This is a test message",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp,
                Metadata = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "priority", "high" },
                    { "category", "notification" }
                }
            };

            await grain.SendMessageAsync(request);
            var message = await grain.GetMessageAsync();

            Assert.NotNull(message);
            Assert.NotNull(message.Metadata);
            Assert.True(message.Metadata.ContainsKey("priority"));
            Assert.True(message.Metadata.ContainsKey("category"));
        }

        [Fact]
        public async Task MessageGrain_SendMessageWithScheduledAt_Success()
        {
            var grainId = Guid.NewGuid().ToString();
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IMessageGrain>(grainId);

            var scheduledTime = DateTime.UtcNow.AddHours(1);
            var request = new MessageRequest
            {
                SenderId = 1,
                ReceiverId = 2,
                Title = "Test Message",
                Content = "This is a test message",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp,
                ScheduledAt = scheduledTime
            };

            await grain.SendMessageAsync(request);
            var message = await grain.GetMessageAsync();

            Assert.NotNull(message);
            Assert.NotNull(message.ScheduledAt);
            Assert.Equal(scheduledTime, message.ScheduledAt);
        }

        [Fact]
        public async Task MessageGrain_SendMessageWithExpiresAt_Success()
        {
            var grainId = Guid.NewGuid().ToString();
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IMessageGrain>(grainId);

            var expiresAt = DateTime.UtcNow.AddHours(24);
            var request = new MessageRequest
            {
                SenderId = 1,
                ReceiverId = 2,
                Title = "Test Message",
                Content = "This is a test message",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp,
                ExpiresAt = expiresAt
            };

            await grain.SendMessageAsync(request);
            var message = await grain.GetMessageAsync();

            Assert.NotNull(message);
            Assert.NotNull(message.ExpiresAt);
            Assert.Equal(expiresAt, message.ExpiresAt);
        }

        [Fact]
        public async Task MessageGrain_GetStatistics_ReturnsStatistics()
        {
            var grainId = Guid.NewGuid().ToString();
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IMessageGrain>(grainId);

            var statistics = await grain.GetStatisticsAsync();

            Assert.NotNull(statistics);
            Assert.True(statistics.TotalMessages >= 0);
            Assert.True(statistics.SentMessages >= 0);
            Assert.True(statistics.ReadMessages >= 0);
            Assert.True(statistics.FailedMessages >= 0);
            Assert.True(statistics.PendingMessages >= 0);
        }

        [Fact]
        public async Task MessageGrain_MultipleMessagesWithDifferentTypes_Success()
        {
            var grainId1 = Guid.NewGuid().ToString();
            var grainId2 = Guid.NewGuid().ToString();
            var grainId3 = Guid.NewGuid().ToString();

            var grain1 = _fixture.Cluster.GrainFactory.GetGrain<IMessageGrain>(grainId1);
            var grain2 = _fixture.Cluster.GrainFactory.GetGrain<IMessageGrain>(grainId2);
            var grain3 = _fixture.Cluster.GrainFactory.GetGrain<IMessageGrain>(grainId3);

            await grain1.SendMessageAsync(new MessageRequest
            {
                SenderId = 1,
                ReceiverId = 2,
                Title = "Notification",
                Content = "Test notification",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp
            });

            await grain2.SendMessageAsync(new MessageRequest
            {
                SenderId = 1,
                ReceiverId = 2,
                Title = "Marketing",
                Content = "Test marketing",
                MessageType = MessageType.Marketing,
                Channel = MessageChannel.Email
            });

            await grain3.SendMessageAsync(new MessageRequest
            {
                SenderId = 1,
                ReceiverId = 2,
                Title = "System",
                Content = "Test system",
                MessageType = MessageType.System,
                Channel = MessageChannel.SMS
            });

            var message1 = await grain1.GetMessageAsync();
            var message2 = await grain2.GetMessageAsync();
            var message3 = await grain3.GetMessageAsync();

            Assert.Equal(MessageType.Notification, message1.MessageType);
            Assert.Equal(MessageType.Marketing, message2.MessageType);
            Assert.Equal(MessageType.System, message3.MessageType);

            Assert.Equal(MessageChannel.InApp, message1.Channel);
            Assert.Equal(MessageChannel.Email, message2.Channel);
            Assert.Equal(MessageChannel.SMS, message3.Channel);
        }
    }
}
