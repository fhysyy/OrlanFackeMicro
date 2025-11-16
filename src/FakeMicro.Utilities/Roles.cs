namespace FakeMicro.Utilities;

/// <summary>
/// 系统角色定义
/// </summary>
public static class Roles
{
    /// <summary>
    /// 普通用户
    /// </summary>
    public const string User = "User";
    
    /// <summary>
    /// 管理员
    /// </summary>
    public const string Admin = "Admin";
    
    /// <summary>
    /// 系统管理员
    /// </summary>
    public const string SystemAdmin = "SystemAdmin";
    
    /// <summary>
    /// 内容管理员
    /// </summary>
    public const string ContentManager = "ContentManager";
    
    /// <summary>
    /// 审核员
    /// </summary>
    public const string Auditor = "Auditor";
    
    /// <summary>
    /// 检查用户是否具有指定角色
    /// </summary>
    public static bool HasRole(string userRole, string requiredRole)
    {
        if (string.IsNullOrEmpty(userRole) || string.IsNullOrEmpty(requiredRole))
            return false;
            
        // 角色权限层级
        var roleHierarchy = new Dictionary<string, int>
        {
            { User, 1 },
            { Auditor, 2 },
            { ContentManager, 3 },
            { Admin, 4 },
            { SystemAdmin, 5 }
        };
        
        // 检查用户角色是否满足要求
        if (roleHierarchy.TryGetValue(userRole, out int userLevel) && 
            roleHierarchy.TryGetValue(requiredRole, out int requiredLevel))
        {
            return userLevel >= requiredLevel;
        }
        
        return false;
    }
    
    /// <summary>
    /// 检查用户是否具有管理员权限
    /// </summary>
    public static bool IsAdmin(string role)
    {
        return HasRole(role, Admin);
    }
    
    /// <summary>
    /// 检查用户是否具有系统管理员权限
    /// </summary>
    public static bool IsSystemAdmin(string role)
    {
        return HasRole(role, SystemAdmin);
    }
}