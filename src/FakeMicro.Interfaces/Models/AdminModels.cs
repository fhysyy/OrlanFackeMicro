using FakeMicro.Entities.Enums;
using Orleans;

namespace FakeMicro.Interfaces.Models
{
    /// <summary>
    /// 更新用户请求
    /// </summary>
    [GenerateSerializer]
    public class UpdateUserRequest
    {
        [Id(0)]
        public string Username { get; set; } = string.Empty;
        [Id(1)]
        public string Email { get; set; } = string.Empty;
        [Id(2)]
        public string? Phone { get; set; }
        [Id(3)]
        public string? Role { get; set; }
        [Id(4)]
        public string? Status { get; set; }
    }

    /// <summary>
    /// 更新角色请求
    /// </summary>
    [GenerateSerializer]
    public class UpdateRoleRequest
    {
        [Id(0)]
        public string Role { get; set; } = string.Empty;
    }

    /// <summary>
    /// 系统统计响应
    /// </summary>
    [GenerateSerializer]
    public class SystemStatisticsResponse
    {
        [Id(0)]
        public int TotalUsers { get; set; }
        [Id(1)]
        public int ActiveUsers { get; set; }
        [Id(2)]
        public int NewUsersToday { get; set; }
        [Id(3)]
        public Dictionary<string, int> RoleDistribution { get; set; } = new();
        [Id(4)]
        public Dictionary<string, int> StatusDistribution { get; set; } = new();
        [Id(5)]
        public TimeSpan SystemUptime { get; set; }
    }

    /// <summary>
    /// 用户搜索请求
    /// </summary>
    [GenerateSerializer]
    public class UserSearchRequest
    {
        [Id(0)]
        public string? Username { get; set; }
        [Id(1)]
        public string? Email { get; set; }
        [Id(2)]
        public string? Status { get; set; }
    }
}