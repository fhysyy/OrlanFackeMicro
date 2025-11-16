using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FakeMicro.Api.Services
{
    public class SimpleUserService : IUserService
    {
        private readonly ILogger<SimpleUserService> _logger;

        public SimpleUserService(ILogger<SimpleUserService> logger)
        {
            _logger = logger;
        }

        public Task<string> GetUserNameAsync(int userId)
        {
            _logger.LogInformation("Getting user name for user {UserId}", userId);
            // 简单的模拟实现
            return Task.FromResult($"User {userId}");
        }
    }
}