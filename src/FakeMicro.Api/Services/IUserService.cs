using System.Threading.Tasks;

namespace FakeMicro.Api.Services
{
    public interface IUserService
    {
        Task<string> GetUserNameAsync(int userId);
    }
}