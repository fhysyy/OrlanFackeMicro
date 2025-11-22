namespace FakeMicro.Interfaces.Models.Requests
{
    /// <summary>
    /// 批量操作类型
    /// </summary>
    public enum BatchOperationType
    {
        /// <summary>
        /// 物理删除
        /// </summary>
        Delete,
        
        /// <summary>
        /// 软删除
        /// </summary>
        SoftDelete,
        
        /// <summary>
        /// 恢复软删除的记录
        /// </summary>
        Restore,
        
        /// <summary>
        /// 归档记录
        /// </summary>
        Archive
    }
}