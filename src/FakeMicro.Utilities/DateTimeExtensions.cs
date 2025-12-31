using System;

namespace FakeMicro.Utilities
{
    /// <summary>
    /// DateTime 辅助扩展，确保写入数据库的时间为 UTC
    /// </summary>
    public static class DateTimeExtensions
    {
        public static DateTime EnsureUtc(this DateTime value)
        {
            return value.Kind == DateTimeKind.Utc
                ? value
                : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        public static DateTime? EnsureUtc(this DateTime? value)
        {
            if (!value.HasValue) return null;
            return value.Value.EnsureUtc();
        }
    }
}