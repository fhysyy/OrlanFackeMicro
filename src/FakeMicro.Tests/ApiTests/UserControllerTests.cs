using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;

using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
using FakeMicro.Entities.Enums;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace FakeMicro.Tests.ApiTests
{
    public class UserControllerTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public UserControllerTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetUser_ReturnsUser_WhenUserExists()
        {
            var userId = "test-user-1";
            var clusterClient = _factory.Server.Services.GetRequiredService<IClusterClient>();
            var userGrain = clusterClient.GetGrain<IUserGrain>(userId);

            await userGrain.SetNicknameAsync("Test User");
            await userGrain.SetEmailAsync("test@example.com");

            var response = await _client.GetAsync($"/api/User/{userId}");

            response.EnsureSuccessStatusCode();
            var user = await response.Content.ReadFromJsonAsync<UserDto>();

            Assert.NotNull(user);
            Assert.Equal(userId, user.Id.ToString());
        }

        [Fact]
        public async Task GetUser_ReturnsNotFound_WhenUserDoesNotExist()
        {
            var userId = "nonexistent-user";

            var response = await _client.GetAsync($"/api/User/{userId}");

            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateUser_ReturnsOk_WhenUpdateSuccessful()
        {
            var userId = "test-user-2";
            var clusterClient = _factory.Server.Services.GetRequiredService<IClusterClient>();
            var userGrain = clusterClient.GetGrain<IUserGrain>(userId);

            await userGrain.SetNicknameAsync("Original Name");

            var userDto = new UserDto
            {
                Id = long.Parse(userId),
                Username = "updateduser",
                Email = "updated@example.com",
                DisplayName = "Updated Name",
                Status = UserStatus.Online
            };

            var response = await _client.PutAsJsonAsync($"/api/User/{userId}", userDto);

            response.EnsureSuccessStatusCode();

            var updatedUser = await userGrain.GetUserAsync();
            Assert.Equal("Updated Name", updatedUser.DisplayName);
        }

        [Fact]
        public async Task GetUserPermissions_ReturnsPermissions_WhenUserExists()
        {
            var userId = "test-user-3";
            var clusterClient = _factory.Server.Services.GetRequiredService<IClusterClient>();
            var userGrain = clusterClient.GetGrain<IUserGrain>(userId);

            var permission = new Permission
            {
                Resource = "test-resource",
                Type =PermissionType.Manage,
            };

            await userGrain.AddPermissionAsync(permission);

            var response = await _client.GetAsync($"/api/User/{userId}/permissions");

            response.EnsureSuccessStatusCode();
            var permissions = await response.Content.ReadFromJsonAsync<System.Collections.Generic.List<PermissionDto>>();

            Assert.NotNull(permissions);
            Assert.NotEmpty(permissions);
        }

        [Fact]
        public async Task GetUserPermissions_ReturnsEmptyList_WhenUserHasNoPermissions()
        {
            var userId = "test-user-4";
            var clusterClient = _factory.Server.Services.GetRequiredService<IClusterClient>();
            var userGrain = clusterClient.GetGrain<IUserGrain>(userId);

            var response = await _client.GetAsync($"/api/User/{userId}/permissions");

            response.EnsureSuccessStatusCode();
            var permissions = await response.Content.ReadFromJsonAsync<System.Collections.Generic.List<PermissionDto>>();

            Assert.NotNull(permissions);
            Assert.Empty(permissions);
        }
    }
}
