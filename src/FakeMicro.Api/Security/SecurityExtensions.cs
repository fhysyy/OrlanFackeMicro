
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FakeMicro.Api.Security;
using FakeMicro.Utilities.Configuration;
using Microsoft.AspNetCore.Builder;
using System;

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
            services.AddAuthentication(options =>
            {
                // options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                //options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            });
            //.AddJwtBearer(options =>
            //{
            //    // 使用配置工厂来获取JWT配置
            //    var serviceProvider = services.BuildServiceProvider();
            //    var jwtConfig = serviceProvider.GetRequiredService<IOptions<JwtConfig>>().Value;
                
            //    options.TokenValidationParameters = new TokenValidationParameters
            //    {
            //        ValidateIssuerSigningKey = true,
            //        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret)),
            //        ValidateIssuer = jwtConfig.ValidateIssuer,
            //        ValidIssuer = jwtConfig.Issuer,
            //        ValidateAudience = jwtConfig.ValidateAudience,
            //        ValidAudience = jwtConfig.Audience,
            //        ValidateLifetime = jwtConfig.ValidateLifetime,
            //        ClockSkew = TimeSpan.FromSeconds(jwtConfig.ClockSkewSeconds)
            //    };

            //    options.Events = new JwtBearerEvents
            //    {
            //        OnAuthenticationFailed = context =>
            //        {
            //            // 记录认证失败日志
            //            return Task.CompletedTask;
            //        },
            //        OnTokenValidated = context =>
            //        {
            //            // Token验证成功后的处理
            //            return Task.CompletedTask;
            //        }
            //    };
            //});

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
                    policy.RequireRole("Admin"));

                // 系统管理员策略
                options.AddPolicy("SystemAdmin", policy =>
                    policy.RequireRole("SystemAdmin"));

                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireRole("Admin"));

                options.AddPolicy("UserOrAdmin", policy =>
                    policy.RequireRole("User", "Admin"));

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