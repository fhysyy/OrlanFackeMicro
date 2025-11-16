using System;

namespace FakeMicro.DatabaseAccess
{
    /// <summary>
    /// 软删除接口
    /// </summary>
    public interface ISoftDeletable
    {
        /// <summary>
        /// 是否已删除
        /// </summary>
        bool IsDeleted { get; set; }
        
        /// <summary>
        /// 删除时间
        /// </summary>
        DateTime? DeletedAt { get; set; }
        
        /// <summary>
        /// 删除人
        /// </summary>
        string? DeletedBy { get; set; }
    }
}