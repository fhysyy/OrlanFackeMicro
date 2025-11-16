using System;

namespace FakeMicro.Api.Security;

public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Teacher = "Teacher";
    public const string Student = "Student";
    public const string Guest = "Guest";
    public const string SystemAdmin = "SystemAdmin";
    public const string ContentManager = "ContentManager";
    public const string Auditor = "Auditor";
    
    /// <summary>
    /// 检查用户是否拥有指定角色
    /// </summary>
    public static bool HasRole(string userRole, string requiredRole)
    {
        return string.Equals(userRole, requiredRole, StringComparison.OrdinalIgnoreCase);
    }
}