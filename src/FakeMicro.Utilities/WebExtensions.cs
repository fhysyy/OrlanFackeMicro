using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace FakeMicro.Utilities
{
    /// <summary>
    /// Web相关扩展方法
    /// </summary>
    public static class WebExtensions
    {
        /// <summary>
        /// 检查IP地址是否为内网地址
        /// </summary>
        public static bool IsPrivateIP(this string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress) || !ValidationHelper.IsValidIpAddress(ipAddress))
                return false;

            var parts = ipAddress.Split('.');
            if (parts.Length != 4) return false;

            var first = int.Parse(parts[0]);
            var second = int.Parse(parts[1]);

            // 10.0.0.0 - 10.255.255.255
            if (first == 10) return true;

            // 172.16.0.0 - 172.31.255.255
            if (first == 172 && second >= 16 && second <= 31) return true;

            // 192.168.0.0 - 192.168.255.255
            if (first == 192 && second == 168) return true;

            // 127.0.0.0 - 127.255.255.255
            if (first == 127) return true;

            return false;
        }

        /// <summary>
        /// 提取HTML中的纯文本
        /// </summary>
        public static string ExtractPlainTextFromHtml(this string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            // 移除HTML标签
            var text = Regex.Replace(html, @"<[^>]*>", string.Empty);
            
            // 解码HTML实体
            text = WebUtility.HtmlDecode(text);
            
            // 移除多余的空格和换行
            text = Regex.Replace(text, @"\s+", " ").Trim();

            return text;
        }

        /// <summary>
        /// 提取HTML中的所有链接
        /// </summary>
        public static List<string> ExtractLinksFromHtml(this string html, string baseUrl = "")
        {
            var links = new List<string>();
            if (string.IsNullOrEmpty(html))
                return links;

            var pattern = @"<a\s+[^>]*href\s*=\s*[""']([^""']+)[""'][^>]*>";
            var matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var href = match.Groups[1].Value;
                    
                    // 处理相对路径
                    if (!string.IsNullOrEmpty(baseUrl) && !href.StartsWith("http"))
                    {
                        href = UrlHelper.CombinePaths(baseUrl, href);
                    }

                    if (UrlHelper.IsValidUrl(href))
                    {
                        links.Add(href);
                    }
                }
            }

            return links.Distinct().ToList();
        }

        /// <summary>
        /// 提取HTML中的所有图片链接
        /// </summary>
        public static List<string> ExtractImageUrlsFromHtml(this string html, string baseUrl = "")
        {
            var images = new List<string>();
            if (string.IsNullOrEmpty(html))
                return images;

            var pattern = @"<img\s+[^>]*src\s*=\s*[""']([^""']+)[""'][^>]*>";
            var matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var src = match.Groups[1].Value;
                    
                    // 处理相对路径
                    if (!string.IsNullOrEmpty(baseUrl) && !src.StartsWith("http"))
                    {
                        src = UrlHelper.CombinePaths(baseUrl, src);
                    }

                    if (UrlHelper.IsValidUrl(src))
                    {
                        images.Add(src);
                    }
                }
            }

            return images.Distinct().ToList();
        }

        /// <summary>
        /// 生成安全的文件名（移除非法字符）
        /// </summary>
        public static string ToSafeFileName(this string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "file";

            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
            
            return string.IsNullOrEmpty(safeName) ? "file" : safeName;
        }

        /// <summary>
        /// 生成安全的文件路径（移除非法字符）
        /// </summary>
        public static string ToSafeFilePath(this string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "file";

            var invalidChars = Path.GetInvalidPathChars();
            var safePath = new string(filePath.Where(c => !invalidChars.Contains(c)).ToArray());
            
            return string.IsNullOrEmpty(safePath) ? "file" : safePath;
        }

        /// <summary>
        /// 获取文件扩展名（小写，不带点）
        /// </summary>
        public static string GetFileExtension(this string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            var extension = Path.GetExtension(fileName);
            return string.IsNullOrEmpty(extension) ? string.Empty : extension.TrimStart('.').ToLowerInvariant();
        }

        /// <summary>
        /// 检查文件扩展名是否在允许的列表中
        /// </summary>
        public static bool HasAllowedExtension(this string fileName, params string[] allowedExtensions)
        {
            if (string.IsNullOrEmpty(fileName) || allowedExtensions == null)
                return false;

            var extension = fileName.GetFileExtension();
            return allowedExtensions.Any(ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 检查是否为图片文件
        /// </summary>
        public static bool IsImageFile(this string fileName)
        {
            var imageExtensions = new[] { "jpg", "jpeg", "png", "gif", "bmp", "webp", "svg" };
            return fileName.HasAllowedExtension(imageExtensions);
        }

        /// <summary>
        /// 检查是否为文档文件
        /// </summary>
        public static bool IsDocumentFile(this string fileName)
        {
            var docExtensions = new[] { "pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx", "txt" };
            return fileName.HasAllowedExtension(docExtensions);
        }

        /// <summary>
        /// 检查是否为视频文件
        /// </summary>
        public static bool IsVideoFile(this string fileName)
        {
            var videoExtensions = new[] { "mp4", "avi", "mov", "wmv", "flv", "webm", "mkv" };
            return fileName.HasAllowedExtension(videoExtensions);
        }

        /// <summary>
        /// 检查是否为音频文件
        /// </summary>
        public static bool IsAudioFile(this string fileName)
        {
            var audioExtensions = new[] { "mp3", "wav", "ogg", "aac", "flac", "wma" };
            return fileName.HasAllowedExtension(audioExtensions);
        }

        /// <summary>
        /// 获取文件大小的人类可读格式
        /// </summary>
        public static string ToFileSizeDisplay(this long fileSize)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            var order = 0;
            double size = fileSize;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }

        /// <summary>
        /// 获取MIME类型根据文件扩展名
        /// </summary>
        public static string GetMimeType(this string fileName)
        {
            var extension = fileName.GetFileExtension();
            
            return extension switch
            {
                "jpg" or "jpeg" => "image/jpeg",
                "png" => "image/png",
                "gif" => "image/gif",
                "bmp" => "image/bmp",
                "webp" => "image/webp",
                "pdf" => "application/pdf",
                "doc" => "application/msword",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "xls" => "application/vnd.ms-excel",
                "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ppt" => "application/vnd.ms-powerpoint",
                "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                "txt" => "text/plain",
                "html" or "htm" => "text/html",
                "css" => "text/css",
                "js" => "application/javascript",
                "json" => "application/json",
                "xml" => "application/xml",
                "zip" => "application/zip",
                "mp3" => "audio/mpeg",
                "wav" => "audio/wav",
                "mp4" => "video/mp4",
                "avi" => "video/x-msvideo",
                "mov" => "video/quicktime",
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// 生成ETag（基于内容的哈希）
        /// </summary>
        public static string GenerateETag(this string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            var hash = CryptoHelper.ComputeMD5Hash(content);
            return $"\"{hash}\"";
        }

        /// <summary>
        /// 生成ETag（基于字节数组的哈希）
        /// </summary>
        public static string GenerateETag(this byte[] data)
        {
            if (data == null || data.Length == 0)
                return string.Empty;

            var content = Encoding.UTF8.GetString(data);
            return content.GenerateETag();
        }

        /// <summary>
        /// 检查ETag是否匹配
        /// </summary>
        public static bool ETagMatches(this string etag, string otherEtag)
        {
            if (string.IsNullOrEmpty(etag) || string.IsNullOrEmpty(otherEtag))
                return false;

            return etag.Equals(otherEtag, StringComparison.Ordinal);
        }

        /// <summary>
        /// 生成随机的User-Agent字符串
        /// </summary>
        public static string GenerateRandomUserAgent()
        {
            var browsers = new[]
            {
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:89.0) Gecko/20100101 Firefox/89.0",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.1.1 Safari/605.1.15",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36 Edg/91.0.864.59"
            };

            var random = new Random();
            return browsers[random.Next(browsers.Length)];
        }
    }
}