using System;

namespace FakeMicro.Entities
{
    /// <summary>
    /// 审计接口
    /// </summary>
    public interface IAuditable
    {
        /// <summary>
        /// 创建时间
        /// </summary>
        DateTime created_at { get; set; }
        
        /// <summary>
        /// 创建人
        /// </summary>
        string created_by { get; set; }
        
        /// <summary>
        /// 更新时间
        /// </summary>
        DateTime? updated_at { get; set; }
        
        /// <summary>
        /// 更新人
        /// </summary>
        string? updated_by { get; set; }
    }
}