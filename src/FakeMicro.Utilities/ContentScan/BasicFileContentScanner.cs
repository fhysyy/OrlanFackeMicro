using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace FakeMicro.Utilities.ContentScan
{
    public class BasicFileContentScanner : IFileContentScanner
    {
        private readonly FileStorageConfig _config;
        private readonly HashSet<string> _blockedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".dll", ".bat", ".cmd", ".ps1", ".psm1", ".vbs", ".js", 
            ".wsf", ".scr", ".pif", ".msi", ".msp", ".com", ".cpl", ".hta",
            ".jar", ".class", ".php", ".asp", ".aspx", ".jsp", ".jspx", ".cer",
            ".swf", ".flv", ".exe", ".bin", ".sh", ".bash", ".zsh", ".ksh",
            ".csh", ".tcsh", ".fish", ".app", ".dmg", ".pkg", ".deb", ".rpm"
        };

        private readonly HashSet<string> _blockedMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "application/x-msdownload", "application/x-executable", "application/x-dosexec",
            "application/x-bat", "application/x-cmd", "application/x-powershell",
            "application/x-vbs", "application/javascript", "application/x-javascript",
            "application/x-shockwave-flash", "application/java-archive", "application/x-java-applet",
            "application/x-php", "text/php", "application/x-httpd-php",
            "application/x-httpd-php-source", "text/x-php", "application/x-asp",
            "application/x-aspx", "application/x-jsp", "application/x-java-jsp",
            "application/x-cer", "application/x-hta", "application/x-scr",
            "application/x-pif", "application/x-msi", "application/x-msp",
            "application/x-com", "application/x-cpl", "application/x-sh",
            "application/x-bash", "application/x-zsh", "application/x-ksh",
            "application/x-csh", "application/x-tcsh", "application/x-fish"
        };

        private readonly List<byte[]> _executableSignatures = new List<byte[]>
        {
            new byte[] { 0x4D, 0x5A }, // MZ header for EXE files
            new byte[] { 0x7F, 0x45, 0x4C, 0x46 }, // ELF header for Linux executables
            new byte[] { 0xFE, 0xED, 0xFA, 0xCE }, // Mach-O header for macOS executables
            new byte[] { 0xCE, 0xFA, 0xED, 0xFE }, // Mach-O header for macOS executables
            new byte[] { 0xFE, 0xED, 0xFA, 0xCF }, // Mach-O header for macOS executables
            new byte[] { 0xCF, 0xFA, 0xED, 0xFE }  // Mach-O header for macOS executables
        };

        private readonly List<Regex> _maliciousPatterns = new List<Regex>
        {
            new Regex(@"eval\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"document\.cookie", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"window\.location\s*=", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"<script", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"</script>", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"onload\s*=", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"onclick\s*=", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"javascript:", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"<?php", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"?>", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"system\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"exec\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"shell_exec\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"passthru\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"popen\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"proc_open\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"assert\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"create_function\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"eval\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"call_user_func\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"call_user_func_array\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"file_get_contents\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"file_put_contents\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"fopen\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"fwrite\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"base64_decode\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"gzinflate\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"gzuncompress\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"str_rot13\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"strrev\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"urldecode\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"rawurldecode\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"html_entity_decode\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"preg_replace\s*\([^,]+\/e", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"mb_ereg_replace\s*\([^,]+\/e", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"\$\_GET\[\", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"\$\_POST\[\", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"\$\_REQUEST\[\", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"\$\_COOKIE\[\", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"\$\_SERVER\[\", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"\$\_FILES\[\", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"\$\_ENV\[\", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"\$\GLOBALS\[", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"\$_SESSION\[", RegexOptions.Compiled | RegexOptions.IgnoreCase)
        };

        public BasicFileContentScanner(IOptions<FileStorageConfig> configOptions)
        {
            _config = configOptions.Value;
        }

        public async Task<FileScanResult> ScanFileContentAsync(Stream fileStream, string fileName, string contentType = null)
        {
            var scanResult = new FileScanResult { IsSafe = true };
            var scanDetails = new List<string>();

            // Save current position to restore later
            var originalPosition = fileStream.Position;

            try
            {
                // Check file extension
                var extension = Path.GetExtension(fileName);
                if (!string.IsNullOrEmpty(extension) && _blockedExtensions.Contains(extension))
                {
                    scanResult.IsSafe = false;
                    scanResult.Message = $"文件扩展名 {extension} 不被允许";
                    scanDetails.Add($"Blocked extension: {extension}");
                }

                // Check MIME type
                if (!string.IsNullOrEmpty(contentType) && _blockedMimeTypes.Contains(contentType))
                {
                    scanResult.IsSafe = false;
                    scanResult.Message = $"文件类型 {contentType} 不被允许";
                    scanDetails.Add($"Blocked MIME type: {contentType}");
                }

                // Check file size
                if (fileStream.Length > _config.MaxFileSize)
                {
                    scanResult.IsSafe = false;
                    scanResult.Message = $"文件大小超过限制 ({_config.MaxFileSize / (1024 * 1024)} MB)";
                    scanDetails.Add($"File size exceeded: {fileStream.Length} > {_config.MaxFileSize}");
                }

                // Check executable signatures
                fileStream.Position = 0;
                var signatureBuffer = new byte[4];
                await fileStream.ReadAsync(signatureBuffer, 0, signatureBuffer.Length);
                
                foreach (var signature in _executableSignatures)
                {
                    if (signatureBuffer.Take(signature.Length).SequenceEqual(signature))
                    {
                        scanResult.IsSafe = false;
                        scanResult.Message = "检测到可执行文件，不允许上传";
                        scanDetails.Add($"Executable signature found: {BitConverter.ToString(signature)}");
                        break;
                    }
                }

                // Check for malicious patterns in text files
                fileStream.Position = 0;
                using (var reader = new StreamReader(fileStream, leaveOpen: true))
                {
                    // Read first 1MB for scanning
                    var buffer = new char[1024 * 1024];
                    var bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length);
                    var content = new string(buffer, 0, bytesRead);

                    foreach (var pattern in _maliciousPatterns)
                    {
                        if (pattern.IsMatch(content))
                        {
                            scanResult.IsSafe = false;
                            scanResult.Message = "检测到潜在的恶意代码";
                            scanDetails.Add($"Malicious pattern found: {pattern.ToString()}");
                            break;
                        }
                    }
                }
            }
            finally
            {
                // Restore original position
                fileStream.Position = originalPosition;
            }

            scanResult.ScanDetails = string.Join(Environment.NewLine, scanDetails);
            return scanResult;
        }
    }
}
