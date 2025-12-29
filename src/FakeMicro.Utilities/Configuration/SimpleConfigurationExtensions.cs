using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FakeMicro.Utilities.Configuration;

/// <summary>
/// 简化配置扩展方法
/// </summary>
public static class SimpleConfigurationExtensions
{
    /// <summary>
    /// 添加简化配置服务
    /// </summary>
    public static IServiceCollection AddSimpleAppSettings(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = new SimpleAppSettings();
        configuration.Bind(settings);
        
        // 从环境变量覆盖敏感信息
        OverrideFromEnvironmentVariables(settings);
        
        services.AddSingleton(settings);
        return services;
    }
    
    /// <summary>
    /// 从环境变量覆盖敏感配置
    /// </summary>
    private static void OverrideFromEnvironmentVariables(SimpleAppSettings settings)
    {
        // 数据库连接字符串
        var dbConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(dbConnectionString))
        {
            settings.DatabaseConnectionString = dbConnectionString;
        }
        else
        {
            // 如果环境变量不存在，从各个部分构建
            var dbServer = Environment.GetEnvironmentVariable("DB_SERVER") ?? "localhost";
            var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
            var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "fakemicro";
            var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
            var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? string.Empty;
            
            // 直接构建连接字符串，不调用InitializeDatabaseConnection方法
            settings.DatabaseConnectionString = $"Server={dbServer};Port={dbPort};Database={dbName};User Id={dbUser};Password={dbPassword};";
        }
        
        // Orleans连接字符串
        var orleansConnectionString = Environment.GetEnvironmentVariable("ORLEANS_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(orleansConnectionString))
        {
            settings.OrleansConnectionString = orleansConnectionString;
        }
        
        // JWT密钥
        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
        if (!string.IsNullOrEmpty(jwtSecret))
        {
            settings.JwtSecret = jwtSecret;
        }
        
        // 生产环境安全检查
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
        {
            if (settings.JwtSecret == "your-super-secret-key-change-in-production")
            {
                throw new InvalidOperationException("生产环境必须设置JWT_SECRET环境变量");
            }
            
            if (settings.DatabaseConnectionString.Contains("Password=123456") || settings.DatabaseConnectionString.Contains("Password="))
            {
                throw new InvalidOperationException("生产环境必须设置DB_PASSWORD环境变量");
            }
        }
    }
}