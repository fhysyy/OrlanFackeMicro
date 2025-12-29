using System.IO;
using System.Threading.Tasks;

namespace FakeMicro.Utilities.Storage
{
    public interface IFileStorageProvider
    {
        Task SaveFileAsync(string filePath, Stream fileStream, string? contentType = null);
        Task<Stream> ReadFileAsync(string filePath);
        Task DeleteFileAsync(string filePath);
        Task<bool> FileExistsAsync(string filePath);
        Task<string> GetFileUrlAsync(string filePath, bool isPublic = false);
    }
}