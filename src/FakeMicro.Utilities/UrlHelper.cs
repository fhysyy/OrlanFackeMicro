using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace FakeMicro.Utilities
{
    /// <summary>
    /// URL处理助手类
    /// </summary>
    public static class UrlHelper
    {
        /// <summary>
        /// 编码URL参数
        /// </summary>
        public static string UrlEncode(string value)
        {
            return HttpUtility.UrlEncode(value);
        }

        /// <summary>
        /// 解码URL参数
        /// </summary>
        public static string UrlDecode(string value)
        {
            return HttpUtility.UrlDecode(value);
        }

        /// <summary>
        /// 编码URL路径
        /// </summary>
        public static string UrlPathEncode(string value)
        {
            return HttpUtility.UrlPathEncode(value);
        }

        /// <summary>
        /// HTML编码
        /// </summary>
        public static string HtmlEncode(string value)
        {
            return HttpUtility.HtmlEncode(value);
        }

        /// <summary>
        /// HTML解码
        /// </summary>
        public static string HtmlDecode(string value)
        {
            return HttpUtility.HtmlDecode(value);
        }

        /// <summary>
        /// 构建完整的URL（包含查询参数）
        /// </summary>
        public static string BuildUrl(string baseUrl, Dictionary<string, object>? parameters = null)
        {
            if (parameters == null || parameters.Count == 0)
                return baseUrl;

            var queryString = BuildQueryString(parameters);
            return baseUrl.Contains("?") ? $"{baseUrl}&{queryString}" : $"{baseUrl}?{queryString}";
        }

        /// <summary>
        /// 构建查询字符串
        /// </summary>
        public static string BuildQueryString(Dictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return string.Empty;

            var query = new List<string>();
            foreach (var param in parameters)
            {
                var value = param.Value?.ToString() ?? string.Empty;
                query.Add($"{UrlEncode(param.Key)}={UrlEncode(value)}");
            }

            return string.Join("&", query);
        }

        /// <summary>
        /// 解析查询字符串为字典
        /// </summary>
        public static Dictionary<string, string> ParseQueryString(string queryString)
        {
            var result = new Dictionary<string, string>();
            
            if (string.IsNullOrEmpty(queryString))
                return result;

            // 移除开头的问号（如果有）
            if (queryString.StartsWith("?"))
                queryString = queryString.Substring(1);

            var pairs = queryString.Split('&');
            foreach (var pair in pairs)
            {
                var parts = pair.Split('=');
                if (parts.Length == 2)
                {
                    var key = UrlDecode(parts[0]);
                    var value = UrlDecode(parts[1]);
                    result[key] = value;
                }
            }

            return result;
        }

        /// <summary>
        /// 检查URL是否有效
        /// </summary>
        public static bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            try
            {
                return Uri.TryCreate(url, UriKind.Absolute, out _);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查URL是否为HTTPS
        /// </summary>
        public static bool IsHttps(string url)
        {
            if (!IsValidUrl(url))
                return false;

            return url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 获取URL的域名部分
        /// </summary>
        public static string? GetDomain(string url)
        {
            if (!IsValidUrl(url))
                return null;

            try
            {
                var uri = new Uri(url);
                return uri.Host;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取URL的路径部分
        /// </summary>
        public static string? GetPath(string url)
        {
            if (!IsValidUrl(url))
                return null;

            try
            {
                var uri = new Uri(url);
                return uri.AbsolutePath;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取URL的查询参数部分
        /// </summary>
        public static string? GetQueryString(string url)
        {
            if (!IsValidUrl(url))
                return null;

            try
            {
                var uri = new Uri(url);
                return uri.Query;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 合并URL路径
        /// </summary>
        public static string CombinePaths(params string[] paths)
        {
            if (paths == null || paths.Length == 0)
                return string.Empty;

            var result = new StringBuilder();
            foreach (var path in paths)
            {
                if (string.IsNullOrEmpty(path))
                    continue;

                var cleanPath = path.Trim().Trim('/');
                if (!string.IsNullOrEmpty(cleanPath))
                {
                    if (result.Length > 0)
                        result.Append('/');
                    result.Append(cleanPath);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// 确保URL以斜杠结尾
        /// </summary>
        public static string EnsureTrailingSlash(string url)
        {
            if (string.IsNullOrEmpty(url))
                return "/";

            return url.EndsWith("/") ? url : url + "/";
        }

        /// <summary>
        /// 确保URL不以斜杠结尾
        /// </summary>
        public static string EnsureNoTrailingSlash(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            return url.TrimEnd('/');
        }

        /// <summary>
        /// 从URL中移除查询参数
        /// </summary>
        public static string RemoveQueryString(string url)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            var queryIndex = url.IndexOf('?');
            return queryIndex >= 0 ? url.Substring(0, queryIndex) : url;
        }

        /// <summary>
        /// 从URL中获取特定查询参数的值
        /// </summary>
        public static string? GetQueryParameter(string url, string parameterName)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(parameterName))
                return null;

            try
            {
                var uri = new Uri(url);
                var query = HttpUtility.ParseQueryString(uri.Query);
                return query[parameterName];
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 添加或更新URL查询参数
        /// </summary>
        public static string AddOrUpdateQueryParameter(string url, string parameterName, object parameterValue)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            var baseUrl = RemoveQueryString(url);
            var existingParams = ParseQueryString(GetQueryString(url) ?? "");
            
            var paramsDict = new Dictionary<string, object>();
            foreach (var param in existingParams)
            {
                paramsDict[param.Key] = param.Value;
            }
            
            paramsDict[parameterName] = parameterValue?.ToString() ?? string.Empty;

            return BuildUrl(baseUrl, paramsDict);
        }

        /// <summary>
        /// 移除URL中的特定查询参数
        /// </summary>
        public static string RemoveQueryParameter(string url, string parameterName)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(parameterName))
                return url;

            var baseUrl = RemoveQueryString(url);
            var existingParams = ParseQueryString(GetQueryString(url) ?? "");
            
            var paramsDict = new Dictionary<string, object>();
            foreach (var param in existingParams)
            {
                paramsDict[param.Key] = param.Value;
            }
            
            paramsDict.Remove(parameterName);

            return BuildUrl(baseUrl, paramsDict);
        }

        /// <summary>
        /// 生成短链接（基于Base64编码）
        /// </summary>
        public static string GenerateShortUrl(string longUrl, int length = 8)
        {
            if (string.IsNullOrEmpty(longUrl))
                return string.Empty;

            var hash = CryptoHelper.ComputeMD5Hash(longUrl);
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(hash))
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "");

            return base64.Substring(0, Math.Min(length, base64.Length));
        }

        /// <summary>
        /// 检查URL是否包含特定路径
        /// </summary>
        public static bool ContainsPath(string url, string path)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(path))
                return false;

            var urlPath = GetPath(url);
            return !string.IsNullOrEmpty(urlPath) && urlPath.Contains(path);
        }

        /// <summary>
        /// 标准化URL（移除多余的斜杠等）
        /// </summary>
        public static string NormalizeUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            try
            {
                var uri = new Uri(url);
                return uri.ToString();
            }
            catch
            {
                return url;
            }
        }

        /// <summary>
        /// 比较两个URL是否相同（忽略协议和端口）
        /// </summary>
        public static bool AreUrlsEquivalent(string url1, string url2)
        {
            if (string.IsNullOrEmpty(url1) || string.IsNullOrEmpty(url2))
                return false;

            try
            {
                var uri1 = new Uri(url1);
                var uri2 = new Uri(url2);

                return uri1.Host.Equals(uri2.Host, StringComparison.OrdinalIgnoreCase) &&
                       uri1.AbsolutePath.Equals(uri2.AbsolutePath, StringComparison.OrdinalIgnoreCase) &&
                       uri1.Query.Equals(uri2.Query, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}