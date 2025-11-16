using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace FakeMicro.Utilities.Configuration;

/// <summary>
/// 配置验证器
/// </summary>
public static class ConfigurationValidator
{
    /// <summary>
    /// 验证配置节
    /// </summary>
    public static void ValidateConfiguration<T>(T configuration) where T : class
    {
        var validationContext = new ValidationContext(configuration);
        var validationResults = new List<ValidationResult>();
        
        if (!Validator.TryValidateObject(configuration, validationContext, validationResults, true))
        {
            var errors = string.Join(", ", validationResults.Select(r => r.ErrorMessage));
            throw new ValidationException($"配置验证失败: {typeof(T).Name} - {errors}");
        }
    }
    
    /// <summary>
    /// 验证环境配置
    /// </summary>
    public static void ValidateEnvironmentConfiguration(AppSettings settings)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        
        // 生产环境安全检查
        if (environment == "Production")
        {
            ValidateProductionSettings(settings);
        }
        
        // 开发环境警告
        if (environment == "Development")
        {
            ValidateDevelopmentSettings(settings);
        }
    }
    
    /// <summary>
    /// 验证生产环境设置
    /// </summary>
    private static void ValidateProductionSettings(AppSettings settings)
    {
        var warnings = new List<string>();
        
        // 检查默认密码
        if (settings.Database.Password == "123456")
        {
            warnings.Add("数据库密码使用默认值，建议通过环境变量DB_PASSWORD设置");
        }
        
        // 检查JWT密钥
        if (settings.Jwt.SecretKey == "your-super-secret-key-change-in-production")
        {
            warnings.Add("JWT密钥使用默认值，建议通过环境变量JWT_SECRET_KEY设置");
        }
        
        // 检查本地主机配置
        if (settings.Database.Server == "localhost" || settings.Database.Server == "127.0.0.1")
        {
            warnings.Add("数据库服务器配置为本地地址，生产环境应使用远程数据库");
        }
        
        if (warnings.Any())
        {
            var warningMessage = string.Join("; ", warnings);
            throw new ValidationException($"生产环境配置警告: {warningMessage}");
        }
    }
    
    /// <summary>
    /// 验证开发环境设置
    /// </summary>
    private static void ValidateDevelopmentSettings(AppSettings settings)
    {
        // 开发环境可以放宽要求，但记录警告
        if (settings.Database.Password == "123456")
        {
            // 开发环境警告：数据库密码使用默认值
            // 注：这里没有记录警告，因为配置验证主要在启动时执行
        }
        
        if (settings.Jwt.SecretKey == "your-super-secret-key-change-in-production")
        {
            // 开发环境警告：JWT密钥使用默认值
            // 注：这里没有记录警告，因为配置验证主要在启动时执行
        }
    }
    
    /// <summary>
    /// 验证数据库连接
    /// </summary>
    public static async Task ValidateDatabaseConnectionAsync(DatabaseConfig databaseConfig)
    {
        try
        {
            using var connection = new NpgsqlConnection(databaseConfig.GetConnectionString());
            await connection.OpenAsync();
            
            // 验证数据库存在 - 使用标准的ADO.NET方法
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync();
            
            await connection.CloseAsync();
        }
        catch (Exception ex)
        {
            throw new ValidationException($"数据库连接验证失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 验证Orleans配置
    /// </summary>
    public static void ValidateOrleansConfiguration(OrleansConfig orleansConfig)
    {
        if (string.IsNullOrEmpty(orleansConfig.ClusterId))
        {
            throw new ValidationException("Orleans ClusterId 不能为空");
        }
        
        if (string.IsNullOrEmpty(orleansConfig.ServiceId))
        {
            throw new ValidationException("Orleans ServiceId 不能为空");
        }
        
        if (orleansConfig.SiloPort <= 0 || orleansConfig.SiloPort > 65535)
        {
            throw new ValidationException("Silo端口必须在1-65535范围内");
        }
        
        if (orleansConfig.GatewayPort <= 0 || orleansConfig.GatewayPort > 65535)
        {
            throw new ValidationException("Gateway端口必须在1-65535范围内");
        }
    }
}

/// <summary>
/// 配置验证启动过滤器
/// </summary>
public class ConfigurationValidationStartupFilter : IStartupFilter
{
    private readonly IConfiguration _configuration;
    
    public ConfigurationValidationStartupFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            // 在应用启动时验证配置
            var settings = _configuration.GetAppSettings();
            ConfigurationValidator.ValidateConfiguration(settings);
            ConfigurationValidator.ValidateEnvironmentConfiguration(settings);
            
            next(app);
        };
    }
}



/// <summary>
/// 配置验证服务接口
/// </summary>
public interface IConfigurationValidator
{
    Task ValidateAsync();
}

/// <summary>
/// 配置验证服务实现
/// </summary>
public class ConfigurationValidatorService : IConfigurationValidator
{
    private readonly AppSettings _settings;
    
    public ConfigurationValidatorService(AppSettings settings)
    {
        _settings = settings;
    }
    
    public async Task ValidateAsync()
    {
        // 验证基本配置
        ConfigurationValidator.ValidateConfiguration(_settings);
        ConfigurationValidator.ValidateEnvironmentConfiguration(_settings);
        ConfigurationValidator.ValidateOrleansConfiguration(_settings.Orleans);
        
        // 验证数据库连接（异步）
        await ConfigurationValidator.ValidateDatabaseConnectionAsync(_settings.Database);
    }
}