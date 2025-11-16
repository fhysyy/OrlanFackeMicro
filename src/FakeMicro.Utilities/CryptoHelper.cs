using System;
using System.Security.Cryptography;
using System.Text;

namespace FakeMicro.Utilities
{
    /// <summary>
    /// 加密助手类
    /// </summary>
    public static class CryptoHelper
    {
        /// <summary>
        /// 计算字符串的MD5哈希值
        /// </summary>
        public static string ComputeMD5Hash(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            
            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = md5.ComputeHash(inputBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// 计算字符串的SHA256哈希值
        /// </summary>
        public static string ComputeSHA256Hash(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            
            using var sha256 = SHA256.Create();
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha256.ComputeHash(inputBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// 计算字符串的SHA1哈希值
        /// </summary>
        public static string ComputeSHA1Hash(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            
            using var sha1 = SHA1.Create();
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha1.ComputeHash(inputBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// 生成随机字符串
        /// </summary>
        public static string GenerateRandomString(int length, string? allowedChars = null)
        {
            allowedChars ??= "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var chars = new char[length];
            
            for (int i = 0; i < length; i++)
            {
                chars[i] = allowedChars[random.Next(allowedChars.Length)];
            }
            return new string(chars);
        }

        /// <summary>
        /// 生成安全的随机数
        /// </summary>
        public static byte[] GenerateRandomBytes(int length)
        {
            var bytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return bytes;
        }

        /// <summary>
        /// 生成GUID字符串（不带连字符）
        /// </summary>
        public static string GenerateGuidString()
        {
            return Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// 简单的字符串加密（Base64编码）
        /// </summary>
        public static string SimpleEncrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;
            
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        /// <summary>
        /// 简单的字符串解密（Base64解码）
        /// </summary>
        public static string SimpleDecrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText)) return string.Empty;
            
            try
            {
                var base64EncodedBytes = Convert.FromBase64String(encryptedText);
                return Encoding.UTF8.GetString(base64EncodedBytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 使用AES加密字符串
        /// </summary>
        public static string AesEncrypt(string plainText, string key, string iv)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;
            
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
            aes.IV = Encoding.UTF8.GetBytes(iv.PadRight(16).Substring(0, 16));
            
            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new System.IO.MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using var sw = new System.IO.StreamWriter(cs);
            sw.Write(plainText);
            sw.Close();
            cs.Close();
            
            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// 使用AES解密字符串
        /// </summary>
        public static string AesDecrypt(string encryptedText, string key, string iv)
        {
            if (string.IsNullOrEmpty(encryptedText)) return string.Empty;
            
            try
            {
                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
                aes.IV = Encoding.UTF8.GetBytes(iv.PadRight(16).Substring(0, 16));
                
                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new System.IO.MemoryStream(Convert.FromBase64String(encryptedText));
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new System.IO.StreamReader(cs);
                return sr.ReadToEnd();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 生成安全的密码哈希（使用PBKDF2）
        /// </summary>
        public static string GeneratePasswordHash(string password, int saltSize = 16, int iterations = 10000, int hashSize = 32)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;
            
            // 生成盐
            var salt = GenerateRandomBytes(saltSize);
            
            // 使用PBKDF2生成哈希
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(hashSize);
            
            // 组合盐和哈希
            var hashBytes = new byte[saltSize + hashSize];
            Array.Copy(salt, 0, hashBytes, 0, saltSize);
            Array.Copy(hash, 0, hashBytes, saltSize, hashSize);
            
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// 验证密码哈希
        /// </summary>
        public static bool VerifyPasswordHash(string password, string hashedPassword, int saltSize = 16, int iterations = 10000, int hashSize = 32)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword)) return false;
            
            try
            {
                var hashBytes = Convert.FromBase64String(hashedPassword);
                if (hashBytes.Length != saltSize + hashSize) return false;
                
                var salt = new byte[saltSize];
                Array.Copy(hashBytes, 0, salt, 0, saltSize);
                
                using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
                var hash = pbkdf2.GetBytes(hashSize);
                
                for (int i = 0; i < hashSize; i++)
                {
                    if (hashBytes[i + saltSize] != hash[i])
                        return false;
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}