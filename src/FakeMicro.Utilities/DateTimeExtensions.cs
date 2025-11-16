using System;

namespace FakeMicro.Utilities
{
    /// <summary>
    /// 日期时间扩展方法
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// 转换为Unix时间戳（秒）
        /// </summary>
        public static long ToUnixTimestamp(this DateTime dateTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long)(dateTime.ToUniversalTime() - epoch).TotalSeconds;
        }

        /// <summary>
        /// 从Unix时间戳转换为DateTime
        /// </summary>
        public static DateTime FromUnixTimestamp(this long timestamp)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(timestamp);
        }

        /// <summary>
        /// 转换为Unix时间戳（毫秒）
        /// </summary>
        public static long ToUnixTimestampMilliseconds(this DateTime dateTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long)(dateTime.ToUniversalTime() - epoch).TotalMilliseconds;
        }

        /// <summary>
        /// 从Unix时间戳（毫秒）转换为DateTime
        /// </summary>
        public static DateTime FromUnixTimestampMilliseconds(this long timestamp)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddMilliseconds(timestamp);
        }

        /// <summary>
        /// 检查日期是否为今天
        /// </summary>
        public static bool IsToday(this DateTime dateTime)
        {
            return dateTime.Date == DateTime.Today;
        }

        /// <summary>
        /// 检查日期是否为工作日（周一到周五）
        /// </summary>
        public static bool IsWeekday(this DateTime dateTime)
        {
            return dateTime.DayOfWeek != DayOfWeek.Saturday && dateTime.DayOfWeek != DayOfWeek.Sunday;
        }

        /// <summary>
        /// 检查日期是否为周末
        /// </summary>
        public static bool IsWeekend(this DateTime dateTime)
        {
            return dateTime.DayOfWeek == DayOfWeek.Saturday || dateTime.DayOfWeek == DayOfWeek.Sunday;
        }

        /// <summary>
        /// 获取年龄
        /// </summary>
        public static int GetAge(this DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            return age;
        }

        /// <summary>
        /// 获取月份的第一天
        /// </summary>
        public static DateTime StartOfMonth(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, 1);
        }

        /// <summary>
        /// 获取月份的最后一天
        /// </summary>
        public static DateTime EndOfMonth(this DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, 1).AddMonths(1).AddDays(-1);
        }

        /// <summary>
        /// 获取周的开始（周一）
        /// </summary>
        public static DateTime StartOfWeek(this DateTime dateTime)
        {
            var diff = (7 + (dateTime.DayOfWeek - DayOfWeek.Monday)) % 7;
            return dateTime.AddDays(-1 * diff).Date;
        }

        /// <summary>
        /// 获取周的结束（周日）
        /// </summary>
        public static DateTime EndOfWeek(this DateTime dateTime)
        {
            return dateTime.StartOfWeek().AddDays(6);
        }

        /// <summary>
        /// 格式化时间为友好显示（如"刚刚"、"5分钟前"等）
        /// </summary>
        public static string ToFriendlyDisplay(this DateTime dateTime)
        {
            var now = DateTime.Now;
            var span = now - dateTime;

            if (span.TotalSeconds < 60)
                return "刚刚";
            if (span.TotalMinutes < 60)
                return $"{(int)span.TotalMinutes}分钟前";
            if (span.TotalHours < 24)
                return $"{(int)span.TotalHours}小时前";
            if (span.TotalDays < 7)
                return $"{(int)span.TotalDays}天前";
            if (span.TotalDays < 30)
                return $"{(int)(span.TotalDays / 7)}周前";
            if (span.TotalDays < 365)
                return $"{(int)(span.TotalDays / 30)}个月前";

            return $"{(int)(span.TotalDays / 365)}年前";
        }

        /// <summary>
        /// 检查日期是否在指定范围内
        /// </summary>
        public static bool IsBetween(this DateTime dateTime, DateTime start, DateTime end)
        {
            return dateTime >= start && dateTime <= end;
        }

        /// <summary>
        /// 转换为ISO 8601格式字符串
        /// </summary>
        public static string ToIso8601String(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }
    }
}