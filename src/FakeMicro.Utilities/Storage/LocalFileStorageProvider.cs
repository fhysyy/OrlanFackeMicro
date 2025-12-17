using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace FakeMicro.Utilities.Storage
{
    public class LocalFileStorageProvider : IFileStorageProvider
    {
        private readonly FileStorageConfig _config;

        public LocalFileStorageProvider(IOptions<FileStorageConfig> configOptions)
        {
            _config = configOptions.Value;
            EnsureStorageDirectoryExists();
        }

        private void EnsureStorageDirectoryExists()
        {
            Directory.CreateDirectory(_config.LocalStoragePath);
        }

        public async Task SaveFileAsync(string filePath, Stream fileStream, string contentType = null)
        {
            var fullPath = Path.Combine(_config.LocalStoragePath, filePath);
            var directory = Path.GetDirectoryName(fullPath);
            
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var targetStream = File.Create(fullPath))
            {
                await fileStream.CopyToAsync(targetStream);
            }
        }

        public async Task<Stream> ReadFileAsync(string filePath)
        {
            var fullPath = Path.Combine(_config.LocalStoragePath, filePath);
            
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("File not found", fullPath);
            }

            return File.OpenRead(fullPath);
        }

        public async Task DeleteFileAsync(string filePath)
        {
            var fullPath = Path.Combine(_config.LocalStoragePath, filePath);
            
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        public async Task<bool> FileExistsAsync(string filePath)
        {
            var fullPath = Path.Combine(_config.LocalStoragePath, filePath);
            return File.Exists(fullPath);
        }

        public async Task<string> GetFileUrlAsync(string filePath, bool isPublic = false)
        {
            if (isPublic)
            {
                return $"{_config.PreviewBaseUrl}/{filePath}";
            }
            
            return $"/api/files/download/{filePath}";
        }
    }
}
