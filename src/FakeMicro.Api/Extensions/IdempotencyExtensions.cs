using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using FakeMicro.Api.Middleware;
using FakeMicro.Api.Services;
using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.DatabaseAccess.Repositories;

namespace FakeMicro.Api.Extensions;

/// <summary>
/// 幂等性扩展方法
/// 用于注册幂等性服务和中间件
/// </summary>
public static class IdempotencyExtensions
{
    /// <summary>
    /// 添加幂等性服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddIdempotency(this IServiceCollection services)
    {
        // 注册幂等性请求仓储
        services.AddScoped<IIdempotentRequestRepository, IdempotentRequestRepository>();
        
        // 注册幂等性服务
        services.AddScoped<IdempotencyService>();

        return services;
    }

    /// <summary>
    /// 使用幂等性中间件
    /// </summary>
    /// <param name="app">应用构建器</param>
    /// <returns>应用构建器</returns>
    public static IApplicationBuilder UseIdempotency(this IApplicationBuilder app)
    {
        // 注册幂等性中间件
        app.UseMiddleware<IdempotencyMiddleware>();

        return app;
    }
}
