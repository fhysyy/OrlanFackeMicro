using System.Security.Cryptography;
using System.Text;

namespace FakeMicro.Api.Services
{
    /// <summary>
    /// 提供常数时间比较方法，用于防止时间攻击
    /// </summary>
    public static class ConstantTimeComparison
    {
        /// <summary>
        /// 使用常数时间比较两个字符串，避免时间攻击
        /// </summary>
        public static bool Equals(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            // 使用字节数组进行比较
            byte[] aBytes = Encoding.UTF8.GetBytes(a);
            byte[] bBytes = Encoding.UTF8.GetBytes(b);

            // 使用常数时间比较
            bool result = true;
            for (int i = 0; i < aBytes.Length; i++)
            {
                result &= (aBytes[i] == bBytes[i]);
            }

            return result;
        }

        /// <summary>
        /// 使用常数时间比较两个字节数组
        /// </summary>
        public static bool Equals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            // 使用常数时间比较
            bool result = true;
            for (int i = 0; i < a.Length; i++)
            {
                result &= (a[i] == b[i]);
            }

            return result;
        }
    }
}