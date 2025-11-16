using System.Security.Cryptography;

namespace FakeMicro.Api.Services
{
    /// <summary>
    /// 加密辅助类
    /// </summary>
    public static class CryptoHelper
    {
        /// <summary>
        /// 使用恒定时间比较两个字符串
        /// 这种比较方法可以防止计时攻击，无论字符串是否相等，都需要相同的时间来执行
        /// </summary>
        /// <param name="a">第一个字符串</param>
        /// <param name="b">第二个字符串</param>
        /// <returns>如果两个字符串相等则返回true，否则返回false</returns>
        public static bool ConstantTimeEquals(string a, string b)
        {
            // 检查null值
            if (a == null && b == null)
                return true;
            
            if (a == null || b == null)
                return false;
            
            // 如果长度不同，直接返回false
            // 注意：这里先检查长度会导致长度信息泄露，但在实际应用中通常是可以接受的
            // 因为长度差异通常不会提供足够的信息来进行有效的攻击
            if (a.Length != b.Length)
                return false;
            
            // 使用恒定时间比较
            bool isEqual = true;
            for (int i = 0; i < a.Length; i++)
            {
                // 即使前面的字符已经不匹配，也要继续比较所有字符
                // 这样可以确保比较时间与字符串内容无关
                if (a[i] != b[i])
                {
                    isEqual = false;
                }
            }
            
            return isEqual;
        }

        /// <summary>
        /// 使用恒定时间比较两个字节数组
        /// </summary>
        /// <param name="a">第一个字节数组</param>
        /// <param name="b">第二个字节数组</param>
        /// <returns>如果两个字节数组相等则返回true，否则返回false</returns>
        public static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            // 使用BCrypt.Net的SecureEquals方法（如果可用）或自行实现
            // 这里使用手动实现的恒定时间比较
            
            // 检查null值
            if (a == null && b == null)
                return true;
            
            if (a == null || b == null)
                return false;
            
            // 如果长度不同，直接返回false
            if (a.Length != b.Length)
                return false;
            
            // 使用恒定时间比较
            int result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                // XOR操作：如果两个字节不同，结果不为0
                result |= a[i] ^ b[i];
            }
            
            // 如果result为0，表示所有字节都相同
            return result == 0;
        }

        /// <summary>
        /// 生成随机字节数组
        /// </summary>
        /// <param name="length">字节数组长度</param>
        /// <returns>随机字节数组</returns>
        public static byte[] GenerateRandomBytes(int length)
        {
            var bytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return bytes;
        }

        /// <summary>
        /// 生成随机字符串
        /// </summary>
        /// <param name="length">字符串长度</param>
        /// <returns>随机字符串</returns>
        public static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var data = GenerateRandomBytes(length);
            var result = new char[length];
            
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[data[i] % chars.Length];
            }
            
            return new string(result);
        }
    }
}