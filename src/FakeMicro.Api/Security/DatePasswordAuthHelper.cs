using Microsoft.AspNetCore.Http;
using System;
using System.Text;

namespace FakeMicro.Api.Security
{
    public static class DatePasswordAuthHelper
    {
        private const string CookieName = "HangfireAuth";

        public static bool AuthenticateRequest(HttpContext context)
        {
            var currentDatePassword = DateTime.Now.ToString("yyyyMMdd");

            var authHeader = context.Request.Headers["Authorization"].ToString();

            if (string.IsNullOrEmpty(authHeader))
            {
                return false;
            }

            if (!authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
            byte[] decodedBytes;
            try
            {
                decodedBytes = Convert.FromBase64String(encodedCredentials);
            }
            catch
            {
                return false;
            }

            var credentials = Encoding.UTF8.GetString(decodedBytes);
            var parts = credentials.Split(':');

            if (parts.Length != 2)
            {
                return false;
            }

            var providedPassword = parts[1];

            if (providedPassword == currentDatePassword)
            {
                SetAuthCookie(context, currentDatePassword);
                return true;
            }

            return false;
        }

        public static bool AuthenticateByQueryString(HttpContext context)
        {
            var currentDatePassword = DateTime.Now.ToString("yyyyMMdd");
            var providedPassword = context.Request.Query["password"].ToString();

          

            if (providedPassword == currentDatePassword)
            {
                SetAuthCookie(context, currentDatePassword);
                return true;
            }

            return false;
        }

        public static bool AuthenticateByCookie(HttpContext context)
        {
            var currentDatePassword = DateTime.Now.ToString("yyyyMMdd");
            var cookieValue = context.Request.Cookies[CookieName];

           

            return cookieValue == currentDatePassword;
        }

        private static void SetAuthCookie(HttpContext context, string password)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.Now.AddDays(1)
            };

            context.Response.Cookies.Append(CookieName, password, cookieOptions);
        }
    }
}
