using Orleans;

namespace FakeMicro.Interfaces.Events
{
    public interface IAuthEvents
    {
        /// <summary>
        /// 用户注册事件
        /// </summary>
        Task HandleUserRegisteredAsync(UserRegisteredEvent userEvent);
        
        /// <summary>
        /// 用户登录事件
        /// </summary>
        Task HandleUserLoggedInAsync(UserLoggedInEvent userEvent);
        
        /// <summary>
        /// 密码修改事件
        /// </summary>
        Task HandlePasswordChangedAsync(PasswordChangedEvent userEvent);
    }
    
    [GenerateSerializer]
    public class UserRegisteredEvent
    {
        [Id(0)] public int UserId { get; set; }
        [Id(1)] public string Username { get; set; } = string.Empty;
        [Id(2)] public string Email { get; set; } = string.Empty;
        [Id(3)] public DateTime RegisteredAt { get; set; }
    }
    
    [GenerateSerializer]
    public class UserLoggedInEvent
    {
        [Id(0)] public int UserId { get; set; }
        [Id(1)] public string Username { get; set; } = string.Empty;
        [Id(2)] public DateTime LoggedInAt { get; set; }
        [Id(3)] public string IpAddress { get; set; } = string.Empty;
    }
    
    [GenerateSerializer]
    public class PasswordChangedEvent
    {
        [Id(0)] public int UserId { get; set; }
        [Id(1)] public string Username { get; set; } = string.Empty;
        [Id(2)] public DateTime ChangedAt { get; set; }
    }
}