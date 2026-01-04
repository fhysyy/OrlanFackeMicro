using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Orleans;

using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
using FakeMicro.Entities.Enums;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace FakeMicro.Tests.ApiTests
{
    public class MessagesControllerTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public MessagesControllerTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task SendMessage_ReturnsSuccess_WhenMessageIsValid()
        {
            var request = new SendMessageRequest
            {
                ReceiverId = 2,
                Title = "Test Message",
                Content = "This is a test message",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp
            };

            var response = await _client.PostAsJsonAsync("/api/Messages/send", request);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<MessageResponse>();

            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.MessageId);
        }

        [Fact]
        public async Task SendMessage_WithScheduledAt_ReturnsSuccess()
        {
            var request = new SendMessageRequest
            {
                ReceiverId = 2,
                Title = "Scheduled Message",
                Content = "This is a scheduled message",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp,
                ScheduledAt = DateTime.UtcNow.AddHours(1)
            };

            var response = await _client.PostAsJsonAsync("/api/Messages/send", request);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<MessageResponse>();

            Assert.NotNull(result);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task SendMessage_WithExpiresAt_ReturnsSuccess()
        {
            var request = new SendMessageRequest
            {
                ReceiverId = 2,
                Title = "Expiring Message",
                Content = "This message will expire",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            var response = await _client.PostAsJsonAsync("/api/Messages/send", request);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<MessageResponse>();

            Assert.NotNull(result);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task SendMessage_WithMetadata_ReturnsSuccess()
        {
            var request = new SendMessageRequest
            {
                ReceiverId = 2,
                Title = "Message with Metadata",
                Content = "This message has metadata",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp,
                Metadata = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "priority", "high" },
                    { "category", "notification" }
                }
            };

            var response = await _client.PostAsJsonAsync("/api/Messages/send", request);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<MessageResponse>();

            Assert.NotNull(result);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task GetMessage_ReturnsMessage_WhenMessageExists()
        {
            var sendMessageRequest = new SendMessageRequest
            {
                ReceiverId = 2,
                Title = "Test Message",
                Content = "This is a test message",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp
            };

            var sendResponse = await _client.PostAsJsonAsync("/api/Messages/send", sendMessageRequest);
            sendResponse.EnsureSuccessStatusCode();
            var sendResult = await sendResponse.Content.ReadFromJsonAsync<MessageResponse>();

            var response = await _client.GetAsync($"/api/Messages/{sendResult.MessageId}");

            response.EnsureSuccessStatusCode();
            var message = await response.Content.ReadFromJsonAsync<MessageDto>();

            Assert.NotNull(message);
            Assert.Equal("Test Message", message.Title);
            Assert.Equal("This is a test message", message.Content);
        }

        [Fact]
        public async Task GetMessage_ReturnsNotFound_WhenMessageDoesNotExist()
        {
            var messageId = Guid.NewGuid();

            var response = await _client.GetAsync($"/api/Messages/{messageId}");

            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task MarkAsRead_ReturnsSuccess_WhenMessageExists()
        {
            var sendMessageRequest = new SendMessageRequest
            {
                ReceiverId = 2,
                Title = "Test Message",
                Content = "This is a test message",
                MessageType = MessageType.Notification,
                Channel = MessageChannel.InApp
            };

            var sendResponse = await _client.PostAsJsonAsync("/api/Messages/send", sendMessageRequest);
            sendResponse.EnsureSuccessStatusCode();
            var sendResult = await sendResponse.Content.ReadFromJsonAsync<MessageResponse>();

            var response = await _client.PostAsync($"/api/Messages/{sendResult.MessageId}/read", null);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<SuccessResponse>();

            Assert.NotNull(result);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task MarkAsRead_ReturnsNotFound_WhenMessageDoesNotExist()
        {
            var messageId = Guid.NewGuid();

            var response = await _client.PostAsync($"/api/Messages/{messageId}/read", null);

            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetUserMessages_ReturnsMessages_WhenUserExists()
        {
            var userId = 1;

            var response = await _client.GetAsync($"/api/Messages/user/{userId}");

            response.EnsureSuccessStatusCode();
            var messages = await response.Content.ReadFromJsonAsync<System.Collections.Generic.List<MessageDto>>();

            Assert.NotNull(messages);
        }

        [Fact]
        public async Task GetUserMessages_WithPagination_ReturnsCorrectPage()
        {
            var userId = 1;
            var page = 1;
            var pageSize = 10;

            var response = await _client.GetAsync($"/api/Messages/user/{userId}?page={page}&pageSize={pageSize}");

            response.EnsureSuccessStatusCode();
            var messages = await response.Content.ReadFromJsonAsync<System.Collections.Generic.List<MessageDto>>();

            Assert.NotNull(messages);
        }

        [Fact]
        public async Task GetStatistics_ReturnsStatistics()
        {
            var response = await _client.GetAsync("/api/Messages/statistics");

            response.EnsureSuccessStatusCode();
            var statistics = await response.Content.ReadFromJsonAsync<MessageStatistics>();

            Assert.NotNull(statistics);
            Assert.True(statistics.TotalMessages >= 0);
            Assert.True(statistics.SentMessages >= 0);
            Assert.True(statistics.ReadMessages >= 0);
            Assert.True(statistics.FailedMessages >= 0);
            Assert.True(statistics.PendingMessages >= 0);
        }
    }
}
