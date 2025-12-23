using FakeMicro.DatabaseAccess.Interfaces;
using FakeMicro.Grains.Services;
using FakeMicro.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FakeMicro.Grains.Extensions
{
    /// <summary>
    /// Grain 服务依赖注入扩展
    /// </summary>
    public static class GrainServiceExtensions
    {
        /// <summary>
        /// 添加 Grain 服务依赖注入
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddGrainServices(this IServiceCollection services)
        {
            // 注册知识库服务
            services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
            
            // 注册AI知识库服务
            services.AddScoped<IAiKnowledgeService, AiKnowledgeService>();
            
            return services;
        }
    }
}