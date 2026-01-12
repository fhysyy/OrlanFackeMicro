
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FakeMicro.Api.Security;
using FakeMicro.Utilities.Configuration;
using Microsoft.AspNetCore.Builder;
using System;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace FakeMicro.Api.Extensions
{
    /// <summary>
    /// 安全相关的扩展方法
    /// </summary>
    public static class SecurityExtensions
    {
        /// <summary>
        /// 添加JWT认证服务
        /// </summary>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
        {
            // 从配置中获取JWT设置
            var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            var appSettings = configuration.GetAppSettings();
            
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Bearer";
                options.DefaultChallengeScheme = "Bearer";
                options.DefaultScheme = "Bearer";
            })
            .AddJwtBearer("Bearer", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = appSettings.Jwt.Issuer,
                    ValidAudience = appSettings.Jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettings.Jwt.SecretKey))
                };
                
                // 可选：配置Token验证事件
                options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            });
            
            return services;
        }

        /// <summary>
        /// 添加授权策略
        /// </summary>
        public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // 管理员策略
                options.AddPolicy("Admin", policy =>
                    policy.RequireRole("Admin", "SUPER_ADMIN"));

                // 系统管理员策略
                options.AddPolicy("SystemAdmin", policy =>
                    policy.RequireRole("SystemAdmin", "SUPER_ADMIN"));

                // 超级管理员策略
                options.AddPolicy("SuperAdmin", policy =>
                    policy.RequireRole("SUPER_ADMIN"));

                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireRole("Admin", "SUPER_ADMIN"));

                options.AddPolicy("UserOrAdmin", policy =>
                    policy.RequireRole("User", "Admin", "SUPER_ADMIN"));

                options.AddPolicy("ApiAccess", policy =>
                    policy.RequireAuthenticatedUser());
            });

            return services;
        }

        /// <summary>
        /// 添加安全头配置
        /// </summary>
        public static IServiceCollection AddSecurityHeaders(this IServiceCollection services)
        {
            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            services.AddHttpsRedirection(options =>
            {
                options.HttpsPort = 443;
            });

            return services;
        }
    }
}