using FakeMicro.Entities;
using FakeMicro.Entities.Enums;
using FakeMicro.Grains;
using FakeMicro.Grains.States;
using FakeMicro.Interfaces;
using FakeMicro.Interfaces.Models;
using FakeMicro.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using Orleans;
using Orleans.Runtime;
using Orleans.Storage;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FakeMicro.Tests.UnitTests
{
    public class UserGrainTests : GrainTestBase
    {
        private readonly Mock<IPersistentState<UserState>> _userStateMock;
        private readonly Mock<ILogger<UserGrain>> _loggerMock;
        private readonly Mock<IGrainContext> _grainContextMock;
        private UserGrain _userGrain;

        public UserGrainTests()
        {
            _userStateMock = new Mock<IPersistentState<UserState>>();
            _loggerMock = new Mock<ILogger<UserGrain>>();
            _grainContextMock = new Mock<IGrainContext>();

            SetupUserState();
            SetupGrainContext();

            _userGrain = new UserGrain(_userStateMock.Object, _loggerMock.Object);
        }

        private void SetupUserState()
        {
            var userState = new UserState
            {
                UserId = 1,
                Username = "testuser",
                Email = "test@example.com",
                DisplayName = "Test User",
                Status = UserStatus.Offline.ToString(),
                IsActive = true,
                EmailVerified = true,
                PhoneVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Sessions = new List<UserSession>(),
                Permissions = new List<UserPermission>(),
                Settings = new UserSettings(),
                Friends = new Dictionary<long, DateTime>(),
                BlockedUsers = new Dictionary<long, DateTime>()
            };

            _userStateMock.Setup(x => x.State).Returns(userState);
            _userStateMock.Setup(x => x.RecordExists).Returns(true);
        }

        private void SetupGrainContext()
        {
            _grainContextMock.Setup(x => x.GrainId).Returns(new GrainId(GrainType.Create("UserGrain"), IdSpan.Create("1")));
        }

        [Fact]
        public async Task SetNicknameAsync_ValidNickname_SetsNicknameAndSavesState()
        {
            var newNickname = "New Nickname";

            await _userGrain.SetNicknameAsync(newNickname);

            Assert.Equal(newNickname, _userStateMock.Object.State.DisplayName);
            _userStateMock.Verify(x => x.WriteStateAsync(), Times.Once);
        }

        [Fact]
        public async Task SetNicknameAsync_NullNickname_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _userGrain.SetNicknameAsync(null!));
        }

        [Fact]
        public async Task SetNicknameAsync_EmptyNickname_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _userGrain.SetNicknameAsync(string.Empty));
        }

        [Fact]
        public async Task SetNicknameAsync_WhitespaceNickname_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _userGrain.SetNicknameAsync("   "));
        }

        [Fact]
        public async Task GetNicknameAsync_ReturnsCurrentNickname()
        {
            var expectedNickname = "Test User";
            _userStateMock.Object.State.DisplayName = expectedNickname;

            var result = await _userGrain.GetNicknameAsync();

            Assert.Equal(expectedNickname, result);
        }

        [Fact]
        public async Task SetAvatarAsync_ValidUrl_SetsAvatarAndSavesState()
        {
            var avatarUrl = "https://example.com/avatar.jpg";

            await _userGrain.SetAvatarAsync(avatarUrl);

            Assert.Equal(avatarUrl, _userStateMock.Object.State.AvatarUrl);
            _userStateMock.Verify(x => x.WriteStateAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAvatarAsync_ReturnsCurrentAvatar()
        {
            var expectedAvatar = "https://example.com/avatar.jpg";
            _userStateMock.Object.State.AvatarUrl = expectedAvatar;

            var result = await _userGrain.GetAvatarAsync();

            Assert.Equal(expectedAvatar, result);
        }

        [Fact]
        public async Task GetProfileAsync_ReturnsCompleteProfile()
        {
            var profile = await _userGrain.GetProfileAsync();

            Assert.NotNull(profile);
            Assert.Equal(1, profile["userId"]);
            Assert.Equal("testuser", profile["username"]);
            Assert.Equal("test@example.com", profile["email"]);
            Assert.Equal("Test User", profile["displayName"]);
            Assert.True((bool)profile["isActive"]);
            Assert.True((bool)profile["emailVerified"]);
            Assert.False((bool)profile["phoneVerified"]);
        }

        [Fact]
        public async Task UpdateProfileAsync_ValidProfile_UpdatesProfileAndSavesState()
        {
            var profile = new Dictionary<string, object>
            {
                { "username", "newusername" },
                { "email", "newemail@example.com" },
                { "displayName", "New Display Name" },
                { "status", UserStatus.Online.ToString() }
            };

            await _userGrain.UpdateProfileAsync(profile);

            Assert.Equal("newusername", _userStateMock.Object.State.Username);
            Assert.Equal("newemail@example.com", _userStateMock.Object.State.Email);
            Assert.Equal("New Display Name", _userStateMock.Object.State.DisplayName);
            Assert.Equal(UserStatus.Online.ToString(), _userStateMock.Object.State.Status);
            _userStateMock.Verify(x => x.WriteStateAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateProfileAsync_NullProfile_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _userGrain.UpdateProfileAsync(null!));
        }

        [Fact]
        public async Task GetUserAsync_ReturnsUserDto()
        {
            var userDto = await _userGrain.GetUserAsync();

            Assert.NotNull(userDto);
            Assert.Equal(1, userDto.Id);
            Assert.Equal("testuser", userDto.Username);
            Assert.Equal("test@example.com", userDto.Email);
            Assert.Equal("Test User", userDto.DisplayName);
            Assert.Equal(UserStatus.Offline, userDto.Status);
        }

        [Fact]
        public async Task UpdateUserAsync_ValidUserDto_UpdatesUserAndReturnsSuccess()
        {
            var userDto = new UserDto
            {
                Id = 1,
                Username = "updateduser",
                Email = "updated@example.com",
                DisplayName = "Updated User",
                Status = UserStatus.Online
            };

            var result = await _userGrain.UpdateUserAsync(userDto);

            Assert.True(result.Success);
            Assert.NotNull(result.UpdatedUser);
            Assert.Equal("updateduser", _userStateMock.Object.State.Username);
            Assert.Equal("updated@example.com", _userStateMock.Object.State.Email);
            Assert.Equal("Updated User", _userStateMock.Object.State.DisplayName);
            Assert.Equal(UserStatus.Online.ToString(), _userStateMock.Object.State.Status);
            _userStateMock.Verify(x => x.WriteStateAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_NullUserDto_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _userGrain.UpdateUserAsync(null!));
        }

        [Fact]
        public async Task CreateSessionAsync_ValidSession_AddsSessionAndSavesState()
        {
            var session = new UserSession
            {
                SessionId = "session-123",
                IpAddress = "192.168.1.1",
                UserAgent = "Mozilla/5.0",
                IsCurrent = true
            };

            await _userGrain.CreateSessionAsync(session);

            Assert.Single(_userStateMock.Object.State.Sessions);
            Assert.Equal("session-123", _userStateMock.Object.State.Sessions[0].SessionId);
            Assert.NotNull(_userStateMock.Object.State.Sessions[0].LoginTime);
            Assert.NotNull(_userStateMock.Object.State.Sessions[0].LastActivity);
            _userStateMock.Verify(x => x.WriteStateAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateSessionAsync_NullSession_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _userGrain.CreateSessionAsync(null!));
        }

        [Fact]
        public async Task CreateSessionAsync_SessionWithoutId_GeneratesIdAndAddsSession()
        {
            var session = new UserSession
            {
                IpAddress = "192.168.1.1",
                UserAgent = "Mozilla/5.0",
                IsCurrent = true
            };

            await _userGrain.CreateSessionAsync(session);

            Assert.Single(_userStateMock.Object.State.Sessions);
            Assert.NotNull(_userStateMock.Object.State.Sessions[0].SessionId);
            Assert.NotEmpty(_userStateMock.Object.State.Sessions[0].SessionId);
        }

        [Fact]
        public async Task GetSessionsAsync_ReturnsAllSessions()
        {
            var sessions = new List<UserSession>
            {
                new UserSession { SessionId = "session-1", IsCurrent = true },
                new UserSession { SessionId = "session-2", IsCurrent = false }
            };
            _userStateMock.Object.State.Sessions = sessions;

            var result = await _userGrain.GetSessionsAsync();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, s => s.SessionId == "session-1");
            Assert.Contains(result, s => s.SessionId == "session-2");
        }

        [Fact]
        public async Task TerminateSessionAsync_ValidSessionId_RemovesSessionAndReturnsSuccess()
        {
            var sessions = new List<UserSession>
            {
                new UserSession { SessionId = "session-1", IsCurrent = true },
                new UserSession { SessionId = "session-2", IsCurrent = false }
            };
            _userStateMock.Object.State.Sessions = sessions;

            var result = await _userGrain.TerminateSessionAsync("session-2");

            Assert.True(result.Success);
            Assert.Equal(1, result.TerminatedCount);
            Assert.Single(_userStateMock.Object.State.Sessions);
            Assert.DoesNotContain(_userStateMock.Object.State.Sessions, s => s.SessionId == "session-2");
            _userStateMock.Verify(x => x.WriteStateAsync(), Times.Once);
        }

        [Fact]
        public async Task TerminateSessionAsync_InvalidSessionId_ReturnsFailed()
        {
            var sessions = new List<UserSession>
            {
                new UserSession { SessionId = "session-1", IsCurrent = true }
            };
            _userStateMock.Object.State.Sessions = sessions;

            var result = await _userGrain.TerminateSessionAsync("nonexistent-session");

            Assert.False(result.Success);
            Assert.Single(_userStateMock.Object.State.Sessions);
        }

        [Fact]
        public async Task TerminateSessionAsync_NullSessionId_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _userGrain.TerminateSessionAsync(null!));
        }

        [Fact]
        public async Task TerminateOtherSessionsAsync_RemovesOtherSessionsAndReturnsSuccess()
        {
            var sessions = new List<UserSession>
            {
                new UserSession { SessionId = "session-1", IsCurrent = true },
                new UserSession { SessionId = "session-2", IsCurrent = false },
                new UserSession { SessionId = "session-3", IsCurrent = false }
            };
            _userStateMock.Object.State.Sessions = sessions;

            var result = await _userGrain.TerminateOtherSessionsAsync();

            Assert.True(result.Success);
            Assert.Equal(2, result.TerminatedCount);
            Assert.Single(_userStateMock.Object.State.Sessions);
            Assert.Equal("session-1", _userStateMock.Object.State.Sessions[0].SessionId);
            _userStateMock.Verify(x => x.WriteStateAsync(), Times.Once);
        }

        [Fact]
        public async Task GenerateRefreshTokenAsync_GeneratesTokenAndSavesState()
        {
            var token = await _userGrain.GenerateRefreshTokenAsync();

            Assert.NotNull(token);
            Assert.NotEmpty(token);
            Assert.Equal(token, _userStateMock.Object.State.RefreshToken);
            Assert.NotNull(_userStateMock.Object.State.RefreshTokenExpiry);
            Assert.True(_userStateMock.Object.State.RefreshTokenExpiry > DateTime.UtcNow);
            _userStateMock.Verify(x => x.WriteStateAsync(), Times.Once);
        }

        [Fact]
        public async Task ValidateRefreshTokenAsync_ValidToken_ReturnsTrue()
        {
            var token = "valid-token-123";
            _userStateMock.Object.State.RefreshToken = token;
            _userStateMock.Object.State.RefreshTokenExpiry = DateTime.UtcNow.AddDays(1);

            var result = await _userGrain.ValidateRefreshTokenAsync(token);

            Assert.True(result);
        }

        [Fact]
        public async Task ValidateRefreshTokenAsync_InvalidToken_ReturnsFalse()
        {
            _userStateMock.Object.State.RefreshToken = "valid-token-123";
            _userStateMock.Object.State.RefreshTokenExpiry = DateTime.UtcNow.AddDays(1);

            var result = await _userGrain.ValidateRefreshTokenAsync("invalid-token");

            Assert.False(result);
        }

        [Fact]
        public async Task ValidateRefreshTokenAsync_ExpiredToken_ReturnsFalse()
        {
            var token = "expired-token";
            _userStateMock.Object.State.RefreshToken = token;
            _userStateMock.Object.State.RefreshTokenExpiry = DateTime.UtcNow.AddDays(-1);

            var result = await _userGrain.ValidateRefreshTokenAsync(token);

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteUserAsync_ClearsStateAndReturnsSuccess()
        {
            var result = await _userGrain.DeleteUserAsync();

            Assert.True(result.Success);
            _userStateMock.Verify(x => x.ClearStateAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateLoginInfoAsync_UpdatesLastLoginAndSavesState()
        {
            await _userGrain.UpdateLoginInfoAsync(true, "192.168.1.1", "Mozilla/5.0");

            Assert.NotNull(_userStateMock.Object.State.LastLoginAt);
            _userStateMock.Verify(x => x.WriteStateAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateSessionAsync_CurrentSessionExists_UpdatesSessionAndSavesState()
        {
            var sessions = new List<UserSession>
            {
                new UserSession
                {
                    SessionId = "session-1",
                    IpAddress = "192.168.1.1",
                    UserAgent = "Mozilla/5.0",
                    IsCurrent = true
                }
            };
            _userStateMock.Object.State.Sessions = sessions;

            await _userGrain.UpdateSessionAsync("192.168.1.2", "Chrome/120.0");

            Assert.Equal("192.168.1.2", _userStateMock.Object.State.Sessions[0].IpAddress);
            Assert.Equal("Chrome/120.0", _userStateMock.Object.State.Sessions[0].UserAgent);
            Assert.NotNull(_userStateMock.Object.State.Sessions[0].LastActivity);
            _userStateMock.Verify(x => x.WriteStateAsync(), Times.Once);
        }

        [Fact]
        public async Task VerifyEmailAsync_ValidToken_VerifiesEmailAndReturnsSuccess()
        {
            var result = await _userGrain.VerifyEmailAsync("valid-token");

            Assert.True(result.Success);
            Assert.True(_userStateMock.Object.State.IsEmailVerified);
            _userStateMock.Verify(x => x.WriteStateAsync(), Times.Once);
        }

        [Fact]
        public async Task VerifyPhoneAsync_ValidCode_VerifiesPhoneAndReturnsSuccess()
        {
            var result = await _userGrain.VerifyPhoneAsync("123456");

            Assert.True(result.Success);
            Assert.True(_userStateMock.Object.State.IsPhoneVerified);
            _userStateMock.Verify(x => x.WriteStateAsync(), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_ValidToken_ResetsPasswordAndReturnsSuccess()
        {
            var result = await _userGrain.ResetPasswordAsync("valid-token", "newpassword123");

            Assert.True(result.Success);
            Assert.NotNull(_userStateMock.Object.State.PasswordHash);
            Assert.NotEmpty(_userStateMock.Object.State.PasswordHash);
            _userStateMock.Verify(x => x.WriteStateAsync(), Times.Once);
        }
    }
}
