using Orleans;

namespace FakeMicro.Interfaces.Models
{
    /// <summary>
    /// 登录响应
    /// </summary>
    [GenerateSerializer]
    public class LoginResponse
    {
        [Id(0)]
        public string Token { get; set; } = string.Empty;
        
        [Id(1)]
        public ControllerUserInfo User { get; set; } = new();
    }
    
    /// <summary>
    /// 控制器用户信息
    /// </summary>
    [GenerateSerializer]
    public class ControllerUserInfo
    {
        [Id(0)]
        public long Id { get; set; }
        
        [Id(1)]
        public string Username { get; set; } = string.Empty;
        
        [Id(2)]
        public string Email { get; set; } = string.Empty;
        
        [Id(3)]
        public string? Phone { get; set; }
        
        [Id(4)]
        public string Role { get; set; } = string.Empty;
        
        [Id(5)]
        public string Status { get; set; } = string.Empty;
        
        [Id(6)]
        public DateTime LastLogin { get; set; }
    }
    
    /// <summary>
    /// 重置密码请求
    /// </summary>
    [GenerateSerializer]
    public class ResetPasswordRequest
    {
        [Id(0)]
        public string Email { get; set; } = string.Empty;
        [Id(1)]
        public string UsernameOrEmail { get; set; } = string.Empty;
        [Id(2)]
        public string NewPassword { get; set; } = string.Empty;
        [Id(3)]
        public string Token { get; set; } = string.Empty;
    }
    [GenerateSerializer]
    public class RequestPasswordResetRequest {
        [Id(0)]
        public string UsernameOrEmail { get; set; } = string.Empty;

    }
    [GenerateSerializer]
    public class VerifyEmailRequest
    {
        [Id(0)]
        public string Token { get; set; } = string.Empty;
    }
    [GenerateSerializer]
    public class VerifyPhoneRequest
    {
        [Id(0)]
        public string Token { get; set; } = string.Empty;
      
        [Id(1)] 
        public string Code { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 管理员响应
    /// </summary>
    [GenerateSerializer]
    public class AdminResponse
    {
        [Id(0)]
        public bool Success { get; set; }
        
        [Id(1)]
        public string Message { get; set; } = string.Empty;
        
        [Id(2)]
        public string Username { get; set; } = string.Empty;
        
        [Id(3)]
        public DateTime Timestamp { get; set; }
    }
    
    /// <summary>
    /// 基于角色的响应
    /// </summary>
    [GenerateSerializer]
    public class RoleBasedResponse
    {
        [Id(0)]
        public bool Success { get; set; }
        
        [Id(1)]
        public string Message { get; set; } = string.Empty;
        
        [Id(2)]
        public string UserRole { get; set; } = string.Empty;
        
        [Id(3)]
        public string RequestedRole { get; set; } = string.Empty;
        
        [Id(4)]
        public string Username { get; set; } = string.Empty;
        
        [Id(5)]
        public DateTime Timestamp { get; set; }
    }

    //[GenerateSerializer]
    //public class ResetPasswordRequest
    //{
    //    [Id(0)]
    //    public string UsernameOrEmail { get; set; } = string.Empty;
    //    [Id(1)]
    //    public string Token { get; set; } = string.Empty;
    //    [Id(2)]
    //    public string NewPassword { get; set; } = string.Empty;
    //}
}