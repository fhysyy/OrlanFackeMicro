using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;

namespace FakeMicro.Api.Middleware
{
    /// <summary>
    /// 全局异常处理器（已弃用，使用ExceptionHandlingMiddleware替代）
    /// </summary>
    [Obsolete("使用ExceptionHandlingMiddleware替代")]
    public static class GlobalExceptionHandler
    {
        /// <summary>
        /// 配置全局异常处理
        /// </summary>
        [Obsolete("使用ExceptionHandlingMiddleware替代")]
        public static void ConfigureExceptionHandler(this IApplicationBuilder app)
        {
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json";

                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null)
                    {
                        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogError(contextFeature.Error, "全局异常处理器捕获到未处理的异常");

                        var response = new ProblemDetails
                        {
                            Status = context.Response.StatusCode,
                            Title = "服务器内部错误",
                            Detail = "处理请求时发生错误，请稍后重试",
                            Instance = context.Request.Path
                        };

                        await context.Response.WriteAsJsonAsync(response);
                    }
                });
            });
        }
    }
}