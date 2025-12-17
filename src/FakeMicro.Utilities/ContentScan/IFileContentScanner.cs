using System.IO;
using System.Threading.Tasks;

namespace FakeMicro.Utilities.ContentScan
{
    public interface IFileContentScanner
    {
        Task<FileScanResult> ScanFileContentAsync(Stream fileStream, string fileName, string contentType = null);
    }

    public class FileScanResult
    {
        public bool IsSafe { get; set; }
        public string Message { get; set; }
        public string ScanDetails { get; set; }
    }
}
