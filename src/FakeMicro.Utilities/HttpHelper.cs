using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FakeMicro.Utilities
{
    /// <summary>
    /// HTTP请求助手类
    /// </summary>
    public static class HttpHelper
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// 发送GET请求
        /// </summary>
        public static async Task<HttpResponseMessage> GetAsync(string url, Dictionary<string, string>? headers = null, TimeSpan? timeout = null)
        {
            return await SendRequestAsync(HttpMethod.Get, url, null, headers, timeout);
        }

        /// <summary>
        /// 发送POST请求（JSON格式）
        /// </summary>
        public static async Task<HttpResponseMessage> PostJsonAsync(string url, object data, Dictionary<string, string>? headers = null, TimeSpan? timeout = null)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await SendRequestAsync(HttpMethod.Post, url, content, headers, timeout);
        }

        /// <summary>
        /// 发送POST请求（表单格式）
        /// </summary>
        public static async Task<HttpResponseMessage> PostFormAsync(string url, Dictionary<string, string> formData, Dictionary<string, string>? headers = null, TimeSpan? timeout = null)
        {
            var content = new FormUrlEncodedContent(formData);
            return await SendRequestAsync(HttpMethod.Post, url, content, headers, timeout);
        }

        /// <summary>
        /// 发送PUT请求（JSON格式）
        /// </summary>
        public static async Task<HttpResponseMessage> PutJsonAsync(string url, object data, Dictionary<string, string>? headers = null, TimeSpan? timeout = null)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await SendRequestAsync(HttpMethod.Put, url, content, headers, timeout);
        }

        /// <summary>
        /// 发送DELETE请求
        /// </summary>
        public static async Task<HttpResponseMessage> DeleteAsync(string url, Dictionary<string, string>? headers = null, TimeSpan? timeout = null)
        {
            return await SendRequestAsync(HttpMethod.Delete, url, null, headers, timeout);
        }

        /// <summary>
        /// 发送HTTP请求
        /// </summary>
        private static async Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, string url, HttpContent? content = null, Dictionary<string, string>? headers = null, TimeSpan? timeout = null)
        {
            using var request = new HttpRequestMessage(method, url);
            
            if (content != null)
            {
                request.Content = content;
            }

            // 添加请求头
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            // 设置超时
            if (timeout.HasValue)
            {
                _httpClient.Timeout = timeout.Value;
            }

            return await _httpClient.SendAsync(request);
        }

        /// <summary>
        /// 读取响应内容为字符串
        /// </summary>
        public static async Task<string> ReadResponseAsStringAsync(HttpResponseMessage response)
        {
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// 读取响应内容为JSON对象
        /// </summary>
        public static async Task<T?> ReadResponseAsJsonAsync<T>(HttpResponseMessage response)
        {
            var json = await ReadResponseAsStringAsync(response);
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        /// <summary>
        /// 检查HTTP响应是否成功
        /// </summary>
        public static bool IsSuccess(HttpResponseMessage response)
        {
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// 获取HTTP状态码描述
        /// </summary>
        public static string GetStatusCodeDescription(int statusCode)
        {
            return statusCode switch
            {
                200 => "OK",
                201 => "Created",
                204 => "No Content",
                400 => "Bad Request",
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "Not Found",
                500 => "Internal Server Error",
                _ => "Unknown"
            };
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
                query.Add($"{Uri.EscapeDataString(param.Key)}={Uri.EscapeDataString(value)}");
            }

            return "?" + string.Join("&", query);
        }

        /// <summary>
        /// 设置基本认证头
        /// </summary>
        public static Dictionary<string, string> CreateBasicAuthHeader(string username, string password)
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            return new Dictionary<string, string>
            {
                { "Authorization", $"Basic {credentials}" }
            };
        }

        /// <summary>
        /// 设置Bearer Token认证头
        /// </summary>
        public static Dictionary<string, string> CreateBearerTokenHeader(string token)
        {
            return new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {token}" }
            };
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        public static async Task DownloadFileAsync(string url, string filePath, Dictionary<string, string>? headers = null)
        {
            var response = await GetAsync(url, headers);
            if (response.IsSuccessStatusCode)
            {
                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(filePath, fileBytes);
            }
            else
            {
                throw new HttpRequestException($"Download failed with status code: {response.StatusCode}");
            }
        }

        /// <summary>
        /// 上传文件
        /// </summary>
        public static async Task<HttpResponseMessage> UploadFileAsync(string url, string filePath, string fieldName = "file", Dictionary<string, string>? additionalData = null, Dictionary<string, string>? headers = null)
        {
            using var form = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(filePath);
            using var fileContent = new StreamContent(fileStream);
            
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            form.Add(fileContent, fieldName, Path.GetFileName(filePath));

            // 添加额外数据
            if (additionalData != null)
            {
                foreach (var data in additionalData)
                {
                    form.Add(new StringContent(data.Value), data.Key);
                }
            }

            return await SendRequestAsync(HttpMethod.Post, url, form, headers);
        }
    }

    /// <summary>
    /// HTTP客户端包装器
    /// </summary>
    public class HttpClientWrapper : IDisposable
    {
        private readonly HttpClient _client;
        private readonly Dictionary<string, string> _defaultHeaders;

        public HttpClientWrapper(string baseUrl = "", Dictionary<string, string>? defaultHeaders = null)
        {
            _client = new HttpClient();
            _defaultHeaders = defaultHeaders ?? new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(baseUrl))
            {
                _client.BaseAddress = new Uri(baseUrl);
            }

            // 设置默认请求头
            foreach (var header in _defaultHeaders)
            {
                _client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        /// <summary>
        /// 设置超时时间
        /// </summary>
        public void SetTimeout(TimeSpan timeout)
        {
            _client.Timeout = timeout;
        }

        /// <summary>
        /// 添加默认请求头
        /// </summary>
        public void AddDefaultHeader(string name, string value)
        {
            _client.DefaultRequestHeaders.Add(name, value);
            _defaultHeaders[name] = value;
        }

        /// <summary>
        /// 移除默认请求头
        /// </summary>
        public void RemoveDefaultHeader(string name)
        {
            _client.DefaultRequestHeaders.Remove(name);
            _defaultHeaders.Remove(name);
        }

        /// <summary>
        /// 发送GET请求
        /// </summary>
        public async Task<HttpResponseMessage> GetAsync(string endpoint, Dictionary<string, string>? headers = null)
        {
            return await HttpHelper.GetAsync(GetFullUrl(endpoint), MergeHeaders(headers));
        }

        /// <summary>
        /// 发送POST请求（JSON格式）
        /// </summary>
        public async Task<HttpResponseMessage> PostJsonAsync(string endpoint, object data, Dictionary<string, string>? headers = null)
        {
            return await HttpHelper.PostJsonAsync(GetFullUrl(endpoint), data, MergeHeaders(headers));
        }

        /// <summary>
        /// 发送PUT请求（JSON格式）
        /// </summary>
        public async Task<HttpResponseMessage> PutJsonAsync(string endpoint, object data, Dictionary<string, string>? headers = null)
        {
            return await HttpHelper.PutJsonAsync(GetFullUrl(endpoint), data, MergeHeaders(headers));
        }

        /// <summary>
        /// 发送DELETE请求
        /// </summary>
        public async Task<HttpResponseMessage> DeleteAsync(string endpoint, Dictionary<string, string>? headers = null)
        {
            return await HttpHelper.DeleteAsync(GetFullUrl(endpoint), MergeHeaders(headers));
        }

        private string GetFullUrl(string endpoint)
        {
            if (_client.BaseAddress == null)
                return endpoint;

            return new Uri(_client.BaseAddress, endpoint).ToString();
        }

        private Dictionary<string, string>? MergeHeaders(Dictionary<string, string>? headers)
        {
            if (headers == null && _defaultHeaders.Count == 0)
                return null;

            var merged = new Dictionary<string, string>(_defaultHeaders);
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    merged[header.Key] = header.Value;
                }
            }

            return merged;
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }

    /// <summary>
    /// HTTP响应结果
    /// </summary>
    public class HttpResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public int StatusCode { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
    }

    /// <summary>
    /// HTTP请求配置
    /// </summary>
    public class HttpRequestConfig
    {
        public Dictionary<string, string>? Headers { get; set; }
        public TimeSpan? Timeout { get; set; }
        public int RetryCount { get; set; } = 0;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    }
}