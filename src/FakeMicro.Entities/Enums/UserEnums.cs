using System.Text.Json.Serialization;

namespace FakeMicro.Entities.Enums
{
    /// <summary>
    /// 用户角色
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserRole
    {
        /// <summary>
        /// 普通用户
        /// </summary>
        User = 0,
        
        /// <summary>
        /// 管理员
        /// </summary>
        Admin = 1,
        
        /// <summary>
        /// 系统管理员
        /// </summary>
        SystemAdmin = 2,

        ContentManager=3
    }

    /// <summary>
    /// 用户状态
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UserStatus
    {
        /// <summary>
        /// 待激活
        /// </summary>
        Pending = 0,
        
        /// <summary>
        /// 活跃
        /// </summary>
        Active = 1,
        
        /// <summary>
        /// 禁用
        /// </summary>
        Disabled = 2,
        
        /// <summary>
        /// 锁定
        /// </summary>
        Locked = 3
    }

    /// <summary>
    /// 权限类型
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PermissionType
    {
        /// <summary>
        /// 读取权限
        /// </summary>
        Read = 0,
        
        /// <summary>
        /// 写入权限
        /// </summary>
        Write = 1,
        
        /// <summary>
        /// 删除权限
        /// </summary>
        Delete = 2,
        
        /// <summary>
        /// 管理权限
        /// </summary>
        Manage = 3
    }
}