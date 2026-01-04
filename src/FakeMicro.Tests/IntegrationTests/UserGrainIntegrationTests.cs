using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
using FakeMicro.Entities;
using FakeMicro.Entities.Enums;
using System;
using System.Threading.Tasks;
using Xunit;

namespace FakeMicro.Tests.IntegrationTests
{
    public class UserGrainIntegrationTests : IClassFixture<TestClusterFixture>
    {
        private readonly TestClusterFixture _fixture;

        public UserGrainIntegrationTests(TestClusterFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task UserGrain_SetAndGetNickname_Success()
        {
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IUserGrain>("test-user-1");

            await grain.SetNicknameAsync("Test Nickname");
            var nickname = await grain.GetNicknameAsync();

            Assert.Equal("Test Nickname", nickname);
        }

        [Fact]
        public async Task UserGrain_SetAndGetAvatar_Success()
        {
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IUserGrain>("test-user-2");

            var avatarUrl = "https://example.com/avatar.jpg";
            await grain.SetAvatarAsync(avatarUrl);
            var result = await grain.GetAvatarAsync();

            Assert.Equal(avatarUrl, result);
        }

        [Fact]
        public async Task UserGrain_GetProfile_ReturnsCompleteProfile()
        {
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IUserGrain>("test-user-3");

            var profile = await grain.GetProfileAsync();

            Assert.NotNull(profile);
            Assert.True(profile.ContainsKey("userId"));
            Assert.True(profile.ContainsKey("username"));
            Assert.True(profile.ContainsKey("email"));
            Assert.True(profile.ContainsKey("displayName"));
        }

        [Fact]
        public async Task UserGrain_UpdateProfile_UpdatesProfileSuccessfully()
        {
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IUserGrain>("test-user-4");

            var profile = new System.Collections.Generic.Dictionary<string, object>
            {
                { "username", "updateduser" },
                { "email", "updated@example.com" },
                { "displayName", "Updated Display Name" }
            };

            await grain.UpdateProfileAsync(profile);

            var updatedProfile = await grain.GetProfileAsync();
            Assert.Equal("updateduser", updatedProfile["username"]);
            Assert.Equal("updated@example.com", updatedProfile["email"]);
            Assert.Equal("Updated Display Name", updatedProfile["displayName"]);
        }

        [Fact]
        public async Task UserGrain_CreateAndGetSession_Success()
        {
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IUserGrain>("test-user-5");

            var session = new UserSession
            {
                SessionId = "test-session-1",
                IpAddress = "192.168.1.1",
                UserAgent = "Mozilla/5.0",
                IsCurrent = true
            };

            await grain.CreateSessionAsync(session);

            var sessions = await grain.GetSessionsAsync();
            Assert.Single(sessions);
            Assert.Equal("test-session-1", sessions[0].SessionId);
        }

        [Fact]
        public async Task UserGrain_TerminateSession_RemovesSessionSuccessfully()
        {
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IUserGrain>("test-user-6");

            var session1 = new UserSession
            {
                SessionId = "session-1",
                IpAddress = "192.168.1.1",
                UserAgent = "Mozilla/5.0",
                IsCurrent = true
            };

            var session2 = new UserSession
            {
                SessionId = "session-2",
                IpAddress = "192.168.1.2",
                UserAgent = "Chrome/120.0",
                IsCurrent = false
            };

            await grain.CreateSessionAsync(session1);
            await grain.CreateSessionAsync(session2);

            var result = await grain.TerminateSessionAsync("session-2");

            Assert.True(result.Success);
            Assert.Equal(1, result.TerminatedCount);

            var sessions = await grain.GetSessionsAsync();
            Assert.Single(sessions);
            Assert.Equal("session-1", sessions[0].SessionId);
        }

        [Fact]
        public async Task UserGrain_GenerateAndValidateRefreshToken_Success()
        {
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IUserGrain>("test-user-7");

            var token = await grain.GenerateRefreshTokenAsync();
            Assert.NotNull(token);
            Assert.NotEmpty(token);

            var isValid = await grain.ValidateRefreshTokenAsync(token);
            Assert.True(isValid);
        }

        [Fact]
        public async Task UserGrain_ValidateInvalidRefreshToken_ReturnsFalse()
        {
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IUserGrain>("test-user-8");

            var isValid = await grain.ValidateRefreshTokenAsync("invalid-token");
            Assert.False(isValid);
        }

        [Fact]
        public async Task UserGrain_SetAndGetStatus_Success()
        {
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IUserGrain>("test-user-9");

            await grain.SetStatusAsync(UserStatus.Online);
            var status = await grain.GetStatusAsync();

            Assert.Equal(UserStatus.Online, status);
        }

        [Fact]
        public async Task UserGrain_AddAndRemoveFriend_Success()
        {
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IUserGrain>("test-user-10");

            await grain.AddFriendAsync(12345);
            var friends = await grain.GetFriendsAsync();
            Assert.Contains(12345, friends);

            await grain.RemoveFriendAsync(12345);
            friends = await grain.GetFriendsAsync();
            Assert.DoesNotContain(12345, friends);
        }

        [Fact]
        public async Task UserGrain_BlockAndUnblockUser_Success()
        {
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IUserGrain>("test-user-11");

            await grain.BlockUserAsync(54321);
            var blockedUsers = await grain.GetBlockedUsersAsync();
            Assert.Contains(54321, blockedUsers);

            await grain.UnblockUserAsync(54321);
            blockedUsers = await grain.GetBlockedUsersAsync();
            Assert.DoesNotContain(54321, blockedUsers);
        }

        [Fact]
        public async Task UserGrain_UpdateAndGetSettings_Success()
        {
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IUserGrain>("test-user-12");

            await grain.UpdateSettingAsync("theme", "dark");
            await grain.UpdateSettingAsync("language", "zh-CN");

            var settings = await grain.GetSettingsAsync();
            Assert.Equal("dark", settings["theme"]);
            Assert.Equal("zh-CN", settings["language"]);
        }

        [Fact]
        public async Task UserGrain_DeleteSetting_RemovesSettingSuccessfully()
        {
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IUserGrain>("test-user-13");

            await grain.UpdateSettingAsync("test-key", "test-value");
            var settings = await grain.GetSettingsAsync();
            Assert.True(settings.ContainsKey("test-key"));

            await grain.DeleteSettingAsync("test-key");
            settings = await grain.GetSettingsAsync();
            Assert.False(settings.ContainsKey("test-key"));
        }

        [Fact]
        public async Task UserGrain_UpdateOnlineStatus_Success()
        {
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IUserGrain>("test-user-14");

            await grain.UpdateOnlineStatusAsync(true);
            var isOnline = await grain.IsOnlineAsync();
            Assert.True(isOnline);

            await grain.UpdateOnlineStatusAsync(false);
            isOnline = await grain.IsOnlineAsync();
            Assert.False(isOnline);
        }

        [Fact]
        public async Task UserGrain_UpdateLoginInfo_Success()
        {
            var grain = _fixture.Cluster.GrainFactory.GetGrain<IUserGrain>("test-user-15");

            await grain.UpdateLoginInfoAsync(true, "192.168.1.100", "Mozilla/5.0");

            var lastLogin = await grain.GetLastLoginAsync();
            Assert.True(lastLogin > DateTime.UtcNow.AddMinutes(-1));
        }
    }
}
