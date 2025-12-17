using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FakeMicro.Utilities.ContentScan;

namespace FakeMicro.Utilities.Storage
{
    public static class StorageServiceCollectionExtensions
    {
        public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<FileStorageConfig>(configuration.GetSection("FileStorage"));
            services.AddSingleton<IFileStorageProvider, LocalFileStorageProvider>();
            services.AddSingleton<IFileContentScanner, BasicFileContentScanner>();
            return services;
        }
    }
}
