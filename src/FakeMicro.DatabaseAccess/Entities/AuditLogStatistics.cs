namespace FakeMicro.DatabaseAccess.Entities
{
    /// <summary>
    /// 审计日志统计信息
    /// </summary>
    public class AuditLogStatistics
    {
        public int TotalCount { get; set; }
        public int UserCount { get; set; }
        public int ActionCount { get; set; }
        public int UniqueUserCount { get; set; }
        public int UniqueActionCount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}